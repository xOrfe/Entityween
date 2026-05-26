using Unity.Collections;
using Unity.Entities;

namespace XO.Entityween
{
    public interface ISequenceActionBlueprint
    {
        TimelineActionKind Kind { get; }
        float Duration { get; }
        FixedString64Bytes CallbackId { get; }

        Entity CreateEntity<TAdapter>(Entity sequenceEntity, TAdapter adapter)
            where TAdapter : IEntityCommandAdapter;
    }
}
