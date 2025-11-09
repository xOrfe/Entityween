using Unity.Entities;

namespace XO.Entityween
{
    public partial class EntityweenSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate ()
        {
            base.OnCreate();
            AddSystemToUpdateList(World.CreateSystem<TweenHandlingSystem>());
            AddSystemToUpdateList(World.CreateSystem<ChaseHandlingSystem>());
            AddSystemToUpdateList(World.CreateSystemManaged<UpdateWorldTimeSystem>());
        }
    }
}