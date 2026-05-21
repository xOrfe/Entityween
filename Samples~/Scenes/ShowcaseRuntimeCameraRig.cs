using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    public class ShowcaseRuntimeCameraRig : MonoBehaviour
    {
        [SerializeField] private float orbitDuration = 42f;
        [SerializeField] private float focusHoldDuration = 4f;
        [SerializeField] private float focusTransitionDuration = 1.4f;
        [SerializeField] private float lookSmooth = 7f;

        private BlobAssetReference<SplineBlob<float3>> cameraPathBlob;

        private static readonly float3[] CameraPath =
        {
            new float3(0f, 20f, -48f),
            new float3(-32f, 18f, -38f),
            new float3(-48f, 20f, -4f),
            new float3(-36f, 17f, 30f),
            new float3(0f, 21f, 42f),
            new float3(36f, 17f, 30f),
            new float3(48f, 20f, -4f),
            new float3(32f, 18f, -38f)
        };

        private static readonly float3[] FocusPath =
        {
            new float3(-36f, 2.6f, 20f),
            new float3(-24f, 2.8f, 20f),
            new float3(-12f, 2.8f, 20f),
            new float3(0f, 2.6f, 20f),
            new float3(12f, 2.7f, 20f),
            new float3(24f, 3f, 20f),
            new float3(36f, 2.8f, 20f),
            new float3(-30f, 5f, 5f),
            new float3(-8f, 5.2f, 5f),
            new float3(15f, 5f, 5f),
            new float3(33f, 3.4f, 5f),
            new float3(-33f, 2.5f, -13f),
            new float3(-10f, 2.8f, -13f),
            new float3(15f, 2.8f, -13f),
            new float3(30f, 2.8f, -16f),
            new float3(-18f, 3.1f, -29f)
        };

        private void OnEnable()
        {
            DisposeCameraPath();
            var cameraSpline = new SerializableSpline<float3>
            {
                splineType = SplineType.CatmullRom,
                isClosed = true,
                points = CameraPath
            };
            cameraSpline.ValidatePoints();
            cameraPathBlob = Spline.CreateSplineBlob<float3, Float3Math>(cameraSpline);
        }

        private void LateUpdate()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null || !cameraPathBlob.IsCreated) return;

            float cameraT = Mathf.Repeat(Time.time / math.max(0.1f, orbitDuration), 1f);
            float3 sampledPosition = Spline.Sample(cameraPathBlob, cameraT);
            sampledPosition.y += math.sin(Time.time * 0.85f) * 1.15f;
            Vector3 cameraPosition = sampledPosition;
            mainCamera.transform.position = cameraPosition;

            Vector3 focus = (Vector3)GetFocusPosition();
            Vector3 toFocus = focus - cameraPosition;
            if (toFocus.sqrMagnitude <= 0.0001f) return;

            Quaternion targetRotation = Quaternion.LookRotation(toFocus.normalized, Vector3.up);
            mainCamera.transform.rotation = Quaternion.Slerp(
                mainCamera.transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-lookSmooth * Time.deltaTime));
        }

        private float3 GetFocusPosition()
        {
            float segmentDuration = math.max(0.1f, focusHoldDuration + focusTransitionDuration);
            float totalDuration = segmentDuration * FocusPath.Length;
            float time = Mathf.Repeat(Time.time, totalDuration);
            int index = math.clamp((int)(time / segmentDuration), 0, FocusPath.Length - 1);
            int nextIndex = (index + 1) % FocusPath.Length;
            float localTime = time - index * segmentDuration;

            float3 current = AnimateFocusPoint(FocusPath[index], index);
            if (localTime <= focusHoldDuration)
            {
                return current;
            }

            float t = math.saturate((localTime - focusHoldDuration) / focusTransitionDuration);
            t = Ease.EasedT(t, EaseType.InOutSine);
            return math.lerp(current, AnimateFocusPoint(FocusPath[nextIndex], nextIndex), t);
        }

        private static float3 AnimateFocusPoint(float3 point, int index)
        {
            point.y += math.sin(Time.time * 1.4f + index * 0.73f) * 0.8f;
            if (index == 4)
            {
                point.z += math.sin(Time.time * 1.05f) * 3f;
            }
            else if (index == 5)
            {
                point.x += math.sin(Time.time * 1.25f) * 2.5f;
            }

            return point;
        }

        private void OnDestroy()
        {
            DisposeCameraPath();
        }

        private void OnDisable()
        {
            DisposeCameraPath();
        }

        private void DisposeCameraPath()
        {
            if (cameraPathBlob.IsCreated)
            {
                cameraPathBlob.Dispose();
                cameraPathBlob = default;
            }
        }
    }
}
