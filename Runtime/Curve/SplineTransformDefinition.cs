using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace XO.Curve
{
    [CreateAssetMenu(fileName = "SplineTransform", menuName = "XO/Entityween/SplineTransformDefinition", order = 0)]
    public class SplineTransformDefinition : ScriptableObject
    {
        public float duration;
        public EaseType easeType;
        
        [TabGroup("SplinePoints","Positional",TextColor = "orange")]
        public bool positionalEnabled = true;
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
        [TabGroup("SplinePoints","Positional",TextColor = "orange")]
        [InlineProperty, HideLabel,ShowIf("positionalEnabled")]
        public Spline.SplineData<float3> positional;
        
        [TabGroup("SplinePoints","Rotational",TextColor = "orange")]
        public bool rotationalEnabled = true;
        [PropertySpace(SpaceBefore = 10, SpaceAfter = 10)]
        [TabGroup("SplinePoints","Rotational",TextColor = "orange")]
        [InlineProperty, HideLabel,ShowIf("rotationalEnabled")]
        public Spline.SplineData<quaternion> rotational;
    }
    
    public readonly struct SplineTransform
    {
        public readonly float Duration;
        public readonly EaseType EaseType;
        public readonly BlobAssetReference<Spline.SplineBlob<float3>> Positional;
        public readonly BlobAssetReference<Spline.SplineBlob<quaternion>> Rotational;
        
        public SplineTransform(SplineTransformDefinition definition)
        {
            Duration = definition.duration;
            EaseType = definition.easeType;
            Positional = Spline.CreateSplineBlob<float3>(definition.positional);
            Rotational = Spline.CreateSplineBlob<quaternion>(definition.rotational);
        }
        
        public readonly void Dispose()
        {
            Positional.Dispose();
            Rotational.Dispose();
        }
    }
}