using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace XO.Entityween
{
    /// <summary>
    /// A static registry that maps ECS (Entity Component System) entities to managed Unity objects (such as Transform or GameObject),
    /// member setters, and callback functions.
    /// Since ECS components can only store unmanaged data, managed references used during tweening are stored
    /// in this registry and cleaned up according to the entities' life cycle.
    /// </summary>
    internal static class TweenManagedRegistry
    {
        #region GameObject Targets

        /// <summary>
        /// Record structure holding the Transform reference of a GameObject paired with an ECS Entity.
        /// </summary>
        public struct GameObjectRecord
        {
            public Transform Target;
        }

        /// <summary>
        /// Dictionary mapping (World, Entity) pairs to their corresponding target GameObject Transform.
        /// </summary>
        public static readonly Dictionary<(World, Entity), GameObjectRecord> GameObjectTargets = new Dictionary<(World, Entity), GameObjectRecord>(1024);

        /// <summary>
        /// Registers the target GameObject Transform for a specified Entity.
        /// </summary>
        public static void RegisterGameObject(World world, Entity entity, Transform target)
        {
            GameObjectTargets[(world, entity)] = new GameObjectRecord { Target = target };
        }

        /// <summary>
        /// Tries to retrieve the registered GameObject Transform for a specified Entity.
        /// </summary>
        public static bool TryGetGameObject(World world, Entity entity, out Transform target)
        {
            if (GameObjectTargets.TryGetValue((world, entity), out var record))
            {
                target = record.Target;
                return true;
            }
            target = null;
            return false;
        }

        #endregion

        #region Generic Member Hooks (Managed Class Members/Fields)

        /// <summary>
        /// Generic registry storage used to tween property/field (member) values of managed objects.
        /// </summary>
        public static class Member<T> where T : unmanaged
        {
            /// <summary>
            /// Record structure holding the target object reference, member name, and setter action.
            /// </summary>
            public struct Record
            {
                public object Target;
                public string MemberName;
                public Action<object, T> Setter;
            }

            /// <summary>
            /// Dictionary storing member tween records mapped by (World, Entity) per type (T).
            /// </summary>
            public static readonly Dictionary<(World, Entity), Record> Records = new Dictionary<(World, Entity), Record>(512);
        }

        /// <summary>
        /// Registers a member of a managed object to be tweened.
        /// </summary>
        public static void RegisterMember<T>(World world, Entity entity, object target, string memberName, Action<object, T> setter) where T : unmanaged
        {
            Member<T>.Records[(world, entity)] = new Member<T>.Record
            {
                Target = target,
                MemberName = memberName,
                Setter = setter
            };
        }

        /// <summary>
        /// Tries to retrieve the registered member tween record for a specified Entity.
        /// </summary>
        public static bool TryGetMember<T>(World world, Entity entity, out Member<T>.Record record) where T : unmanaged
        {
            return Member<T>.Records.TryGetValue((world, entity), out record);
        }

        #endregion

        #region Generic Callback Hooks (Tween Events / Callbacks)

        /// <summary>
        /// Generic registry storage for C# methods (callbacks) triggered during tween steps or on completion.
        /// </summary>
        public static class Callback<T> where T : unmanaged
        {
            /// <summary>
            /// Record structure holding the callback action, optional state object, and state-based callback action.
            /// </summary>
            public struct Record
            {
                public Action<T> Callback;
                public object State;
                public Action<object, T> StateCallback;
            }

            /// <summary>
            /// Dictionary storing callback records mapped by (World, Entity) per type (T).
            /// </summary>
            public static readonly Dictionary<(World, Entity), Record> Records = new Dictionary<(World, Entity), Record>(512);
        }

        /// <summary>
        /// Registers a callback function for a tween.
        /// </summary>
        public static void RegisterCallback<T>(World world, Entity entity, Action<T> callback, object state, Action<object, T> stateCallback) where T : unmanaged
        {
            Callback<T>.Records[(world, entity)] = new Callback<T>.Record
            {
                Callback = callback,
                State = state,
                StateCallback = stateCallback
            };
        }

        /// <summary>
        /// Tries to retrieve the registered callback record for a specified Entity.
        /// </summary>
        public static bool TryGetCallback<T>(World world, Entity entity, out Callback<T>.Record record) where T : unmanaged
        {
            return Callback<T>.Records.TryGetValue((world, entity), out record);
        }

        #endregion

        #region Cleanup (Memory Cleanup)

        private static readonly List<(World, Entity)> _tempKeys = new List<(World, Entity)>(1024);

        /// <summary>
        /// Cleans up all managed registry records associated with destroyed Entities in the specified ECS World.
        /// This is crucial to prevent memory leaks.
        /// </summary>
        public static void Cleanup(World world, EntityManager em)
        {
            CleanupDict(world, em, GameObjectTargets);

            CleanupDict(world, em, Member<float>.Records);
            CleanupDict(world, em, Member<Unity.Mathematics.float2>.Records);
            CleanupDict(world, em, Member<Unity.Mathematics.float3>.Records);
            CleanupDict(world, em, Member<Unity.Mathematics.quaternion>.Records);

            CleanupDict(world, em, Callback<float>.Records);
            CleanupDict(world, em, Callback<Unity.Mathematics.float2>.Records);
            CleanupDict(world, em, Callback<Unity.Mathematics.float3>.Records);
            CleanupDict(world, em, Callback<Unity.Mathematics.quaternion>.Records);
        }

        /// <summary>
        /// Iterates through a dictionary and removes records whose Entity no longer exists (EntityManager.Exists = false) in the specified World.
        /// </summary>
        private static void CleanupDict<TValue>(World world, EntityManager em, Dictionary<(World, Entity), TValue> dict)
        {
            _tempKeys.Clear();

            foreach (var key in dict.Keys)
            {
                if (key.Item1 == world && !em.Exists(key.Item2))
                {
                    _tempKeys.Add(key);
                }
            }

            for (int i = 0; i < _tempKeys.Count; i++)
            {
                dict.Remove(_tempKeys[i]);
            }
        }

        #endregion
    }
}
