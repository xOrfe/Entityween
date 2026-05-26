using Unity.Entities;

namespace XO.Entityween
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    internal partial class EntityweenSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(EntityweenSystemGroup))]
    internal partial class EntityweenSequenceGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(EntityweenSystemGroup))]
    [UpdateAfter(typeof(EntityweenSequenceGroup))]
    internal partial class EntityweenTweenGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(EntityweenSystemGroup))]
    [UpdateAfter(typeof(EntityweenTweenGroup))]
    internal partial class EntityweenChaseGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(EntityweenSystemGroup))]
    [UpdateAfter(typeof(EntityweenChaseGroup))]
    internal partial class EntityweenEndSimulationGroup : ComponentSystemGroup { }
}
