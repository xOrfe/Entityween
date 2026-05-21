using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using TMPro;

namespace Entityween.Samples
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ShowcaseTextSystem : SystemBase
    {
        private Dictionary<Entity, GameObject> _labels = new Dictionary<Entity, GameObject>();

        protected override void OnUpdate()
        {
            var activeEntities = new HashSet<Entity>();

            // Query all entities that have a floating text component and a position
            foreach (var (text, ltw, entity) in SystemAPI.Query<RefRO<ShowcaseText>, RefRO<LocalToWorld>>()
                         .WithEntityAccess())
            {
                activeEntities.Add(entity);

                if (!_labels.TryGetValue(entity, out var labelGo))
                {
                    // Create a new 3D TextMeshPro GameObject
                    labelGo = new GameObject($"ShowcaseLabel_{entity.Index}");
                    var tmp = labelGo.AddComponent<TextMeshPro>();
                    tmp.text = text.ValueRO.Value.ToString();
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.fontSize = 5.0f;
                    tmp.color = Color.white;
                    
                    _labels[entity] = labelGo;
                }

                // Keep the text centered and floating 2.0 units above the entity's position
                labelGo.transform.position = (Vector3)ltw.ValueRO.Position + new Vector3(0f, 2.0f, 0f);

                if (Camera.main != null)
                {
                    // Make text face the main camera so it remains readable from all angles
                    labelGo.transform.rotation = Quaternion.LookRotation(labelGo.transform.position - Camera.main.transform.position);
                }
            }

            // Cleanup GameObjects for entities that no longer exist or lost their ShowcaseText component
            var toRemove = new List<Entity>();
            foreach (var pair in _labels)
            {
                if (!activeEntities.Contains(pair.Key))
                {
                    if (pair.Value != null)
                    {
                        Object.Destroy(pair.Value);
                    }
                    toRemove.Add(pair.Key);
                }
            }

            foreach (var entity in toRemove)
            {
                _labels.Remove(entity);
            }
        }

        protected override void OnDestroy()
        {
            foreach (var label in _labels.Values)
            {
                if (label != null)
                {
                    Object.Destroy(label);
                }
            }
            _labels.Clear();
        }
    }
}
