using RootAccess.Core;
using RootAccess.Input;
using TMPro;
using UnityEngine;

namespace RootAccess.UI
{
    /// <summary>Scrolling credits; Esc / Submit / end of scroll returns to the main menu.</summary>
    public sealed class CreditsView : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [SerializeField] private RectTransform scroller;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private float speed = 45f;
        [SerializeField] private float endY = 1400f;

        private bool _leaving;

        private void Awake()
        {
            if (body != null) body.text = $"<size=140%><b>{Strings.Credits.Title}</b></size>\n\n{Strings.Credits.Body}";
            if (hint != null) hint.text = Strings.Credits.BackHint;
        }

        private void OnEnable()
        {
            if (input == null) return;
            input.UICancelEvent += Leave;
            input.UISubmitEvent += Leave;
        }

        private void OnDisable()
        {
            if (input == null) return;
            input.UICancelEvent -= Leave;
            input.UISubmitEvent -= Leave;
        }

        private void Update()
        {
            if (scroller == null) return;
            scroller.anchoredPosition += Vector2.up * (speed * Time.unscaledDeltaTime);
            if (scroller.anchoredPosition.y > endY) Leave();
        }

        private void Leave()
        {
            if (_leaving) return;
            _leaving = true;
            if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.ReturnToMainMenu();
        }
    }
}
