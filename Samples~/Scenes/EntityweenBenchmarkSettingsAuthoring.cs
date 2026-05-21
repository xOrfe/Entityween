using Unity.Entities;
using UnityEngine;

namespace Entityween.Samples
{
    public class EntityweenBenchmarkSettingsAuthoring : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("The prefab that will be spawned in the stress test.")]
        public GameObject prefab;
    }

    public class EntityweenBenchmarkSettingsBaker : Baker<EntityweenBenchmarkSettingsAuthoring>
    {
        public override void Bake(EntityweenBenchmarkSettingsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BenchmarkSettings
            {
                Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic)
            });
        }
    }
}
