using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween
{
    public struct Sequence : IComponentData
    {
        public PlaybackState State;
        public float TimeScale;
        public float Time;
        public float Duration;
        public int Direction;

        public readonly float Position() => Time;
        public readonly float TotalDuration() => Duration;

        public static SequenceBuilder Create() => SequenceBuilder.Create();
    }

    internal enum SequenceElementKind : byte
    {
        Tween,
        Wait,
        Callback
    }

    [InternalBufferCapacity(8)]
    internal struct SequenceElement : IBufferElementData
    {
        public SequenceElementKind Kind;
        public Entity GhostEntity;
        public Entity TargetEntity;
        public TweenType TweenType;
        public TweenSpace Space;
        public float StartTime;
        public float Duration;
        public FixedString64Bytes CallbackId;
        public bool Started;
        public bool Completed;
        public bool UseChase;
        public ChaseMode ChaseMode;
        public float ChaseSmoothTime;
        public float ChaseMaxSpeed;
        public bool KillOnChase;
        public bool StartFromCurrent;
    }

    public struct SequenceCallbackEvent : IComponentData
    {
        public Entity SequenceEntity;
        public FixedString64Bytes CallbackId;
    }

    public readonly struct SequenceWait
    {
        public readonly float Seconds;

        public SequenceWait(float seconds)
        {
            Seconds = math.max(0f, seconds);
        }
    }

    public static class SequenceTweenExtensions
    {
        public static SequenceWait Wait(this Entity entity, float seconds) => new(seconds);
    }

    public struct SequenceBuilder
    {
        private List<SequenceBuildItem> _items;
        private float _cursor;
        private float _duration;
        private float _lastStart;
        private LoopType _loopType;
        private uint _loopCount;
        private PlaybackTimeType _timeType;
        private float _timeScale;

        public static SequenceBuilder Create()
        {
            return new SequenceBuilder
            {
                _items = new List<SequenceBuildItem>(),
                _timeType = PlaybackTimeType.Fixed,
                _timeScale = 1f
            };
        }

        public SequenceBuilder Append<T>(TweenBlueprint<T> tween) where T : unmanaged
        {
            EnsureItems();
            AddTween(tween, _cursor);
            _lastStart = _cursor;
            _cursor += math.max(0f, tween.SecondsToPlay);
            _duration = math.max(_duration, _cursor);
            return this;
        }

        public SequenceBuilder Join<T>(TweenBlueprint<T> tween) where T : unmanaged
        {
            EnsureItems();
            AddTween(tween, _lastStart);
            _duration = math.max(_duration, _lastStart + math.max(0f, tween.SecondsToPlay));
            _cursor = math.max(_cursor, _lastStart + math.max(0f, tween.SecondsToPlay));
            return this;
        }

        public SequenceBuilder Insert<T>(float at, TweenBlueprint<T> tween) where T : unmanaged
        {
            EnsureItems();
            var startTime = math.max(0f, at);
            AddTween(tween, startTime);
            _duration = math.max(_duration, startTime + math.max(0f, tween.SecondsToPlay));
            return this;
        }

        public SequenceBuilder Append(SequenceWait wait)
        {
            EnsureItems();
            AddWait(_cursor, wait.Seconds);
            _lastStart = _cursor;
            _cursor += wait.Seconds;
            _duration = math.max(_duration, _cursor);
            return this;
        }

        public SequenceBuilder Insert(float at, SequenceWait wait)
        {
            EnsureItems();
            var startTime = math.max(0f, at);
            AddWait(startTime, wait.Seconds);
            _duration = math.max(_duration, startTime + wait.Seconds);
            return this;
        }

        public SequenceBuilder AppendCallback(string callbackId)
        {
            return InsertCallback(_cursor, callbackId);
        }

        public SequenceBuilder InsertCallback(float at, string callbackId)
        {
            EnsureItems();
            _items.Add(SequenceBuildItem.Callback(math.max(0f, at), new FixedString64Bytes(callbackId)));
            _duration = math.max(_duration, math.max(0f, at));
            return this;
        }

        public SequenceBuilder Loop(LoopType loopType = LoopType.PingPong, uint count = 0)
        {
            if (loopType == LoopType.Random)
            {
                Debug.LogWarning("Sequence does not support LoopType.Random yet; falling back to Repeat.");
                loopType = LoopType.Repeat;
            }

            _loopType = loopType;
            _loopCount = count;
            return this;
        }

        public SequenceBuilder TimeType(PlaybackTimeType timeType)
        {
            _timeType = timeType;
            return this;
        }

        public SequenceBuilder TimeScale(float timeScale)
        {
            _timeScale = math.max(0f, timeScale);
            return this;
        }

        public Entity Play(EntityCommandBuffer ecb)
        {
            var adapter = new EntityCommandBufferAdapter { ECB = ecb };
            return PlayInternal(adapter);
        }

        public Entity Play(EntityManager entityManager)
        {
            var adapter = new EntityManagerAdapter { Em = entityManager };
            return PlayInternal(adapter);
        }

        public Entity Play<TAuth>(Baker<TAuth> baker) where TAuth : MonoBehaviour
        {
            var adapter = new BakerAdapter<TAuth> { Baker = baker };
            return PlayInternal(adapter);
        }

        private Entity PlayInternal<TAdapter>(TAdapter adapter)
            where TAdapter : struct, IEntityCommandAdapter
        {
            EnsureItems();

            var sequenceEntity = adapter.CreateEntity();
            adapter.AddComponent(sequenceEntity, new Sequence
            {
                State = PlaybackState.Playing,
                TimeScale = _timeScale <= 0f ? 1f : _timeScale,
                Time = 0f,
                Duration = _duration,
                Direction = 1
            });

            adapter.AddComponent(sequenceEntity, new PlaybackProgress
            {
                TimeType = _timeType,
                NormalizedTime = 0f
            });

            if (_loopType != LoopType.None)
            {
                adapter.AddComponent(sequenceEntity, new PlaybackLoop
                {
                    LoopType = _loopType,
                    LoopCount = _loopCount,
                    LoopIndex = 0
                });
            }

            var runtimeElements = new List<SequenceElement>(_items.Count);
            for (int i = 0; i < _items.Count; i++)
                runtimeElements.Add(CreateRuntimeElement(_items[i], adapter));

            var buffer = adapter.AddBuffer<SequenceElement>(sequenceEntity);
            for (int i = 0; i < runtimeElements.Count; i++)
                buffer.Add(runtimeElements[i]);

            return sequenceEntity;
        }

        private SequenceElement CreateRuntimeElement<TAdapter>(SequenceBuildItem item, TAdapter adapter)
            where TAdapter : struct, IEntityCommandAdapter
        {
            if (item.Kind != SequenceElementKind.Tween)
            {
                return new SequenceElement
                {
                    Kind = item.Kind,
                    StartTime = item.StartTime,
                    Duration = item.Duration,
                    CallbackId = item.CallbackId
                };
            }

            var ghostEntity = CreateGhostEntity(item, adapter);
            item.DisposeSplineBlobs();
            return new SequenceElement
            {
                Kind = SequenceElementKind.Tween,
                GhostEntity = ghostEntity,
                TargetEntity = item.TargetEntity,
                TweenType = item.TweenType,
                Space = item.Space,
                StartTime = item.StartTime,
                Duration = item.Duration,
                UseChase = item.UseChase,
                ChaseMode = item.ChaseMode,
                ChaseSmoothTime = item.ChaseSmoothTime,
                ChaseMaxSpeed = item.ChaseMaxSpeed,
                KillOnChase = item.KillOnChase,
                StartFromCurrent = item.StartFromCurrent
            };
        }

        private Entity CreateGhostEntity<TAdapter>(SequenceBuildItem item, TAdapter adapter)
            where TAdapter : struct, IEntityCommandAdapter
        {
            var entity = TweenBlueprintExtensions.CreateGhostEntity(item, adapter, false, false);
            adapter.AddComponent(entity, new TweenSequenceDriven());
            return entity;
        }

        private void AddTween<T>(TweenBlueprint<T> tween, float startTime) where T : unmanaged
        {
            tween.TimeType = _timeType;
            _items.Add(SequenceBuildItem.Tween(tween, math.max(0f, startTime), math.max(0f, tween.SecondsToPlay)));
        }

        private void AddWait(float startTime, float duration)
        {
            _items.Add(SequenceBuildItem.Wait(math.max(0f, startTime), math.max(0f, duration)));
        }

        private void EnsureItems()
        {
            _items ??= new List<SequenceBuildItem>();
            if (_timeScale <= 0f) _timeScale = 1f;
        }
    }

    internal struct SequenceBuildItem
    {
        public SequenceElementKind Kind;
        public Entity TargetEntity;
        public TweenType TweenType;
        public TweenSpace Space;
        public float StartTime;
        public float Duration;
        public FixedString64Bytes CallbackId;
        public float4 StartPoint;
        public float4 EndPoint;
        public TweenValueKind ValueKind;
        public EaseType EaseType;
        public PlaybackTimeType TimeType;
        public float SecondsToPlay;
        public bool IsLoop;
        public uint LoopCount;
        public LoopType LoopType;
        public bool UseChase;
        public ChaseMode ChaseMode;
        public float ChaseSmoothTime;
        public float ChaseMaxSpeed;
        public bool KillOnChase;
        public bool StartFromCurrent;
        public bool IsSpline;
        public SplineType SplineType;
        public bool SplineIsClosed;
        public bool VisualizePath;
        public BlobAssetReference<SplineBlob<float>> SplineBlobFloat;
        public BlobAssetReference<SplineBlob<float2>> SplineBlobFloat2;
        public BlobAssetReference<SplineBlob<float3>> SplineBlobFloat3;
        public BlobAssetReference<SplineBlob<quaternion>> SplineBlobQuaternion;

        public static SequenceBuildItem Tween<T>(TweenBlueprint<T> tween, float startTime, float duration) where T : unmanaged
        {
            var item = new SequenceBuildItem
            {
                Kind = SequenceElementKind.Tween,
                TargetEntity = tween.Entity,
                TweenType = tween.TweenType,
                Space = tween.Space,
                StartTime = startTime,
                Duration = duration,
                EaseType = tween.EaseType,
                TimeType = tween.TimeType,
                SecondsToPlay = tween.SecondsToPlay,
                IsLoop = tween.IsLoop,
                LoopCount = tween.LoopCount,
                LoopType = tween.LoopType,
                UseChase = tween.UseChase,
                ChaseMode = tween.ChaseMode,
                ChaseSmoothTime = tween.ChaseSmoothTime,
                ChaseMaxSpeed = tween.ChaseMaxSpeed,
                KillOnChase = tween.KillOnChase,
                StartFromCurrent = tween.StartFromCurrent,
                IsSpline = tween.IsSpline,
                SplineType = tween.SplineType,
                SplineIsClosed = tween.SplineIsClosed,
                VisualizePath = tween.VisualizePath
            };

            if (typeof(T) == typeof(float))
            {
                var start = (float)(object)tween.StartPoint;
                var end = (float)(object)tween.EndPoint;
                item.StartPoint = new float4(start, 0f, 0f, 0f);
                item.EndPoint = new float4(end, 0f, 0f, 0f);
                item.ValueKind = TweenValueKind.Float;
                if (tween.IsSpline && tween.SplineBlobRef.IsCreated)
                    item.SplineBlobFloat = (BlobAssetReference<SplineBlob<float>>)(object)tween.SplineBlobRef;
            }
            else if (typeof(T) == typeof(float2))
            {
                var start = (float2)(object)tween.StartPoint;
                var end = (float2)(object)tween.EndPoint;
                item.StartPoint = new float4(start, 0f, 0f);
                item.EndPoint = new float4(end, 0f, 0f);
                item.ValueKind = TweenValueKind.Float2;
                if (tween.IsSpline && tween.SplineBlobRef.IsCreated)
                    item.SplineBlobFloat2 = (BlobAssetReference<SplineBlob<float2>>)(object)tween.SplineBlobRef;
            }
            else if (typeof(T) == typeof(float3))
            {
                var start = (float3)(object)tween.StartPoint;
                var end = (float3)(object)tween.EndPoint;
                item.StartPoint = new float4(start, 0f);
                item.EndPoint = new float4(end, 0f);
                item.ValueKind = TweenValueKind.Float3;
                if (tween.IsSpline && tween.SplineBlobRef.IsCreated)
                    item.SplineBlobFloat3 = (BlobAssetReference<SplineBlob<float3>>)(object)tween.SplineBlobRef;
            }
            else if (typeof(T) == typeof(quaternion))
            {
                var start = (quaternion)(object)tween.StartPoint;
                var end = (quaternion)(object)tween.EndPoint;
                item.StartPoint = start.value;
                item.EndPoint = end.value;
                item.ValueKind = TweenValueKind.Quaternion;
                if (tween.IsSpline && tween.SplineBlobRef.IsCreated)
                    item.SplineBlobQuaternion = (BlobAssetReference<SplineBlob<quaternion>>)(object)tween.SplineBlobRef;
            }
            else
            {
                Debug.LogWarning($"Sequence tween value type {typeof(T)} is not supported.");
            }

            return item;
        }

        public static SequenceBuildItem Wait(float startTime, float duration)
        {
            return new SequenceBuildItem
            {
                Kind = SequenceElementKind.Wait,
                StartTime = startTime,
                Duration = duration
            };
        }

        public static SequenceBuildItem Callback(float startTime, FixedString64Bytes callbackId)
        {
            return new SequenceBuildItem
            {
                Kind = SequenceElementKind.Callback,
                StartTime = startTime,
                CallbackId = callbackId
            };
        }

        public void DisposeSplineBlobs()
        {
            if (SplineBlobFloat.IsCreated) SplineBlobFloat.Dispose();
            if (SplineBlobFloat2.IsCreated) SplineBlobFloat2.Dispose();
            if (SplineBlobFloat3.IsCreated) SplineBlobFloat3.Dispose();
            if (SplineBlobQuaternion.IsCreated) SplineBlobQuaternion.Dispose();

            SplineBlobFloat = default;
            SplineBlobFloat2 = default;
            SplineBlobFloat3 = default;
            SplineBlobQuaternion = default;
        }
    }
}
