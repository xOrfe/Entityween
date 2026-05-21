using Unity.Entities;

namespace XO.Entityween
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    internal partial class EntityweenSystemGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(EntityweenSystemGroup))]
    internal partial class EntityweenSequenceGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(EntityweenSystemGroup))]
    [UpdateAfter(typeof(EntityweenSequenceGroup))]
    internal partial class EntityweenCalculationGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(EntityweenSystemGroup))]
    [UpdateAfter(typeof(EntityweenCalculationGroup))]
    internal partial class EntityweenChaseGroup : ComponentSystemGroup { }
}
