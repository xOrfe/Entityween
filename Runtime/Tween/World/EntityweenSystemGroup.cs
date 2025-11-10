using Unity.Entities;
using Unity.Transforms;

namespace XO.Entityween
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(LateSimulationSystemGroup))]
    public partial class EntityweenSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate ()
        {
            base.OnCreate();
            AddSystemToUpdateList(World.CreateSystemManaged<UpdateWorldTimeSystem>());
            AddSystemToUpdateList(World.CreateSystem<TweenHandlingSystem>());
            AddSystemToUpdateList(World.CreateSystem<ChaseHandlingSystem>());
        }
    }
}