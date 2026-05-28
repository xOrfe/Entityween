using Unity.Mathematics;
using UnityEngine;
using XO.Curve;
using XO.Entityween;

namespace Entityween.Samples
{
    public enum ShowcasePreset
    {
        MoveLocal,
        MoveWorld,
        MoveWithChase,
        RotateWorld,
        RotateLocal,
        ScalePingPong,
        ScaleUniform,
        SplinePath,
        ChaseTarget,
        ChasePositionAndRotation,
        ChasePositionAndLook,
        SequenceShowcase,
        LookAtTarget
    }

    public abstract class BaseShowcaseItem : MonoBehaviour
    {
        [Header("Showcase Text")]
        [Tooltip("The description text displayed floating above the object.")]
        public string description;

        [Header("Tween Configuration")]
        [Tooltip("The preset animation behavior for this showcase item.")]
        public ShowcasePreset preset;
        public float duration = 2.0f;
        public EaseType ease = EaseType.InOutSine;
        public LoopType loop = LoopType.PingPong;

        [Header("Preset Details")]
        [Tooltip("Offset for MoveLocal and Sequence presets.")]
        public float3 moveOffset = new float3(0f, 3f, 0f);

        [Tooltip("Euler destination for rotation presets.")]
        public float3 rotationDegrees = new float3(0f, 178f, 0f);

        [Tooltip("Destination scale for scale presets.")]
        public float3 scaleTarget = new float3(1.8f);

        [Tooltip("Uniform scale destination for ScaleUniform.")]
        public float uniformScaleTarget = 2f;

        [Tooltip("Spline path configuration (used for SplinePath preset).")]
        public SerializableSpline<float3> splinePath = new SerializableSpline<float3>();

        [Tooltip("The target GameObject to chase (used for ChaseTarget preset).")]
        public GameObject chaseTarget;
        public float chaseSmoothTime = 0.3f;

        [Tooltip("The target GameObject to look at (used for LookAtTarget preset).")]
        public GameObject lookTarget;
        public float lookSmoothTime = 0.15f;
    }
}
