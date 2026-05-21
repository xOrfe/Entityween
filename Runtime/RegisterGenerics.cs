using Unity.Entities;
using Unity.Mathematics;
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
