using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using XO.Curve;

namespace XO.Entityween
{
    public sealed class TweenBlueprint<T> where T : unmanaged
    {
        public World World;
        public Entity Entity;
        public TweenType TweenType { get; set; }
        public EaseType EaseType { get; set; }

        public float StartTime { get; set; }
        public float SecondsToPlay { get; set; }

        public bool IsLoop { get; set; }
        public uint LoopCount { get; set; }
        public LoopType LoopType { get; set; }

        public T StartPoint { get; set; }
        public T EndPoint { get; set; }

        public bool IsSpline { get; set; }
        public bool DisposeSpline { get; set; }
        public BlobAssetReference<Spline.SplineBlob<T>> Spline { get; set; }

        public FunctionPointer<TweenCallback>? OnStart;
        public FunctionPointer<TweenCallback>? OnUpdate;
        public FunctionPointer<TweenCallback>? OnComplete;
        public FunctionPointer<TweenCallback>? OnLoop;

        public bool Error;
    }

    [BurstCompile]
    public static class TweenBlueprintExtensions
    {
        public static TweenBlueprint<float3> MoveTo(this Entity entity, float time, World world)
        {
            var components = new ComponentType[] { typeof(LocalTransform) };
            var hasProperComponents = entity.CheckEntityHasProperComponents(world.EntityManager, components);
            if (!hasProperComponents)
            {
            }

            var blueprint = new TweenBlueprint<float3>
            {
                World = world,
                Entity = entity,
                TweenType = TweenType.MoveTo,
                EaseType = EaseType.Linear,
                SecondsToPlay = time,
                DisposeSpline = false,
                IsSpline = false,
                IsLoop = false,
                StartPoint = entity.GetEntityPositionWorld(world.EntityManager)
            };
            return blueprint;
        }

        public static TweenBlueprint<float3> MoveToWorld(this Entity entity, float time, World world)
        {
            var components = new ComponentType[] { typeof(LocalTransform) };
            var hasProperComponents = entity.CheckEntityHasProperComponents(world.EntityManager, components);
            if (!hasProperComponents)
            {
            }

            var blueprint = new TweenBlueprint<float3>
            {
                World = world,
                Entity = entity,
                TweenType = TweenType.MoveToWorld,
                EaseType = EaseType.Linear,
                SecondsToPlay = time,
                DisposeSpline = false,
                IsSpline = false,
                IsLoop = false,
                StartPoint = entity.GetEntityPositionWorld(world.EntityManager)
            };
            return blueprint;
        }

        public static TweenBlueprint<quaternion> RotateTo(this Entity entity, float time, World world)
        {
            var components = new ComponentType[] { typeof(LocalTransform) };
            var hasProperComponents = entity.CheckEntityHasProperComponents(world.EntityManager, components);
            if (!hasProperComponents)
            {
            }

            var blueprint = new TweenBlueprint<quaternion>
            {
                World = world,
                Entity = entity,
                TweenType = TweenType.RotateTo,
                EaseType = EaseType.Linear,
                SecondsToPlay = time,
                DisposeSpline = false,
                IsSpline = false,
                IsLoop = false,
                StartPoint = entity.GetEntityRotationWorld(world.EntityManager)
            };
            return blueprint;
        }

        public static TweenBlueprint<quaternion> RotateToWorld(this Entity entity, float time, World world)
        {
            var components = new ComponentType[] { typeof(LocalTransform) };
            var hasProperComponents = entity.CheckEntityHasProperComponents(world.EntityManager, components);
            if (!hasProperComponents)
            {
            }

            var blueprint = new TweenBlueprint<quaternion>
            {
                World = world,
                Entity = entity,
                TweenType = TweenType.RotateToWorld,
                EaseType = EaseType.Linear,
                SecondsToPlay = time,
                DisposeSpline = false,
                IsSpline = false,
                IsLoop = false,
                StartPoint = entity.GetEntityRotationWorld(world.EntityManager)
            };
            return blueprint;
        }

        private static TweenBlueprint<float3> ScaleTo(this Entity entity, float time, World world)
        {
            var components = new ComponentType[] { typeof(LocalTransform), typeof(PostTransformMatrix) };
            var hasProperComponents = entity.CheckEntityHasProperComponents(world.EntityManager, components);
            if (!hasProperComponents)
            {
                Debug.Log(
                    "You might missing PostTransformMatrix component. If you want to scale the entity uniform way you might want to use ScaleUniform function instead. Otherwise adding PostTransformMatrix component to the entity might solve the issue.");
            }

            var blueprint = new TweenBlueprint<float3>
            {
                World = world,
                Entity = entity,
                TweenType = TweenType.ScaleTo,
                EaseType = EaseType.Linear,
                SecondsToPlay = time,
                DisposeSpline = false,
                IsSpline = false,
                IsLoop = false,
                StartPoint = entity.GetEntityScale(world.EntityManager)
            };
            return blueprint;
        }

        public static TweenBlueprint<float> ScaleToUniform(this Entity entity, float time, World world)
        {
            var components = new ComponentType[] { typeof(LocalTransform) };
            var hasProperComponents = entity.CheckEntityHasProperComponents(world.EntityManager, components);
            if (!hasProperComponents)
            {
            }

            var blueprint = new TweenBlueprint<float>
            {
                World = world,
                TweenType = TweenType.ScaleToUniform,
                EaseType = EaseType.Linear,
                SecondsToPlay = time,
                DisposeSpline = false,
                IsSpline = false,
                IsLoop = false,
                StartPoint = entity.GetEntityScaleUniform(world.EntityManager)
            };
            return blueprint;
        }

        public static TweenBlueprint<float> Destination(this TweenBlueprint<float> blueprint, float target)
        {
            blueprint.EndPoint = target;
            return blueprint;
        }

        public static TweenBlueprint<float2> Destination(this TweenBlueprint<float2> blueprint, float2 target)
        {
            blueprint.EndPoint = target;
            return blueprint;
        }

        public static TweenBlueprint<float3> Destination(this TweenBlueprint<float3> blueprint, float3 target)
        {
            blueprint.EndPoint = target;
            return blueprint;
        }

        public static TweenBlueprint<quaternion> Destination(this TweenBlueprint<quaternion> blueprint,
            quaternion target)
        {
            blueprint.EndPoint = target;
            return blueprint;
        }

        public static TweenBlueprint<float> Destination(this TweenBlueprint<float> blueprint,
            NativeArray<float> destination, SplineType splineType)
        {
            if (!IsSplineAllowed(blueprint.TweenType))
            {
                return blueprint;
            }

            if (blueprint.IsSpline && blueprint.DisposeSpline)
                blueprint.Spline.Dispose();

            blueprint.IsSpline = true;
            blueprint.DisposeSpline = true;

            blueprint.Spline = Spline.CreateSplineBlob(splineType, false, true, destination);
            return blueprint;
        }

        public static TweenBlueprint<float2> Destination(this TweenBlueprint<float2> blueprint,
            NativeArray<float2> destination, SplineType splineType)
        {
            if (!IsSplineAllowed(blueprint.TweenType))
            {
                return blueprint;
            }

            if (blueprint.IsSpline && blueprint.DisposeSpline)
                blueprint.Spline.Dispose();

            blueprint.IsSpline = true;
            blueprint.DisposeSpline = true;

            blueprint.Spline = Spline.CreateSplineBlob(splineType, false, true, destination);
            return blueprint;
        }

        public static TweenBlueprint<float3> Destination(this TweenBlueprint<float3> blueprint,
            NativeArray<float3> destination, SplineType splineType)
        {
            if (!IsSplineAllowed(blueprint.TweenType))
            {
                return blueprint;
            }

            if (blueprint.IsSpline && blueprint.DisposeSpline)
                blueprint.Spline.Dispose();

            blueprint.IsSpline = true;
            blueprint.DisposeSpline = true;

            blueprint.Spline = Spline.CreateSplineBlob(splineType, false, true, destination);
            return blueprint;
        }

        public static TweenBlueprint<quaternion> Destination(this TweenBlueprint<quaternion> blueprint,
            NativeArray<quaternion> destination)
        {
            if (!IsSplineAllowed(blueprint.TweenType))
            {
                return blueprint;
            }

            if (blueprint.IsSpline && blueprint.DisposeSpline)
                blueprint.Spline.Dispose();

            blueprint.IsSpline = true;
            blueprint.DisposeSpline = true;

            blueprint.Spline = Spline.CreateSplineBlob(SplineType.CubicBezier, false, true, destination);
            return blueprint;
        }

        private static bool IsSplineAllowed(TweenType tweenType)
        {
            var state = tweenType is not (TweenType.None or TweenType.Wait or TweenType.Callback);
            if (!state)
            {
                Debug.LogWarning("Tween Type " + tweenType + " is not suitable for splines.");
            }

            return state;
        }

        public static TweenBlueprint<float> Ease(this TweenBlueprint<float> blueprint, EaseType easeType)
        {
            blueprint.EaseType = easeType;
            return blueprint;
        }

        public static TweenBlueprint<float2> Ease(this TweenBlueprint<float2> blueprint, EaseType easeType)
        {
            blueprint.EaseType = easeType;
            return blueprint;
        }

        public static TweenBlueprint<float3> Ease(this TweenBlueprint<float3> blueprint, EaseType easeType)
        {
            blueprint.EaseType = easeType;
            return blueprint;
        }

        public static TweenBlueprint<quaternion> Ease(this TweenBlueprint<quaternion> blueprint, EaseType easeType)
        {
            blueprint.EaseType = easeType;
            return blueprint;
        }


        public static TweenBlueprint<float> OnStart(this TweenBlueprint<float> blueprint, TweenCallback func)
        {
            blueprint.OnStart = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<float2> OnStart(this TweenBlueprint<float2> blueprint, TweenCallback func)
        {
            blueprint.OnStart = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<float3> OnStart(this TweenBlueprint<float3> blueprint, TweenCallback func)
        {
            blueprint.OnStart = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<quaternion> OnStart(this TweenBlueprint<quaternion> blueprint, TweenCallback func)
        {
            blueprint.OnStart = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<float> OnUpdate(this TweenBlueprint<float> blueprint, TweenCallback func)
        {
            blueprint.OnUpdate = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<float2> OnUpdate(this TweenBlueprint<float2> blueprint, TweenCallback func)
        {
            blueprint.OnUpdate = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<float3> OnUpdate(this TweenBlueprint<float3> blueprint, TweenCallback func)
        {
            blueprint.OnUpdate = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<quaternion> OnUpdate(this TweenBlueprint<quaternion> blueprint, TweenCallback func)
        {
            blueprint.OnUpdate = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<float> OnComplete(this TweenBlueprint<float> blueprint, TweenCallback func)
        {
            blueprint.OnComplete = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<float2> OnComplete(this TweenBlueprint<float2> blueprint, TweenCallback func)
        {
            blueprint.OnComplete = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<float3> OnComplete(this TweenBlueprint<float3> blueprint, TweenCallback func)
        {
            blueprint.OnComplete = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<quaternion> OnComplete(this TweenBlueprint<quaternion> blueprint,
            TweenCallback func)
        {
            blueprint.OnComplete = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<float> Loop(this TweenBlueprint<float> blueprint,
            LoopType loopTypeType = LoopType.PingPong, uint loopCount = 1)
        {
            blueprint.IsLoop = true;
            blueprint.LoopCount = loopCount;
            blueprint.LoopType = loopTypeType;
            return blueprint;
        }

        public static TweenBlueprint<float2> Loop(this TweenBlueprint<float2> blueprint,
            LoopType loopTypeType = LoopType.PingPong, uint loopCount = 1)
        {
            blueprint.IsLoop = true;
            blueprint.LoopCount = loopCount;
            blueprint.LoopType = loopTypeType;
            return blueprint;
        }

        public static TweenBlueprint<float3> Loop(this TweenBlueprint<float3> blueprint,
            LoopType loopTypeType = LoopType.PingPong, uint loopCount = 1)
        {
            blueprint.IsLoop = true;
            blueprint.LoopCount = loopCount;
            blueprint.LoopType = loopTypeType;
            return blueprint;
        }

        public static TweenBlueprint<quaternion> Loop(this TweenBlueprint<quaternion> blueprint,
            LoopType loopTypeType = LoopType.PingPong, uint loopCount = 1)
        {
            blueprint.IsLoop = true;
            blueprint.LoopCount = loopCount;
            blueprint.LoopType = loopTypeType;
            return blueprint;
        }

        public static TweenBlueprint<float> OnLoop(this TweenBlueprint<float> blueprint, TweenCallback func)
        {
            blueprint.OnLoop = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<float2> OnLoop(this TweenBlueprint<float2> blueprint, TweenCallback func)
        {
            blueprint.OnLoop = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<float3> OnLoop(this TweenBlueprint<float3> blueprint, TweenCallback func)
        {
            blueprint.OnLoop = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static TweenBlueprint<quaternion> OnLoop(this TweenBlueprint<quaternion> blueprint, TweenCallback func)
        {
            blueprint.OnLoop = BurstCompiler.CompileFunctionPointer<TweenCallback>(func);
            return blueprint;
        }

        public static void Play(this TweenBlueprint<float> blueprint, EntityCommandBuffer ecb)
        {
            if (blueprint.Error)
            {
                Debug.LogError("Corrupted Tween play attempt.");
                return;
            }

            blueprint.StartTime = (float)blueprint.World.Time.ElapsedTime;
            var handle = blueprint.World.GetOrCreateSystemManaged<TweenHandlingSystem>();
            handle.AttachTween(blueprint, ecb);
        }

        public static void Play(this TweenBlueprint<float2> blueprint, EntityCommandBuffer ecb)
        {
            if (blueprint.Error)
            {
                Debug.LogError("Corrupted Tween play attempt.");
                return;
            }

            blueprint.StartTime = (float)blueprint.World.Time.ElapsedTime;
            var handle = blueprint.World.GetOrCreateSystemManaged<TweenHandlingSystem>();
            handle.AttachTween(blueprint, ecb);
        }

        public static void Play(this TweenBlueprint<float3> blueprint, EntityCommandBuffer ecb)
        {
            if (blueprint.Error)
            {
                Debug.LogError("Corrupted Tween play attempt.");
                return;
            }

            blueprint.StartTime = (float)blueprint.World.Time.ElapsedTime;
            var handle = blueprint.World.GetOrCreateSystemManaged<TweenHandlingSystem>();
            handle.AttachTween(blueprint, ecb);
        }

        public static void Play(this TweenBlueprint<quaternion> blueprint, EntityCommandBuffer ecb)
        {
            if (blueprint.Error)
            {
                Debug.LogError("Corrupted Tween play attempt.");
                return;
            }

            blueprint.StartTime = (float)blueprint.World.Time.ElapsedTime;
            var handle = blueprint.World.GetOrCreateSystemManaged<TweenHandlingSystem>();
            handle.AttachTween(blueprint, ecb);
        }

        private static float3 GetEntityPositionLocal(this Entity entity, EntityManager entityManager)
        {
            return entityManager.GetComponentData<LocalTransform>(entity).Position;
        }

        private static float3 GetEntityPositionWorld(this Entity entity, EntityManager entityManager)
        {
            if (entityManager.HasComponent<Parent>(entity))
            {
                if (entityManager.HasComponent<LocalToWorld>(entity))
                {
                    return entityManager.GetComponentData<LocalToWorld>(entity).Position;
                }

                Debug.LogWarning("Entity has Parent but not LocalToWorld component.");
            }

            return entityManager.GetComponentData<LocalTransform>(entity).Position;
        }

        private static quaternion GetEntityRotationLocal(this Entity entity, EntityManager entityManager)
        {
            return entityManager.GetComponentData<LocalTransform>(entity).Rotation;
        }

        private static quaternion GetEntityRotationWorld(this Entity entity, EntityManager entityManager)
        {
            if (entityManager.HasComponent<Parent>(entity))
            {
                if (entityManager.HasComponent<LocalToWorld>(entity))
                {
                    return entityManager.GetComponentData<LocalToWorld>(entity).Rotation;
                }

                Debug.LogWarning("Entity has Parent but not LocalToWorld component.");
            }

            return entityManager.GetComponentData<LocalTransform>(entity).Rotation;
        }

        private static float3 GetEntityScale(this Entity entity, EntityManager entityManager)
        {
            var scale = float3.zero;
            var matrix = (entityManager.GetComponentData<PostTransformMatrix>(entity).Value);
            GetScaleFromMatrix(in matrix, ref scale);

            return scale;
        }
        
        private static float GetEntityScaleUniform(this Entity entity, EntityManager entityManager)
        {
            return entityManager.GetComponentData<LocalTransform>(entity).Scale;
        }

        private static bool CheckEntityHasProperComponents(this Entity entity, EntityManager entityManager,
            ComponentType[] components)
        {
            var state = true;
            foreach (var component in components)
            {
                if (entityManager.HasComponent(entity, component)) continue;

                Debug.LogError(
                    "The entity which you are trying to assign a tween doesnt have component { " + component + " }.");
                state = false;
            }

            return state;
        }
        [BurstCompile]
        private static void GetScaleFromMatrix(in float4x4 matrix,ref float3 result)
        {
            var scaleX = math.length(matrix.c0.xyz); // Right vector
            var scaleY = math.length(matrix.c1.xyz); // Up vector
            var scaleZ = math.length(matrix.c2.xyz); // Forward vector
            result = new float3(scaleX, scaleY, scaleZ);
        }
    }

    public enum TweenPointType
    {
        None,
        Float,
        Float2,
        Float3,
        Quaternion,
    }

    public enum LoopType
    {
        None,
        Repeat,
        PingPong,
        Random
    }

    public enum TweenType
    {
        None,
        Wait,
        Callback,

        //Move,
        //MoveLocal,
        MoveTo, //Float3
        MoveToWorld, //Float3

        //Rotate, //quaternion
        //RotateLocal, //quaternion
        RotateTo, //quaternion
        RotateToWorld, //quaternion

        //Scale, //Float3
        ScaleTo, //Float3

        //ScaleUniform, //Float
        ScaleToUniform, //Float

        FloatTo, //Float
        Float2To, //Float2
        Float3To, //Float3
        QuaternionTo, //quaternion
    }
}