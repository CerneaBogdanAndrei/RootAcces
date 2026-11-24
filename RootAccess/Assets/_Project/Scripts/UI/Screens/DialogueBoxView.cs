using RootAccess.Core;
using RootAccess.Input;
using RootAccess.Systems;
using TMPro;
using UnityEngine;

namespace RootAccess.UI
{
    /// <summary>Bottom-screen dialogue box with typewriter text; Submit completes the line or advances.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DialogueBoxView : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private float charactersPerSecond = 45f;

        private CanvasGroup _group;
        private float _visibleCharacters;
        private int _totalCharacters;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            SetVisible(false);
        }

        private void OnEnable()
        {
            EventBus<DialogueLineShown>.Subscribe(OnLine);
            EventBus<DialogueEnded>.Subscribe(OnEnded);
            if (input != null) input.UISubmitEvent += OnSubmit;
        }

        private void OnDisable()
        {
            EventBus<DialogueLineShown>.Unsubscribe(OnLine);
            EventBus<DialogueEnded>.Unsubscribe(OnEnded);
            if (input != null) input.UISubmitEvent -= OnSubmit;
        }

        private void Update()
        {
            if (_group.alpha <= 0f || bodyLabel == null || _visibleCharacters >= _totalCharacters) return;
            _visibleCharacters += charactersPerSecond * Time.unscaledDeltaTime;
            bodyLabel.maxVisibleCharacters = Mathf.FloorToInt(_visibleCharacters);
        }

        private void OnLine(DialogueLineShown evt)
        {
            SetVisible(true);
            if (speakerLabel != null) speakerLabel.text = evt.Line.Speaker;
            if (bodyLabel == null) return;
            bodyLabel.text = evt.Line.Text;
            _totalCharacters = evt.Line.Text?.Length ?? 0;
            _visibleCharacters = 0f;
            bodyLabel.maxVisibleCharacters = 0;
        }

        private void OnEnded(DialogueEnded _) => SetVisible(false);

        private void OnSubmit()
        {
            if (_group.alpha <= 0f) return;
            if (_visibleCharacters < _totalCharacters)
            {
                _visibleCharacters = _totalCharacters;
                if (bodyLabel != null) bodyLabel.maxVisibleCharacters = _totalCharacters;
                return;
            }

            if (ServiceLocator.TryGet(out DialogueService dialogue)) dialogue.Advance();
        }

        private void SetVisible(bool visible)
        {
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = false;
        }
    }
}
