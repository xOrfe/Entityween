using Unity.Collections;
using Unity.Entities;

namespace XO.Entityween
{
    [UpdateInGroup(typeof(EntityweenSequenceGroup))]
    [UpdateBefore(typeof(SequencePlaybackSystem))]
    [RequireMatchingQueriesForUpdate]
    internal partial struct PlaybackCommandConsumeSystem : ISystem
    {
        private EntityQuery m_PauseQuery;
        private EntityQuery m_ResumeQuery;
        private EntityQuery m_KillQuery;
        private EntityQuery m_CompleteQuery;
        private EntityQuery m_RewindQuery;

        public void OnCreate(ref SystemState state)
        {
            m_PauseQuery = state.GetEntityQuery(typeof(PlaybackPauseRequest));
            m_ResumeQuery = state.GetEntityQuery(typeof(PlaybackResumeRequest));
            m_KillQuery = state.GetEntityQuery(typeof(PlaybackKillRequest));
            m_CompleteQuery = state.GetEntityQuery(typeof(PlaybackCompleteRequest));
            m_RewindQuery = state.GetEntityQuery(typeof(PlaybackRewindRequest));
        }

        public void OnUpdate(ref SystemState state)
        {
            var em = state.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Process Pause Requests
            if (!m_PauseQuery.IsEmptyIgnoreFilter)
            {
                using (var entities = m_PauseQuery.ToEntityArray(Allocator.Temp))
                {
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var entity = entities[i];
                        if (em.Exists(entity))
                        {
                            PlaybackControlInternal.PauseInternal(entity, em);
                            ecb.RemoveComponent<PlaybackPauseRequest>(entity);
                        }
                    }
                }
            }

            // Process Resume Requests
            if (!m_ResumeQuery.IsEmptyIgnoreFilter)
            {
                using (var entities = m_ResumeQuery.ToEntityArray(Allocator.Temp))
                {
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var entity = entities[i];
                        if (em.Exists(entity))
                        {
                            PlaybackControlInternal.ResumeInternal(entity, em);
                            ecb.RemoveComponent<PlaybackResumeRequest>(entity);
                        }
                    }
                }
            }

            // Process Kill Requests
            if (!m_KillQuery.IsEmptyIgnoreFilter)
            {
                using (var entities = m_KillQuery.ToEntityArray(Allocator.Temp))
                {
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var entity = entities[i];
                        if (em.Exists(entity))
                        {
                            PlaybackControlInternal.KillInternal(entity, em);
                        }
                    }
                }
            }

            // Process Complete Requests
            if (!m_CompleteQuery.IsEmptyIgnoreFilter)
            {
                using (var entities = m_CompleteQuery.ToEntityArray(Allocator.Temp))
                {
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var entity = entities[i];
                        if (em.Exists(entity))
                        {
                            PlaybackControlInternal.CompleteInternal(entity, em);
                        }
                    }
                }
            }

            // Process Rewind Requests
            if (!m_RewindQuery.IsEmptyIgnoreFilter)
            {
                using (var entities = m_RewindQuery.ToEntityArray(Allocator.Temp))
                {
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var entity = entities[i];
                        if (em.Exists(entity))
                        {
                            PlaybackControlInternal.RewindInternal(entity, em);
                            ecb.RemoveComponent<PlaybackRewindRequest>(entity);
                        }
                    }
                }
            }

            ecb.Playback(em);
            ecb.Dispose();
        }
    }
}
