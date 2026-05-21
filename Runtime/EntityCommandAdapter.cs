using Unity.Entities;
using UnityEngine;

namespace XO.Entityween
{

    internal interface IEntityCommandAdapter
    {
        Entity CreateEntity();
        void AddComponent<T>(Entity e, T component) where T : unmanaged, IComponentData;
        void SetComponent<T>(Entity e, T component) where T : unmanaged, IComponentData;
        void RemoveComponent<T>(Entity e) where T : unmanaged, IComponentData;
        void SetComponentEnabled<T>(Entity e, bool enabled) where T : unmanaged, IEnableableComponent, IComponentData;
        DynamicBuffer<T> AddBuffer<T>(Entity e) where T : unmanaged, IBufferElementData;
        void AppendToBuffer<T>(Entity e, T element) where T : unmanaged, IBufferElementData;
        Entity Instantiate(Entity e);
        void DestroyEntity(Entity e);
    }

    internal struct EntityManagerAdapter : IEntityCommandAdapter
    {
        public EntityManager Em;

        public Entity CreateEntity() => Em.CreateEntity();
        public void AddComponent<T>(Entity e, T component) where T : unmanaged, IComponentData
        {
            if (Em.HasComponent<T>(e)) Em.SetComponentData(e, component);
            else Em.AddComponentData(e, component);
        }
        public void SetComponent<T>(Entity e, T component) where T : unmanaged, IComponentData => Em.SetComponentData(e, component);
        public void RemoveComponent<T>(Entity e) where T : unmanaged, IComponentData => Em.RemoveComponent<T>(e);
        public void SetComponentEnabled<T>(Entity e, bool enabled) where T : unmanaged, IEnableableComponent, IComponentData => Em.SetComponentEnabled<T>(e, enabled);
        public DynamicBuffer<T> AddBuffer<T>(Entity e) where T : unmanaged, IBufferElementData => Em.AddBuffer<T>(e);
        public void AppendToBuffer<T>(Entity e, T element) where T : unmanaged, IBufferElementData => Em.GetBuffer<T>(e).Add(element);
        public Entity Instantiate(Entity e) => Em.Instantiate(e);
        public void DestroyEntity(Entity e) => Em.DestroyEntity(e);
    }

    internal struct EntityCommandBufferAdapter : IEntityCommandAdapter
    {
        public EntityCommandBuffer ECB;

        public Entity CreateEntity() => ECB.CreateEntity();
        public void AddComponent<T>(Entity e, T component) where T : unmanaged, IComponentData => ECB.AddComponent(e, component);
        public void SetComponent<T>(Entity e, T component) where T : unmanaged, IComponentData => ECB.SetComponent(e, component);
        public void RemoveComponent<T>(Entity e) where T : unmanaged, IComponentData => ECB.RemoveComponent<T>(e);
        public void SetComponentEnabled<T>(Entity e, bool enabled) where T : unmanaged, IEnableableComponent, IComponentData => ECB.SetComponentEnabled<T>(e, enabled);
        public DynamicBuffer<T> AddBuffer<T>(Entity e) where T : unmanaged, IBufferElementData => ECB.AddBuffer<T>(e);
        public void AppendToBuffer<T>(Entity e, T element) where T : unmanaged, IBufferElementData => ECB.AppendToBuffer(e, element);
        public Entity Instantiate(Entity e) => ECB.Instantiate(e);
        public void DestroyEntity(Entity e) => ECB.DestroyEntity(e);
    }

    internal struct ParallelWriterAdapter : IEntityCommandAdapter
    {
        public int SortKey;
        public EntityCommandBuffer.ParallelWriter ECB;

        public Entity CreateEntity() => ECB.CreateEntity(SortKey);
        public void AddComponent<T>(Entity e, T component) where T : unmanaged, IComponentData => ECB.AddComponent(SortKey, e, component);
        public void SetComponent<T>(Entity e, T component) where T : unmanaged, IComponentData => ECB.SetComponent(SortKey, e, component);
        public void RemoveComponent<T>(Entity e) where T : unmanaged, IComponentData => ECB.RemoveComponent<T>(SortKey, e);
        public void SetComponentEnabled<T>(Entity e, bool enabled) where T : unmanaged, IEnableableComponent, IComponentData => ECB.SetComponentEnabled<T>(SortKey, e, enabled);
        public DynamicBuffer<T> AddBuffer<T>(Entity e) where T : unmanaged, IBufferElementData => ECB.AddBuffer<T>(SortKey, e);
        public void AppendToBuffer<T>(Entity e, T element) where T : unmanaged, IBufferElementData => ECB.AppendToBuffer(SortKey, e, element);
        public Entity Instantiate(Entity e) => ECB.Instantiate(SortKey, e);
        public void DestroyEntity(Entity e) => ECB.DestroyEntity(SortKey, e);
    }

    internal struct BakerAdapter<TAuth> : IEntityCommandAdapter where TAuth : MonoBehaviour
    {
        public Baker<TAuth> Baker;
        public Entity CreateEntity() => Baker.CreateAdditionalEntity(TransformUsageFlags.Dynamic);
        public void AddComponent<T>(Entity e, T component) where T : unmanaged, IComponentData => Baker.AddComponent(e, component);
        public void SetComponent<T>(Entity e, T component) where T : unmanaged, IComponentData => Baker.SetComponent(e, component);
        public void RemoveComponent<T>(Entity e) where T : unmanaged, IComponentData => Debug.LogError("Cannot remove component from Baker.");
        public void SetComponentEnabled<T>(Entity e, bool enabled) where T : unmanaged, IEnableableComponent, IComponentData => Baker.SetComponentEnabled<T>(e, enabled);
        public DynamicBuffer<T> AddBuffer<T>(Entity e) where T : unmanaged, IBufferElementData => Baker.AddBuffer<T>(e);
        public void AppendToBuffer<T>(Entity e, T element) where T : unmanaged, IBufferElementData => Baker.AddBuffer<T>(e).Add(element);

        public Entity Instantiate(Entity e) { Debug.LogError("Cannot instantiate entity from baker"); return Entity.Null; }
        public void DestroyEntity(Entity e) { Debug.LogError("Cannot destroy entity from baker"); }
    }
}
