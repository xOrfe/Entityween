using Unity.Entities;


namespace XO.Entityween
{
    public class EntityweenWorldBootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            return false;
            
            var world = new World("Entityween", WorldFlags.Game | WorldFlags.Editor | WorldFlags.Live);
            
            var initGroup = world.CreateSystemManaged<InitializationSystemGroup>();
            var simGroup  = world.CreateSystemManaged<SimulationSystemGroup>();
            var presGroup = world.CreateSystemManaged<PresentationSystemGroup>();
            var entityweenGroup = world.CreateSystemManaged<EntityweenSystemGroup>();
            simGroup.AddSystemToUpdateList(entityweenGroup);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);
            
            return false;
        }
    }
}