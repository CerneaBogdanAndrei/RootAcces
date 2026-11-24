using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>Base for full-screen panels: animated show/hide and gamepad-friendly initial selection.</summary>
    [RequireComponent(typeof(CanvasGroup), typeof(UITween))]
    public abstract class UIPanel : MonoBehaviour
    {
        [SerializeField] private Selectable firstSelected;
        [SerializeField] private bool startVisible;

        private UITween _tween;

        public bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            _tween = GetComponent<UITween>();
            IsVisible = startVisible;
            _tween.Play(startVisible, instant: true);
        }

        public virtual void Show()
        {
            IsVisible = true;
            _tween.Play(true);
            SelectFirst();
        }

        public virtual void Hide()
        {
            IsVisible = false;
            _tween.Play(false);
        }

        public void SelectFirst()
        {
            if (firstSelected != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
        }
    }
}
