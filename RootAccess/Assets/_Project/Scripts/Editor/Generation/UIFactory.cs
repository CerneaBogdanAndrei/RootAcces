using RootAccess.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.EditorTools
{
    /// <summary>UGUI + TextMeshPro construction helpers with the game's neon style.</summary>
    public static class UIFactory
    {
        public static readonly Color Backdrop = new(0.02f, 0.01f, 0.06f, 0.82f);
        public static readonly Color Panel = new(0.05f, 0.04f, 0.11f, 0.94f);
        public static readonly Color Cyan = new(0.18f, 0.95f, 1f);
        public static readonly Color Magenta = new(1f, 0.18f, 0.65f);
        public static readonly Color Yellow = new(1f, 0.82f, 0.25f);
        public static readonly Color TextColor = new(0.9f, 0.94f, 1f);
        public static readonly Color Muted = new(0.6f, 0.64f, 0.78f);

        public static Sprite White => GeneratedArt.Sprite("white");

        public static GameObject Canvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = Core.Layers.UI;
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = Core.Layers.UI };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect, float margin = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
            return rect;
        }

        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Image(string name, Transform parent, Color color, bool raycast = false)
        {
            RectTransform rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = White;
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        public static Image FullscreenBackdrop(Transform parent, Color color, bool blockRaycasts = true)
        {
            Image image = Image("Backdrop", parent, color, blockRaycasts);
            Stretch(image.rectTransform);
            return image;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, Color? color = null, FontStyles style = FontStyles.Normal)
        {
            RectTransform rect = Rect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = color ?? TextColor;
            label.fontStyle = style;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            Localize(label);
            return label;
        }

        /// <summary>
        /// Gives a baked label its English twin when its text is a known UI or content string, so it follows the language
        /// chosen in the menu (<see cref="LocalizedLabel"/>). Labels that views fill at runtime are left alone.
        /// </summary>
        public static void Localize(TMP_Text label)
        {
            if (label == null || !Translations.TryEnglish(label.text, out string english) || english == label.text) return;
            if (!label.TryGetComponent(out LocalizedLabel localized)) localized = label.gameObject.AddComponent<LocalizedLabel>();
            localized.Configure(label, label.text, english);
        }

        public static Image FillBar(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color fill)
        {
            Image background = Image(name, parent, new Color(0f, 0f, 0f, 0.6f));
            Place(background.rectTransform, anchor, position, size);
            Image bar = Image("Fill", background.transform, fill);
            Stretch(bar.rectTransform, 3f);
            bar.type = UnityEngine.UI.Image.Type.Filled;
            bar.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            bar.fillOrigin = 0;
            bar.fillAmount = 1f;
            return bar;
        }

        public static Button Button(string name, Transform parent, string label, float width = 460f, float height = 62f)
        {
            Image image = Image(name, parent, Color.white, raycast: true);
            image.rectTransform.sizeDelta = new Vector2(width, height);
            var layout = image.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = ButtonColors();
            TextMeshProUGUI text = Text("Label", image.transform, label, 30f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 6f);
            image.gameObject.AddComponent<ButtonSfx>();
            return button;
        }

        public static ColorBlock ButtonColors() => new()
        {
            normalColor = new Color(0.1f, 0.08f, 0.2f, 0.95f),
            highlightedColor = new Color(0.12f, 0.45f, 0.55f, 1f),
            pressedColor = new Color(0.7f, 0.12f, 0.45f, 1f),
            selectedColor = new Color(0.12f, 0.45f, 0.55f, 1f),
            disabledColor = new Color(0.12f, 0.12f, 0.15f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        public static VerticalLayoutGroup Vertical(GameObject go, float spacing, int padding = 0, TextAnchor alignment = TextAnchor.UpperCenter)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static HorizontalLayoutGroup Horizontal(GameObject go, float spacing, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static RectTransform Row(string name, Transform parent, float width, float height)
        {
            RectTransform row = Rect(name, parent);
            row.sizeDelta = new Vector2(width, height);
            Horizontal(row.gameObject, 20f);
            return row;
        }

        public static Slider Slider(string name, Transform parent, string label, float width = 760f)
        {
            RectTransform row = Row(name, parent, width, 54f);
            TextMeshProUGUI text = Text("Label", row, label, 28f);
            text.rectTransform.sizeDelta = new Vector2(260f, 54f);
            GameObject sliderGo = DefaultControls.CreateSlider(Resources());
            sliderGo.name = "Slider";
            sliderGo.transform.SetParent(row, false);
            ((RectTransform)sliderGo.transform).sizeDelta = new Vector2(width - 300f, 28f);
            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            slider.colors = ButtonColors();
            Tint(sliderGo.transform.Find("Background"), new Color(0.1f, 0.1f, 0.18f));
            Tint(sliderGo.transform.Find("Fill Area/Fill"), Cyan);
            Tint(sliderGo.transform.Find("Handle Slide Area/Handle"), Color.white);
            SetLayer(sliderGo);
            return slider;
        }

        public static Toggle Toggle(string name, Transform parent, string label, float width = 760f)
        {
            RectTransform row = Row(name, parent, width, 54f);
            TextMeshProUGUI text = Text("Label", row, label, 28f);
            text.rectTransform.sizeDelta = new Vector2(260f, 54f);
            GameObject toggleGo = DefaultControls.CreateToggle(Resources());
            toggleGo.name = "Toggle";
            toggleGo.transform.SetParent(row, false);
            ((RectTransform)toggleGo.transform).sizeDelta = new Vector2(40f, 40f);
            Object.DestroyImmediate(toggleGo.transform.Find("Label").gameObject);
            Transform background = toggleGo.transform.Find("Background");
            var bgRect = (RectTransform)background;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Tint(background, new Color(0.1f, 0.1f, 0.18f));
            Tint(background.Find("Checkmark"), Cyan);
            var toggle = toggleGo.GetComponent<Toggle>();
            toggle.colors = ButtonColors();
            SetLayer(toggleGo);
            return toggle;
        }

        public static TMP_Dropdown Dropdown(string name, Transform parent, string label, float width = 760f)
        {
            RectTransform row = Row(name, parent, width, 54f);
            TextMeshProUGUI text = Text("Label", row, label, 28f);
            text.rectTransform.sizeDelta = new Vector2(260f, 54f);
            var resources = new TMP_DefaultControls.Resources { standard = White, background = White, inputField = White, knob = White, checkmark = White, dropdown = White, mask = White };
            GameObject dropdownGo = TMP_DefaultControls.CreateDropdown(resources);
            dropdownGo.name = "Dropdown";
            dropdownGo.transform.SetParent(row, false);
            ((RectTransform)dropdownGo.transform).sizeDelta = new Vector2(width - 300f, 46f);
            var dropdown = dropdownGo.GetComponent<TMP_Dropdown>();
            dropdown.colors = ButtonColors();
            foreach (TMP_Text t in dropdownGo.GetComponentsInChildren<TMP_Text>(true))
            {
                t.fontSize = 24f;
                t.color = TextColor;
            }

            Transform template = dropdownGo.transform.Find("Template");
            if (template != null) Tint(template, Panel);
            Transform item = dropdownGo.transform.Find("Template/Viewport/Content/Item/Item Background");
            if (item != null) Tint(item, new Color(0.1f, 0.1f, 0.2f));
            SetLayer(dropdownGo);
            return dropdown;
        }

        public static CanvasGroup Group(GameObject go)
        {
            if (!go.TryGetComponent(out CanvasGroup group)) group = go.AddComponent<CanvasGroup>();
            return group;
        }

        public static void Tint(Transform target, Color color)
        {
            if (target != null && target.TryGetComponent(out Image image))
            {
                image.sprite = White;
                image.color = color;
            }
        }

        private static DefaultControls.Resources Resources() => new()
        {
            standard = White, background = White, inputField = White, knob = White, checkmark = White, dropdown = White, mask = White
        };

        private static void SetLayer(GameObject root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Core.Layers.UI;
        }

        public static void SetPanel(UIPanel panel, Selectable firstSelected, bool startVisible = false)
        {
            EditorAssetUtil.SetFields(panel, so =>
            {
                so.Require("firstSelected").objectReferenceValue = firstSelected;
                so.Require("startVisible").boolValue = startVisible;
            });
        }
    }
}
