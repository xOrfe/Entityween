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
    
    public struct SplineTransform
    {
        public readonly float duration;
        public readonly EaseType easeType;
        public readonly BlobAssetReference<Spline.SplineBlob<float3>> positional;
        public readonly BlobAssetReference<Spline.SplineBlob<quaternion>> rotational;
        
        public SplineTransform(SplineTransformDefinition definition)
        {
            duration = definition.duration;
            easeType = definition.easeType;
            positional = Spline.CreateSplineBlob<float3>(definition.positional);
            rotational = Spline.CreateSplineBlob<quaternion>(definition.rotational);
        }
        
        public readonly void Dispose()
        {
            positional.Dispose();
            rotational.Dispose();
        }
    }
}