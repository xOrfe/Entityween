using Unity.Entities;
using UnityEngine;

namespace XO.Entityween
{
    public struct EntityweenSettingsComponent : IComponentData
    {
        public float DefaultDuration;
        public bool EnableLogs;
    }

    [DisallowMultipleComponent]
    public class EntityweenSettingsAuthoring : MonoBehaviour
    {
        internal class Baker : Baker<EntityweenSettingsAuthoring>
        {
            public override void Bake(EntityweenSettingsAuthoring authoring)
            {
                var settings = EntityweenSettings.Instance;
                if (settings == null) return;

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new EntityweenSettingsComponent
                {
                    DefaultDuration = settings.DefaultDuration,
                    EnableLogs = settings.EnableLogs
                });
            }
        }
    }
}
