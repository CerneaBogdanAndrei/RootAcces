using RootAccess.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>UI sounds for a Selectable: hover/selection and click.</summary>
    [RequireComponent(typeof(Selectable))]
    public sealed class ButtonSfx : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        [SerializeField] private SfxId clickSound = SfxId.UiClick;
        [SerializeField] private SfxId hoverSound = SfxId.UiHover;

        private Selectable _selectable;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            if (_selectable is Button button) button.onClick.AddListener(PlayClick);
        }

        public void OnPointerEnter(PointerEventData eventData) => PlayHover();

        public void OnSelect(BaseEventData eventData) => PlayHover();

        private void PlayHover()
        {
            if (_selectable.IsInteractable()) EventBus<SfxRequested>.Publish(SfxRequested.Ui(hoverSound));
        }

        private void PlayClick() => EventBus<SfxRequested>.Publish(SfxRequested.Ui(clickSound));
    }
}
