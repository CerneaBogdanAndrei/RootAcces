using System;
using RootAccess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RootAccess.Hacking
{
    /// <summary>
    /// Base view for a hacking minigame hosted by <see cref="HackingOverlay"/>. Subclasses build their UI from a
    /// button template at runtime and report success or failure exactly once through <see cref="Complete"/>.
    /// </summary>
    public abstract class HackingMinigame : MonoBehaviour
    {
        [SerializeField] protected RectTransform content;
        [SerializeField] protected Button buttonTemplate;
        [SerializeField] protected TMP_Text statusLabel;

        /// <summary>Neutral tints so a cell's own colour (wire colour, flash colour) is shown as-is; selection only brightens it.</summary>
        private static readonly ColorBlock MinigameColors = new()
        {
            normalColor = Color.white,
            highlightedColor = new Color(1f, 1f, 1f, 1f),
            selectedColor = new Color(0.85f, 1f, 1f, 1f),
            pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f),
            disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.8f),
            colorMultiplier = 1.15f,
            fadeDuration = 0.06f
        };

        public event Action<bool> Finished;

        public abstract HackingMinigameType Type { get; }
        public abstract string Hint { get; }
        public bool IsRunning { get; private set; }

        protected virtual void Awake()
        {
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
        }

        public void Begin(int difficulty, int seed)
        {
            gameObject.SetActive(true);
            IsRunning = true;
            OnBegin(Mathf.Clamp(difficulty, 1, 5), seed);
        }

        public void Abort()
        {
            if (!IsRunning) return;
            IsRunning = false;
            OnAbort();
        }

        public virtual void OnSubmit() { }
        public virtual void OnNavigate(Vector2 direction) { }
        public virtual void OnClick() { }

        protected abstract void OnBegin(int difficulty, int seed);
        protected virtual void OnAbort() { }

        protected void Complete(bool success)
        {
            if (!IsRunning) return;
            IsRunning = false;
            EventBus<SfxRequested>.Publish(SfxRequested.Ui(success ? SfxId.HackSuccess : SfxId.HackFail));
            Finished?.Invoke(success);
        }

        protected void SetStatus(string text)
        {
            if (statusLabel != null) statusLabel.text = text;
        }

        protected void ClearContent()
        {
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform child = content.GetChild(i);
                if (buttonTemplate != null && child == buttonTemplate.transform) continue;
                Destroy(child.gameObject);
            }
        }

        protected Button CreateButton(string label, Color color, Action onClick)
        {
            Button button = Instantiate(buttonTemplate, content);
            button.gameObject.SetActive(true);
            button.name = label;
            button.colors = MinigameColors;
            if (button.TryGetComponent(out Image image)) image.color = color;
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null) text.text = label;
            button.onClick.AddListener(() => onClick());
            return button;
        }

        protected static void Select(Selectable selectable)
        {
            if (selectable != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
