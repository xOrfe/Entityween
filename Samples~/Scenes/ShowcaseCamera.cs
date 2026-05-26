using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    public struct ShowcaseCameraTag : IComponentData { }

    public struct ShowcaseCameraLookTarget : IComponentData
    {
        public Entity Value;
    }

    public struct ShowcaseCameraFocusSettings : IComponentData
    {
        public float HoldDuration;
        public float TransitionDuration;
    }

    public struct ShowcaseCameraFocusTarget : IBufferElementData
    {
        public Entity Value;
    }

    public class ShowcaseCameraAuthoring : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The helper GameObject representing the look target.")]
        public GameObject lookTargetObject;

        [Tooltip("List of showcase items the camera will focus on sequentially.")]
        public List<GameObject> showcaseItems = new List<GameObject>();

        [Header("Animation Settings")]
        [Tooltip("Time in seconds for the camera to complete one full loop around the scene.")]
        public float moveDuration = 35f;

        [Tooltip("Time in seconds the camera focuses on a single item before moving to the next.")]
        public float lookHoldDuration = 4f;

        [Tooltip("Time in seconds it takes to transition focus from one item to the next.")]
        public float lookTransitionDuration = 1.5f;
    }

    public class ShowcaseCameraBaker : Baker<ShowcaseCameraAuthoring>
    {
        public override void Bake(ShowcaseCameraAuthoring authoring)
        {
            var cameraEntity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ShowcaseCameraTag>(cameraEntity);

            if (authoring.lookTargetObject == null) return;
            var lookTargetEntity = GetEntity(authoring.lookTargetObject, TransformUsageFlags.Dynamic);
            if (lookTargetEntity == Entity.Null) return;
            AddComponent(cameraEntity, new ShowcaseCameraLookTarget { Value = lookTargetEntity });
            AddComponent(cameraEntity, new ShowcaseCameraFocusSettings
            {
                HoldDuration = math.max(0.1f, authoring.lookHoldDuration),
                TransitionDuration = math.max(0.01f, authoring.lookTransitionDuration)
            });

            var focusTargets = AddBuffer<ShowcaseCameraFocusTarget>(cameraEntity);
            if (authoring.showcaseItems != null)
            {
                foreach (var itemGo in authoring.showcaseItems)
                {
                    if (itemGo == null) continue;
                    var itemEntity = GetEntity(itemGo, TransformUsageFlags.Dynamic);
                    if (itemEntity != Entity.Null)
                    {
                        focusTargets.Add(new ShowcaseCameraFocusTarget { Value = itemEntity });
                    }
                }
            }

            float3[] pathPoints = new float3[]
            {
                new float3(0f, 14f, -28f),
                new float3(-24f, 12f, -12f),
                new float3(-24f, 14f, 18f),
                new float3(0f, 12f, 26f),
                new float3(24f, 14f, 18f),
                new float3(24f, 12f, -12f)
            };

            using (var nativePathPoints = new NativeArray<float3>(pathPoints, Allocator.Temp))
            {
                cameraEntity.MoveToWorld(pathPoints[0], authoring.moveDuration)
                    .Along(nativePathPoints, SplineType.CatmullRom, isClosed: true)
                    .Ease(EaseType.Linear)
                    .Loop(LoopType.Repeat)
                    .Play(this);
            }

            cameraEntity.Look(lookTargetEntity)
                .SmoothDamp(0.6f)
                .Play(this);

            if (authoring.showcaseItems == null || authoring.showcaseItems.Count == 0) return;

            var seq = Sequence.Create();
            for (int i = 0; i < authoring.showcaseItems.Count; i++)
            {
                var itemGo = authoring.showcaseItems[i];
                if (itemGo == null) continue;

                float3 itemPos = itemGo.transform.position;

                if (i == 0)
                {
                    seq.Append(lookTargetEntity.MoveToWorld(itemPos, 0.1f));
                }
                else
                {
                    seq.Append(lookTargetEntity.MoveToWorld(itemPos, authoring.lookTransitionDuration).Ease(EaseType.InOutSine));
                }
                seq.AppendWait(authoring.lookHoldDuration);
            }

            seq.Loop(LoopType.Repeat)
               .Play(this);
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct ShowcaseCameraSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (Camera.main == null) return;
            if (Object.FindFirstObjectByType<ShowcaseRuntimeCameraRig>() != null) return;

            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            foreach (var (transform, settings, focusTargets, lookTarget) in SystemAPI
                         .Query<RefRO<LocalToWorld>, RefRO<ShowcaseCameraFocusSettings>, DynamicBuffer<ShowcaseCameraFocusTarget>, RefRO<ShowcaseCameraLookTarget>>()
                         .WithAll<ShowcaseCameraTag>())
            {
                var cameraPosition = (Vector3)transform.ValueRO.Position;
                Camera.main.transform.position = cameraPosition;

                bool hasTarget = TryGetFocusPosition(focusTargets, settings.ValueRO, localToWorldLookup, out var targetPosition);
                if (!hasTarget && localToWorldLookup.TryGetComponent(lookTarget.ValueRO.Value, out var targetTransform))
                {
                    targetPosition = targetTransform.Position;
                    hasTarget = true;
                }

                if (!hasTarget)
                {
                    Camera.main.transform.rotation = transform.ValueRO.Rotation;
                    continue;
                }

                var toTarget = (Vector3)(targetPosition - transform.ValueRO.Position);
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    Camera.main.transform.rotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                }
            }
        }

        private static bool TryGetFocusPosition(
            DynamicBuffer<ShowcaseCameraFocusTarget> focusTargets,
            ShowcaseCameraFocusSettings settings,
            ComponentLookup<LocalToWorld> localToWorldLookup,
            out float3 position)
        {
            position = default;
            if (focusTargets.Length == 0) return false;

            float segmentDuration = math.max(0.1f, settings.HoldDuration + settings.TransitionDuration);
            float totalDuration = segmentDuration * focusTargets.Length;
            float time = totalDuration > 0f ? Mathf.Repeat(Time.time, totalDuration) : 0f;
            int index = math.clamp((int)(time / segmentDuration), 0, focusTargets.Length - 1);
            float localTime = time - index * segmentDuration;
            int nextIndex = (index + 1) % focusTargets.Length;

            if (!localToWorldLookup.TryGetComponent(focusTargets[index].Value, out var currentTransform))
            {
                return false;
            }

            position = currentTransform.Position;
            if (localTime <= settings.HoldDuration || focusTargets.Length == 1)
            {
                return true;
            }

            if (localToWorldLookup.TryGetComponent(focusTargets[nextIndex].Value, out var nextTransform))
            {
                float t = math.saturate((localTime - settings.HoldDuration) / settings.TransitionDuration);
                t = t * t * (3f - 2f * t);
                position = math.lerp(position, nextTransform.Position, t);
            }

            return true;
        }
    }
}
