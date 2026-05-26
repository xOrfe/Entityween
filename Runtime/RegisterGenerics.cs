using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;
using XO.Curve;
using XO.Entityween;

[assembly: RegisterGenericComponentType(typeof(TweenValue<float>))]
[assembly: RegisterGenericComponentType(typeof(TweenValue<float2>))]
[assembly: RegisterGenericComponentType(typeof(TweenValue<float3>))]
[assembly: RegisterGenericComponentType(typeof(TweenValue<quaternion>))]

[assembly: RegisterGenericComponentType(typeof(SplineElement<float>))]
[assembly: RegisterGenericComponentType(typeof(SplineElement<float2>))]
[assembly: RegisterGenericComponentType(typeof(SplineElement<float3>))]
[assembly: RegisterGenericComponentType(typeof(SplineElement<quaternion>))]

[assembly: RegisterGenericComponentType(typeof(SplineBlobRef<float>))]
[assembly: RegisterGenericComponentType(typeof(SplineBlobRef<float2>))]
[assembly: RegisterGenericComponentType(typeof(SplineBlobRef<float3>))]
[assembly: RegisterGenericComponentType(typeof(SplineBlobRef<quaternion>))]


[assembly: RegisterGenericComponentType(typeof(TweenMemberHook<float>))]
[assembly: RegisterGenericComponentType(typeof(TweenMemberHook<float2>))]
[assembly: RegisterGenericComponentType(typeof(TweenMemberHook<float3>))]
[assembly: RegisterGenericComponentType(typeof(TweenMemberHook<quaternion>))]

[assembly: RegisterGenericComponentType(typeof(TweenCallbackHook<float>))]
[assembly: RegisterGenericComponentType(typeof(TweenCallbackHook<float2>))]
[assembly: RegisterGenericComponentType(typeof(TweenCallbackHook<float3>))]
[assembly: RegisterGenericComponentType(typeof(TweenCallbackHook<quaternion>))]

namespace XO.Entityween
{
    internal static class EntityweenAotPreserve
    {
        [Preserve]
        private static void UsedOnlyForAOTCodeGeneration()
        {
            TweenMemberBinder.AotTouch<float>();
            TweenMemberBinder.AotTouch<float2>();
            TweenMemberBinder.AotTouch<float3>();
            TweenMemberBinder.AotTouch<quaternion>();

            throw new System.InvalidOperationException(
                "This method is used only for AOT code generation.");
        }
    }
}
