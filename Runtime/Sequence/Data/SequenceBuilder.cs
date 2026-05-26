using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace XO.Entityween
{
    public readonly struct SequenceWait : ISequenceActionBlueprint
    {
        public readonly float Seconds;

        public SequenceWait(float seconds)
        {
            Seconds = math.max(0f, seconds);
        }

        public TimelineActionKind Kind => TimelineActionKind.Wait;
        public float Duration => Seconds;
        public FixedString64Bytes CallbackId => default;

        public Entity CreateEntity<TAdapter>(Entity sequenceEntity, TAdapter adapter)
            where TAdapter : IEntityCommandAdapter
        {
            return Entity.Null;
        }
    }

    public readonly struct SequenceCallback : ISequenceActionBlueprint
    {
        public readonly FixedString64Bytes Callback;

        public SequenceCallback(string callbackId)
        {
            Callback = new FixedString64Bytes(callbackId);
        }

        public TimelineActionKind Kind => TimelineActionKind.Callback;
        public float Duration => 0f;
        public FixedString64Bytes CallbackId => Callback;

        public Entity CreateEntity<TAdapter>(Entity sequenceEntity, TAdapter adapter)
            where TAdapter : IEntityCommandAdapter
        {
            return Entity.Null;
        }
    }


    public struct SequenceBuilder
    {
        private List<SequenceElement> _elements;
        private List<ISequenceActionBlueprint> _actions;
        private IEntityCommandAdapter _preparedAdapter;
        private Entity _preparedSequenceEntity;
        private float _cursor;
        private float _duration;
        private float _lastStart;
        private LoopConfig _loop;
        private PlaybackTimeType _timeType;
        private float _timeScale;
        private bool _dynamicTime;

        public static SequenceBuilder Create()
        {
            return new SequenceBuilder
            {
                _elements = new List<SequenceElement>(),
                _actions = new List<ISequenceActionBlueprint>(),
                _timeType = PlaybackTimeType.Fixed,
                _timeScale = 1f,
                _loop = LoopConfig.Default
            };
        }

        internal static SequenceBuilder Create<TAdapter>(TAdapter adapter)
            where TAdapter : struct, IEntityCommandAdapter
        {
            var builder = Create();
            builder.Prepare(adapter);
            return builder;
        }

        public static SequenceBuilder Create(EntityManager entityManager)
        {
            return Create(new EntityManagerAdapter { Em = entityManager });
        }

        public static SequenceBuilder Create(EntityCommandBuffer ecb)
        {
            return Create(new EntityCommandBufferAdapter { ECB = ecb });
        }

        public static SequenceBuilder Create<TAuth>(Baker<TAuth> baker) where TAuth : MonoBehaviour
        {
            return Create(new BakerAdapter<TAuth> { Baker = baker });
        }

        public SequenceBuilder Append<TAction>(TAction action) where TAction : ISequenceActionBlueprint
        {
            AddAction(action, _cursor);
            _lastStart = _cursor;
            _cursor += action.Duration;
            _duration = math.max(_duration, _cursor);
            return this;
        }

        public SequenceBuilder Join<TAction>(TAction action) where TAction : ISequenceActionBlueprint
        {
            AddAction(action, _lastStart);
            var end = _lastStart + action.Duration;
            _duration = math.max(_duration, end);
            _cursor = math.max(_cursor, end);
            return this;
        }

        public SequenceBuilder Insert<TAction>(float at, TAction action) where TAction : ISequenceActionBlueprint
        {
            var startTime = math.max(0f, at);
            AddAction(action, startTime);
            _duration = math.max(_duration, startTime + action.Duration);
            return this;
        }

        public SequenceBuilder AppendWait(float waitSeconds)
            => Append(new SequenceWait(waitSeconds));

        public SequenceBuilder InsertWait(float at, float waitSeconds)
            => Insert(at, new SequenceWait(waitSeconds));

        public SequenceBuilder AppendCallback(string callbackId)
            => Append(new SequenceCallback(callbackId));

        public SequenceBuilder InsertCallback(float at, string callbackId)
            => Insert(at, new SequenceCallback(callbackId));

        public SequenceBuilder Loop(LoopType loopType = LoopType.PingPong, uint count = 0)
        {
            if (loopType == LoopType.Random)
            {
                Debug.LogWarning("Sequence does not support LoopType.Random yet; falling back to Repeat.");
                loopType = LoopType.Repeat;
            }

            _loop.Type = loopType;
            _loop.Count = count;
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

        public SequenceBuilder DynamicTime(bool enabled = true)
        {
            _dynamicTime = enabled;
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
            where TAdapter : IEntityCommandAdapter
        {
            EnsureState();

            var sequenceEntity = CanReusePreparedSequence(adapter)
                ? _preparedSequenceEntity
                : adapter.CreateEntity();

            ConfigureSequenceEntity(sequenceEntity, adapter);

            if (!HasReusablePreparedBuffer(adapter, sequenceEntity))
            {
                var runtimeElements = new List<SequenceElement>(_elements.Count);
                for (int i = 0; i < _elements.Count; i++)
                {
                    var element = _elements[i];
                    if (element.Kind is TimelineActionKind.Tween or TimelineActionKind.Chase)
                    {
                        element.ActionEntity = _actions[i].CreateEntity(sequenceEntity, adapter);
                        _elements[i] = element;
                    }
                    runtimeElements.Add(element);
                }

                var buffer = adapter.AddBuffer<SequenceElement>(sequenceEntity);
                for (int i = 0; i < runtimeElements.Count; i++)
                {
                    buffer.Add(runtimeElements[i]);
                }
            }
            return sequenceEntity;
        }

        private void ConfigureSequenceEntity<TAdapter>(Entity sequenceEntity, TAdapter adapter)
            where TAdapter : IEntityCommandAdapter
        {
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
                NormalizedTime = 0f,
                LoopType = _loop.Type,
                LoopCount = _loop.Count,
                LoopIndex = 0,
                LoopEaseMode = _loop.EaseMode
            });

            if (_dynamicTime)
                adapter.AddComponent(sequenceEntity, new SequenceDynamicTime());
        }

        private void AddAction<TAction>(TAction action, float startTime) where TAction : ISequenceActionBlueprint
        {
            EnsureState();


            var element = new SequenceElement
            {
                Kind = action.Kind,
                StartTime = math.max(0f, startTime),
                Duration = math.max(0f, action.Duration),
                CallbackId = action.CallbackId
            };

            if (_preparedSequenceEntity != Entity.Null && _preparedAdapter != null)
            {
                if (element.Kind is TimelineActionKind.Tween or TimelineActionKind.Chase)
                    element.ActionEntity = action.CreateEntity(_preparedSequenceEntity, _preparedAdapter);
                _preparedAdapter.AppendToBuffer(_preparedSequenceEntity, element);
            }

            _elements.Add(element);
            _actions.Add(action);
        }

        private void Prepare<TAdapter>(TAdapter adapter)
            where TAdapter : struct, IEntityCommandAdapter
        {
            if (_preparedSequenceEntity != Entity.Null) return;

            _preparedAdapter = adapter;
            _preparedSequenceEntity = adapter.CreateEntity();
            adapter.AddComponent(_preparedSequenceEntity, new Sequence
            {
                State = PlaybackState.Paused,
                TimeScale = _timeScale <= 0f ? 1f : _timeScale,
                Time = 0f,
                Duration = 0f,
                Direction = 1
            });
            adapter.AddComponent(_preparedSequenceEntity, new PlaybackProgress
            {
                TimeType = _timeType,
                NormalizedTime = 0f
            });
            adapter.AddBuffer<SequenceElement>(_preparedSequenceEntity);
        }

        private bool CanReusePreparedSequence<TAdapter>(TAdapter adapter)
            where TAdapter : IEntityCommandAdapter
        {
            return _preparedSequenceEntity != Entity.Null &&
                   _preparedAdapter != null &&
                   _preparedAdapter.World != null &&
                   adapter.World != null &&
                   _preparedAdapter.World == adapter.World;
        }

        private bool HasReusablePreparedBuffer<TAdapter>(TAdapter adapter, Entity sequenceEntity)
            where TAdapter : IEntityCommandAdapter
        {
            if (!CanReusePreparedSequence(adapter))
                return false;

            var world = _preparedAdapter.World;
            if (world == null || !world.IsCreated)
                return false;

            var em = world.EntityManager;
            return em.Exists(sequenceEntity) && em.HasBuffer<SequenceElement>(sequenceEntity);
        }

        private void EnsureState()
        {
            _elements ??= new List<SequenceElement>();
            _actions ??= new List<ISequenceActionBlueprint>();
            if (_timeScale <= 0f) _timeScale = 1f;
        }
    }
}
