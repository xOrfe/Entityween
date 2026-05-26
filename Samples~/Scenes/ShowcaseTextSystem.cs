using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using TMPro;

namespace Entityween.Samples
{
    /// <summary>
    /// Creates and manages floating 3D text labels for entities that have a ShowcaseText component.
    /// This system is fully self-managing: it creates labels when ShowcaseText entities appear and
    /// destroys them when entities are removed (e.g. when a SubScene is unloaded).
    /// 
    /// Do NOT disable this system externally — let it run and it will clean up naturally.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ShowcaseTextSystem : SystemBase
    {
        private readonly Dictionary<Entity, GameObject> labels = new();
        private readonly HashSet<Entity> visibleEntities = new();
        private readonly List<Entity> staleEntities = new();

        protected override void OnUpdate()
        {
            visibleEntities.Clear();

            foreach (var (text, ltw, entity) in SystemAPI.Query<RefRO<ShowcaseText>, RefRO<LocalToWorld>>()
                         .WithEntityAccess())
            {
                visibleEntities.Add(entity);

                if (!labels.TryGetValue(entity, out var labelGo))
                {
                    labelGo = CreateLabel(entity, text.ValueRO.Value.ToString());
                    labels[entity] = labelGo;
                }

                PositionLabel(labelGo, ltw.ValueRO.Position);
            }

            // When a SubScene is unloaded, its entities disappear from the query above.
            // visibleEntities will be empty (or missing those entities), so RemoveStaleLabels
            // destroys the orphaned label GameObjects automatically.
            RemoveStaleLabels();
        }

        private static GameObject CreateLabel(Entity entity, string text)
        {
            var labelGo = new GameObject($"ShowcaseLabel_{entity.Index}");
            var tmp = labelGo.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 5.0f;
            tmp.color = Color.white;
            return labelGo;
        }

        private static void PositionLabel(GameObject labelGo, Unity.Mathematics.float3 position)
        {
            labelGo.transform.position = (Vector3)position + new Vector3(0f, 2.0f, 0f);

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                labelGo.transform.rotation = Quaternion.LookRotation(labelGo.transform.position - mainCamera.transform.position);
            }
        }

        private void RemoveStaleLabels()
        {
            staleEntities.Clear();
            foreach (var pair in labels)
            {
                if (visibleEntities.Contains(pair.Key)) continue;

                if (pair.Value != null)
                {
                    Object.Destroy(pair.Value);
                }
                staleEntities.Add(pair.Key);
            }

            foreach (var entity in staleEntities)
            {
                labels.Remove(entity);
            }
        }

        protected override void OnDestroy()
        {
            foreach (var label in labels.Values)
            {
                if (label != null)
                {
                    Object.Destroy(label);
                }
            }
            labels.Clear();
        }
    }
}
