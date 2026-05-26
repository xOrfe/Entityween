using Unity.Entities;
using UnityEngine;

namespace XO.Entityween
{
    public static class EntityweenBaker<TAuth> where TAuth : MonoBehaviour
    {
        public static void Pause(Entity entity, Baker<TAuth> baker) => baker.AddComponent<PlaybackPauseRequest>(entity);
        public static void Resume(Entity entity, Baker<TAuth> baker) => baker.AddComponent<PlaybackResumeRequest>(entity);
        public static void Kill(Entity entity, Baker<TAuth> baker) => baker.AddComponent<PlaybackKillRequest>(entity);
        public static void Complete(Entity entity, Baker<TAuth> baker) => baker.AddComponent<PlaybackCompleteRequest>(entity);
        public static void Rewind(Entity entity, Baker<TAuth> baker) => baker.AddComponent<PlaybackRewindRequest>(entity);
    }
    public static class Entityween
    {
        public static void Pause(Entity entity, EntityManager em) => PlaybackControlInternal.PauseInternal(entity, em);
        public static void Pause(Entity entity, EntityCommandBuffer ecb) => ecb.AddComponent<PlaybackPauseRequest>(entity);
        public static void Pause(Entity entity, int sortKey, EntityCommandBuffer.ParallelWriter ecb) => ecb.AddComponent<PlaybackPauseRequest>(sortKey,entity);

        public static void Resume(Entity entity, EntityManager em) => PlaybackControlInternal.ResumeInternal(entity, em);
        public static void Resume(Entity entity, EntityCommandBuffer ecb) => ecb.AddComponent<PlaybackResumeRequest>(entity);
        public static void Resume(Entity entity, int sortKey, EntityCommandBuffer.ParallelWriter ecb) => ecb.AddComponent<PlaybackResumeRequest>(sortKey,entity);
        
        public static void Kill(Entity entity, EntityManager em) => PlaybackControlInternal.KillInternal(entity, em);
        public static void Kill(Entity entity, EntityCommandBuffer ecb) => ecb.AddComponent<PlaybackKillRequest>(entity);
        public static void Kill(Entity entity, int sortKey, EntityCommandBuffer.ParallelWriter ecb) => ecb.AddComponent<PlaybackKillRequest>(sortKey,entity);
        
        public static void Complete(Entity entity, EntityManager em) => PlaybackControlInternal.CompleteInternal(entity, em);
        public static void Complete(Entity entity, EntityCommandBuffer ecb) => ecb.AddComponent<PlaybackCompleteRequest>(entity);
        public static void Complete(Entity entity, int sortKey, EntityCommandBuffer.ParallelWriter ecb) => ecb.AddComponent<PlaybackCompleteRequest>(sortKey,entity);

        public static void Rewind(Entity entity, EntityManager em) => PlaybackControlInternal.RewindInternal(entity, em);
        public static void Rewind(Entity entity, EntityCommandBuffer ecb) => ecb.AddComponent<PlaybackRewindRequest>(entity);
        public static void Rewind(Entity entity, int sortKey, EntityCommandBuffer.ParallelWriter ecb) => ecb.AddComponent<PlaybackRewindRequest>(sortKey,entity);
    }
}