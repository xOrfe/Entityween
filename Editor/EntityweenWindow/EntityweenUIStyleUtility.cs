using UnityEngine;
using UnityEngine.UIElements;

namespace XO.Entityween.Editor
{
    public static class EntityweenUIStyleUtility
    {
        public static readonly Color BgColor        = new Color(0.12f, 0.12f, 0.13f, 1f);
        public static readonly Color CardBgEven     = new Color(0.18f, 0.19f, 0.21f, 1f);
        public static readonly Color CardBgOdd      = new Color(0.15f, 0.16f, 0.18f, 1f);
        public static readonly Color DarkBorder     = new Color(0.24f, 0.25f, 0.27f, 1f);
        public static readonly Color AccentBlue     = new Color(0.22f, 0.78f, 1f,  1f);
        public static readonly Color AccentGreen    = new Color(0.3f,  1f,   0.48f, 1f);
        public static readonly Color AccentGold     = new Color(1f,   0.8f,  0.3f,  1f);
        public static readonly Color AccentRed      = new Color(1f,   0.3f,  0.3f,  1f);
        public static readonly Color AccentPurple   = new Color(0.75f, 0.4f, 1f,   1f);

        public static VisualElement MakeStatusDot(Color color)
        {
            var dot = new VisualElement();
            dot.style.width = 6;
            dot.style.height = 6;
            dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = 3;
            dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 3;
            dot.style.backgroundColor = color;
            dot.style.marginRight = 6;
            return dot;
        }

        public static VisualElement MakeCardRoot(bool isEven)
        {
            var card = new VisualElement();
            card.style.backgroundColor = isEven ? CardBgEven : CardBgOdd;
            card.style.paddingTop = 10;
            card.style.paddingBottom = 10;
            card.style.paddingLeft = 12;
            card.style.paddingRight = 12;
            card.style.marginBottom = 6;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 6;
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 6;
            card.style.borderLeftWidth = card.style.borderRightWidth = 1;
            card.style.borderTopWidth = card.style.borderBottomWidth = 1;
            card.style.borderLeftColor = card.style.borderRightColor = DarkBorder;
            card.style.borderTopColor = card.style.borderBottomColor = DarkBorder;
            return card;
        }

        public static VisualElement MakeProgressBarBg()
        {
            var bg = new VisualElement();
            bg.style.flexGrow = 1;
            bg.style.height = 10;
            bg.style.backgroundColor = new Color(0.09f, 0.10f, 0.11f, 1f);
            bg.style.borderTopLeftRadius = bg.style.borderTopRightRadius = 5;
            bg.style.borderBottomLeftRadius = bg.style.borderBottomRightRadius = 5;
            bg.style.borderLeftWidth = bg.style.borderRightWidth = 1;
            bg.style.borderTopWidth = bg.style.borderBottomWidth = 1;
            bg.style.borderLeftColor = bg.style.borderRightColor = DarkBorder;
            bg.style.borderTopColor = bg.style.borderBottomColor = DarkBorder;
            bg.style.marginRight = 8;
            bg.style.overflow = Overflow.Hidden;
            return bg;
        }

        public static VisualElement MakeProgressFill(Color color)
        {
            var fill = new VisualElement();
            fill.style.width = Length.Percent(0);
            fill.style.height = Length.Percent(100);
            fill.style.backgroundColor = color;
            return fill;
        }

        public static void StyleMiniChip(Label label, Color bg, Color textCol)
        {
            label.style.fontSize = 8;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = textCol;
            label.style.backgroundColor = bg;
            label.style.borderTopLeftRadius = label.style.borderTopRightRadius = 4;
            label.style.borderBottomLeftRadius = label.style.borderBottomRightRadius = 4;
            label.style.paddingTop = 1;
            label.style.paddingBottom = 1;
            label.style.paddingLeft = 5;
            label.style.paddingRight = 5;
        }

        public static void StyleMiniChipBg(Label label, Color bg)
        {
            label.style.backgroundColor = bg;
        }

        public static void StyleActionButton(Button btn, Color bg, Color border, Color textColor)
        {
            btn.style.paddingLeft = 6;
            btn.style.paddingRight = 6;
            btn.style.height = 18;
            btn.style.fontSize = 9;
            btn.style.backgroundColor = bg;
            btn.style.borderTopColor = btn.style.borderBottomColor = border;
            btn.style.borderLeftColor = btn.style.borderRightColor = border;
            btn.style.color = textColor;
        }

        public static void StyleProgressLabel(Label label, int width, TextAnchor anchor)
        {
            label.style.fontSize = 9;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = Color.white;
            label.style.width = width;
            label.style.unityTextAlign = anchor;
        }

        public static void StyleValuesLabel(Label label)
        {
            label.style.fontSize = 9;
            label.style.color = new Color(0.6f, 0.6f, 0.62f, 1f);
            label.style.marginTop = 5;
            label.style.borderTopWidth = 1;
            label.style.borderTopColor = new Color(0.22f, 0.23f, 0.25f, 1f);
            label.style.paddingTop = 4;
        }

        public static void StyleLargeButton(Button btn, Color bg, Color hoverColor)
        {
            btn.style.fontSize = 10;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.color = Color.white;
            btn.style.backgroundColor = bg;
            btn.style.borderTopColor = btn.style.borderBottomColor = DarkBorder;
            btn.style.borderLeftColor = btn.style.borderRightColor = DarkBorder;
            btn.style.height = 32;
            btn.style.paddingLeft = 14;
            btn.style.paddingRight = 14;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 6;

            btn.RegisterCallback<MouseOverEvent>(evt => btn.style.backgroundColor = hoverColor);
            btn.RegisterCallback<MouseOutEvent>(evt => btn.style.backgroundColor = bg);
        }

        public static void StyleMiniButton(Button btn)
        {
            var bg = new Color(0.22f, 0.23f, 0.25f, 1f);
            btn.style.fontSize = 9;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.color = new Color(0.85f, 0.85f, 0.87f);
            btn.style.backgroundColor = bg;
            btn.style.borderTopColor = btn.style.borderBottomColor = DarkBorder;
            btn.style.borderLeftColor = btn.style.borderRightColor = DarkBorder;
            btn.style.height = 24;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.borderTopLeftRadius = btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 4;

            btn.RegisterCallback<MouseOverEvent>(evt =>
            {
                btn.style.backgroundColor = new Color(0.28f, 0.3f, 0.33f, 1f);
                btn.style.color = Color.white;
            });
            btn.RegisterCallback<MouseOutEvent>(evt =>
            {
                btn.style.backgroundColor = bg;
                btn.style.color = new Color(0.85f, 0.85f, 0.87f);
            });
        }

        public static Color StateColor(PlaybackState state) => state switch
        {
            PlaybackState.Playing   => AccentGreen,
            PlaybackState.Paused    => AccentGold,
            PlaybackState.Completed => new Color(0.4f, 0.4f, 0.43f),
            _ => Color.gray
        };

        public static Color StateBgColor(PlaybackState state) => state switch
        {
            PlaybackState.Playing   => new Color(0.1f, 0.25f, 0.12f),
            PlaybackState.Paused    => new Color(0.25f, 0.2f, 0.05f),
            PlaybackState.Completed => new Color(0.17f, 0.17f, 0.19f),
            _ => new Color(0.15f, 0.15f, 0.17f)
        };

        public static Color ElementKindAccentColor(TimelineActionKind kind) => kind switch
        {
            TimelineActionKind.Tween    => AccentBlue,
            TimelineActionKind.Chase    => AccentGreen,
            TimelineActionKind.Wait     => new Color(0.7f, 0.7f, 0.75f),
            TimelineActionKind.Callback => AccentGold,
            _ => Color.gray
        };

        public static Color ElementKindColor(TimelineActionKind kind, bool completed, bool started)
        {
            if (completed) return new Color(0.22f, 0.22f, 0.25f, 0.7f);
            var accent = ElementKindAccentColor(kind);
            return started
                ? new Color(accent.r, accent.g, accent.b, 0.85f)
                : new Color(accent.r * 0.4f, accent.g * 0.4f, accent.b * 0.4f, 0.5f);
        }

        public static VisualElement CreateLabelWithIcon(string iconText, string labelText, float fontSize, Color textColor, bool isBold = true, float spacing = 6f)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            if (!string.IsNullOrEmpty(iconText))
            {
                var icon = new Label(iconText);
                icon.style.fontSize = fontSize;
                icon.style.width = fontSize * 1.3f;
                icon.style.unityTextAlign = TextAnchor.MiddleCenter;
                icon.style.marginRight = spacing;
                icon.style.color = textColor;
                row.Add(icon);
            }

            var label = new Label(labelText);
            label.style.fontSize = fontSize;
            label.style.color = textColor;
            if (isBold)
            {
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            row.Add(label);

            return row;
        }

        public static VisualElement CreateMiniChipWithIcon(string iconText, string labelText, Color bg, Color textCol, float spacing = 4f)
        {
            var chip = CreateLabelWithIcon(iconText, labelText, 8, textCol, true, spacing);
            chip.style.backgroundColor = bg;
            chip.style.borderTopLeftRadius = chip.style.borderTopRightRadius = 4;
            chip.style.borderBottomLeftRadius = chip.style.borderBottomRightRadius = 4;
            chip.style.paddingTop = 1;
            chip.style.paddingBottom = 1;
            chip.style.paddingLeft = 5;
            chip.style.paddingRight = 5;
            return chip;
        }
    }
}
