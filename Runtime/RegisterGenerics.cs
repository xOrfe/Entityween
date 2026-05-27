using Unity.Entities;
using Unity.Mathematics;
using XO.Curve;
using XO.Entityween;

[assembly: RegisterGenericComponentType(typeof(TweenRange<float>))]
[assembly: RegisterGenericComponentType(typeof(TweenRange<float2>))]
[assembly: RegisterGenericComponentType(typeof(TweenRange<float3>))]
[assembly: RegisterGenericComponentType(typeof(TweenRange<quaternion>))]

[assembly: RegisterGenericComponentType(typeof(TweenRuntime<float>))]
[assembly: RegisterGenericComponentType(typeof(TweenRuntime<float2>))]
[assembly: RegisterGenericComponentType(typeof(TweenRuntime<float3>))]
[assembly: RegisterGenericComponentType(typeof(TweenRuntime<quaternion>))]

[assembly: RegisterGenericComponentType(typeof(SplineElement<float>))]
[assembly: RegisterGenericComponentType(typeof(SplineElement<float2>))]
[assembly: RegisterGenericComponentType(typeof(SplineElement<float3>))]
[assembly: RegisterGenericComponentType(typeof(SplineElement<quaternion>))]

[assembly: RegisterGenericComponentType(typeof(SplineBlobRef<float>))]
[assembly: RegisterGenericComponentType(typeof(SplineBlobRef<float2>))]
[assembly: RegisterGenericComponentType(typeof(SplineBlobRef<float3>))]
[assembly: RegisterGenericComponentType(typeof(SplineBlobRef<quaternion>))]

