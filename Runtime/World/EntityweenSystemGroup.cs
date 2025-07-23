using Unity.Entities;

namespace XO.Entityween
{
    public partial class EntityweenSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate ()
        {
            base.OnCreate();
            AddSystemToUpdateList(World.CreateSystem<TweenHandlingSystem>());
            AddSystemToUpdateList(World.CreateSystemManaged<UpdateWorldTimeSystem>());
        }
    }
}