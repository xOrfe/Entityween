using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace XO.Entityween.Editor
{
    public class EntityweenSettingsView : IEntityweenView
    {
        private EntityweenWindow _window;
        private VisualElement _root;

        public void Initialize(EntityweenWindow window, VisualElement root)
        {
            _window = window;
            _root = root;

            BuildSettingsView();
        }

        public void Cleanup()
        {
            _root.Clear();
        }

        public void Tick()
        {
            // Settings doesn't require real-time ticking
        }

        private void BuildSettingsView()
        {
            var header = new VisualElement();
            header.style.marginBottom = 15;

            var title = EntityweenUIStyleUtility.CreateLabelWithIcon("⚙", "ENTITYWEEN SETTINGS", 14, EntityweenUIStyleUtility.AccentPurple, true);
            header.Add(title);

            var subtitle = new Label("Configure default global settings for the Entityween library.");
            subtitle.style.fontSize = 10;
            subtitle.style.color = new Color(0.6f, 0.6f, 0.62f);
            subtitle.style.marginTop = 3;
            header.Add(subtitle);

            _root.Add(header);

            var serializedSettings = _window.SerializedSettings;
            if (serializedSettings == null)
            {
                _window.LoadSettings();
                serializedSettings = _window.SerializedSettings;
            }

            if (serializedSettings != null)
            {
                serializedSettings.Update();

                var bodyCard = new VisualElement();
                bodyCard.style.backgroundColor = EntityweenUIStyleUtility.CardBgEven;
                bodyCard.style.paddingTop = 15;
                bodyCard.style.paddingBottom = 15;
                bodyCard.style.paddingLeft = 15;
                bodyCard.style.paddingRight = 15;
                bodyCard.style.borderTopLeftRadius = bodyCard.style.borderTopRightRadius = 8;
                bodyCard.style.borderBottomLeftRadius = bodyCard.style.borderBottomRightRadius = 8;
                bodyCard.style.borderLeftWidth = 1;
                bodyCard.style.borderRightWidth = 1;
                bodyCard.style.borderTopWidth = 1;
                bodyCard.style.borderBottomWidth = 1;
                bodyCard.style.borderLeftColor = EntityweenUIStyleUtility.DarkBorder;
                bodyCard.style.borderRightColor = EntityweenUIStyleUtility.DarkBorder;
                bodyCard.style.borderTopColor = EntityweenUIStyleUtility.DarkBorder;
                bodyCard.style.borderBottomColor = EntityweenUIStyleUtility.DarkBorder;

                var defaultDurationProp = serializedSettings.FindProperty("_defaultDuration");
                var enableLogsProp = serializedSettings.FindProperty("_enableLogs");

                var defaultDurationField = new PropertyField(defaultDurationProp, "Default Duration");
                defaultDurationField.style.marginBottom = 10;
                bodyCard.Add(defaultDurationField);

                var enableLogsField = new PropertyField(enableLogsProp, "Enable Logs");
                enableLogsField.style.marginBottom = 15;
                bodyCard.Add(enableLogsField);

                bodyCard.Bind(serializedSettings);

                var actionRow = new VisualElement();
                actionRow.style.flexDirection = FlexDirection.Row;

                var selectBtn = new Button(() =>
                {
                    Selection.activeObject = EntityweenSettings.Instance;
                    EditorGUIUtility.PingObject(EntityweenSettings.Instance);
                });
                selectBtn.text = "Select Settings Asset File";
                EntityweenUIStyleUtility.StyleMiniButton(selectBtn);
                actionRow.Add(selectBtn);

                bodyCard.Add(actionRow);
                _root.Add(bodyCard);
            }
            else
            {
                var errorBox = new Label("Could not load or create Entityween settings.");
                errorBox.style.color = EntityweenUIStyleUtility.AccentRed;
                errorBox.style.unityFontStyleAndWeight = FontStyle.Bold;
                _root.Add(errorBox);
            }
        }
    }
}
