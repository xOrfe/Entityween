using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace XO.Entityween.Editor
{
    public class EntityweenDashboardView : IEntityweenView
    {
        private EntityweenWindow _window;
        private VisualElement _root;

        public void Initialize(EntityweenWindow window, VisualElement root)
        {
            _window = window;
            _root = root;

            BuildDashboardView();
        }

        public void Cleanup()
        {
            _root.Clear();
        }

        public void Tick()
        {
            // Dashboard doesn't require real-time ticking
        }

        private void BuildDashboardView()
        {
            // Title Header Card
            var headerCard = new VisualElement();
            headerCard.style.backgroundColor = new Color(0.15f, 0.20f, 0.28f, 1f);
            headerCard.style.paddingTop = 18;
            headerCard.style.paddingBottom = 18;
            headerCard.style.paddingLeft = 20;
            headerCard.style.paddingRight = 20;
            headerCard.style.borderTopLeftRadius = headerCard.style.borderTopRightRadius = 8;
            headerCard.style.borderBottomLeftRadius = headerCard.style.borderBottomRightRadius = 8;
            headerCard.style.marginBottom = 15;
            headerCard.style.borderLeftWidth = 4;
            headerCard.style.borderLeftColor = EntityweenUIStyleUtility.AccentBlue;

            var title = EntityweenUIStyleUtility.CreateLabelWithIcon("", "ENTITYWEEN DASHBOARD", 16, Color.white, true);
            headerCard.Add(title);

            var desc = new Label("High-performance, memory-efficient tweening utilities optimized for Unity ECS (DOTS).");
            desc.style.fontSize = 10;
            desc.style.color = new Color(0.8f, 0.82f, 0.88f);
            desc.style.marginTop = 5;
            headerCard.Add(desc);

            _root.Add(headerCard);

            var grid = new VisualElement();
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexGrow = 1;

            var leftCol = new VisualElement();
            leftCol.style.flexGrow = 1;
            leftCol.style.marginRight = 10;
            grid.Add(leftCol);

            // Samples & Showcase Generator Card
            var samplesCard = new VisualElement();
            samplesCard.style.backgroundColor = EntityweenUIStyleUtility.CardBgEven;
            samplesCard.style.paddingTop = 15;
            samplesCard.style.paddingBottom = 15;
            samplesCard.style.paddingLeft = 15;
            samplesCard.style.paddingRight = 15;
            samplesCard.style.borderTopLeftRadius = samplesCard.style.borderTopRightRadius = 8;
            samplesCard.style.borderBottomLeftRadius = samplesCard.style.borderBottomRightRadius = 8;
            samplesCard.style.borderLeftWidth = 1;
            samplesCard.style.borderRightWidth = 1;
            samplesCard.style.borderTopWidth = 1;
            samplesCard.style.borderBottomWidth = 1;
            samplesCard.style.borderLeftColor = EntityweenUIStyleUtility.DarkBorder;
            samplesCard.style.borderRightColor = EntityweenUIStyleUtility.DarkBorder;
            samplesCard.style.borderTopColor = EntityweenUIStyleUtility.DarkBorder;
            samplesCard.style.borderBottomColor = EntityweenUIStyleUtility.DarkBorder;
            samplesCard.style.marginBottom = 15;

            var sTitle = EntityweenUIStyleUtility.CreateLabelWithIcon("🎬", "Samples & Showcase Generation", 12, Color.white, true);
            sTitle.style.marginBottom = 10;
            samplesCard.Add(sTitle);

            var statusRow = new VisualElement();
            statusRow.style.flexDirection = FlexDirection.Row;
            statusRow.style.alignItems = Align.Center;
            statusRow.style.marginBottom = 12;

            bool isImported = Directory.Exists("Assets/Samples/Entityween/1.0.0");
            var statusDot = EntityweenUIStyleUtility.MakeStatusDot(isImported ? EntityweenUIStyleUtility.AccentGreen : EntityweenUIStyleUtility.AccentGold);
            statusRow.Add(statusDot);

            var statusText = new Label(isImported ? "Status: Samples Imported (v1.0.0)" : "Status: Samples Not Imported");
            statusText.style.fontSize = 10;
            statusText.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusText.style.color = isImported ? EntityweenUIStyleUtility.AccentGreen : EntityweenUIStyleUtility.AccentGold;
            statusRow.Add(statusText);
            samplesCard.Add(statusRow);

            var sDesc = new Label("Import package samples to gain access to interactive showcases, spline paths, benchmarks, and ease galleries.");
            sDesc.style.fontSize = 9;
            sDesc.style.color = new Color(0.6f, 0.6f, 0.62f);
            sDesc.style.marginBottom = 15;
            sDesc.style.whiteSpace = WhiteSpace.Normal;
            samplesCard.Add(sDesc);

            var btns = new VisualElement();
            btns.style.flexDirection = FlexDirection.Row;
            btns.style.flexWrap = Wrap.Wrap;

            var importBtn = new Button(() => { _window.ImportSamples(); _window.SwitchView(EntityweenWindow.ViewType.Dashboard); });
            importBtn.text = isImported ? "Re-import Samples" : "Import Samples";
            EntityweenUIStyleUtility.StyleLargeButton(importBtn, new Color(0.18f, 0.22f, 0.26f), EntityweenUIStyleUtility.AccentBlue);
            importBtn.style.marginRight = 8;
            importBtn.style.marginBottom = 8;
            btns.Add(importBtn);

            if (isImported)
            {
                var genShowcasesBtn = new Button(() => { RunShowcaseGenerators(); });
                genShowcasesBtn.text = "Generate Showcases";
                EntityweenUIStyleUtility.StyleLargeButton(genShowcasesBtn, new Color(0.15f, 0.25f, 0.18f), EntityweenUIStyleUtility.AccentGreen);
                genShowcasesBtn.style.marginBottom = 8;
                btns.Add(genShowcasesBtn);
            }
            
            samplesCard.Add(btns);
            leftCol.Add(samplesCard);

            // Info Card
            var infoCard = new VisualElement();
            infoCard.style.backgroundColor = EntityweenUIStyleUtility.CardBgEven;
            infoCard.style.paddingTop = 15;
            infoCard.style.paddingBottom = 15;
            infoCard.style.paddingLeft = 15;
            infoCard.style.paddingRight = 15;
            infoCard.style.borderTopLeftRadius = infoCard.style.borderTopRightRadius = 8;
            infoCard.style.borderBottomLeftRadius = infoCard.style.borderBottomRightRadius = 8;
            infoCard.style.borderLeftWidth = 1;
            infoCard.style.borderRightWidth = 1;
            infoCard.style.borderTopWidth = 1;
            infoCard.style.borderBottomWidth = 1;
            infoCard.style.borderLeftColor = EntityweenUIStyleUtility.DarkBorder;
            infoCard.style.borderRightColor = EntityweenUIStyleUtility.DarkBorder;
            infoCard.style.borderTopColor = EntityweenUIStyleUtility.DarkBorder;
            infoCard.style.borderBottomColor = EntityweenUIStyleUtility.DarkBorder;

            var infoTitle = EntityweenUIStyleUtility.CreateLabelWithIcon("📚", "Useful Documentation & Links", 12, Color.white, true);
            infoTitle.style.marginBottom = 10;
            infoCard.Add(infoTitle);

            var infoText = new Label("Entityween provides highly optimized, Burst-compiled ECS tweening jobs. Visit the documentation page or open issues directly on our GitHub page.");
            infoText.style.fontSize = 9;
            infoText.style.color = new Color(0.6f, 0.6f, 0.62f);
            infoText.style.marginBottom = 12;
            infoText.style.whiteSpace = WhiteSpace.Normal;
            infoCard.Add(infoText);

            var linksRow = new VisualElement();
            linksRow.style.flexDirection = FlexDirection.Row;
            linksRow.style.flexWrap = Wrap.Wrap;

            var docBtn = new Button(() => { Application.OpenURL("https://github.com/xOrfe/Entityween#readme"); });
            docBtn.text = "Open Documentation";
            EntityweenUIStyleUtility.StyleMiniButton(docBtn);
            docBtn.style.marginRight = 8;
            docBtn.style.marginBottom = 8;
            linksRow.Add(docBtn);

            var gitBtn = new Button(() => { Application.OpenURL("https://github.com/xOrfe/Entityween"); });
            gitBtn.text = "GitHub Repository";
            EntityweenUIStyleUtility.StyleMiniButton(gitBtn);
            gitBtn.style.marginBottom = 8;
            linksRow.Add(gitBtn);

            infoCard.Add(linksRow);
            leftCol.Add(infoCard);

            _root.Add(grid);
        }

        private void RunShowcaseGenerators()
        {
            var type1 = Type.GetType("Entityween.Editor.EntityweenShowcaseSceneBuilder, Assembly-CSharp-Editor");
            var type2 = Type.GetType("Entityween.Editor.EntityweenGameObjectShowcaseSceneBuilder, Assembly-CSharp-Editor");

            if (type1 == null || type2 == null)
            {
                EditorUtility.DisplayDialog("Entityween Showcase Generator", "Showcase Scene Builders are not compiled yet. Ensure samples are imported and compiled.", "OK");
                return;
            }

            bool success1 = false;
            bool success2 = false;

            try
            {
                var method1 = type1.GetMethod("GenerateShowcaseScene", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method1 != null)
                {
                    method1.Invoke(null, null);
                    success1 = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Entityween] Error generating Combined Showcase: {ex.Message}");
            }

            try
            {
                var method2 = type2.GetMethod("GenerateShowcaseScene", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method2 != null)
                {
                    method2.Invoke(null, null);
                    success2 = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Entityween] Error generating GameObject Showcase: {ex.Message}");
            }

            if (success1 && success2)
            {
                EditorUtility.DisplayDialog("Entityween Showcase Generator", "Showcase scenes successfully generated!", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Entityween Showcase Generator", "Failed to generate one or more showcase scenes. Please check the Unity console for errors.", "OK");
            }
        }
    }
}
