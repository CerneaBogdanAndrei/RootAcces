using RootAccess.Core;
using RootAccess.Input;
using TMPro;
using UnityEngine;

namespace RootAccess.Hacking
{
    /// <summary>
    /// Full-screen hacking UI. Receives <see cref="HackRequested"/>, runs the matching minigame (falls back to
    /// Pattern Memory), shows the result and answers with <see cref="HackResolved"/>.
    /// </summary>
    public sealed class HackingOverlay : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private InputReader input;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private TMP_Text resultLabel;
        [SerializeField] private HackingMinigame[] minigames = new HackingMinigame[0];
        [SerializeField] private float resultSeconds = 0.9f;
        [SerializeField] private Color successColor = new(0.3f, 1f, 0.6f);
        [SerializeField] private Color failureColor = new(1f, 0.3f, 0.35f);

        private HackingMinigame _active;
        private int _requestId = -1;
        private float _closeAt = -1f;
        private bool _lastSuccess;

        public bool IsOpen => _requestId >= 0;

        private void Awake()
        {
            SetVisible(false);
            foreach (HackingMinigame minigame in minigames)
                if (minigame != null) minigame.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus<HackRequested>.Subscribe(OnHackRequested);
            if (input == null) return;
            input.HackCancelEvent += OnCancel;
            input.HackSubmitEvent += OnSubmit;
            input.HackClickEvent += OnClick;
            input.HackNavigateEvent += OnNavigate;
        }

        private void OnDisable()
        {
            EventBus<HackRequested>.Unsubscribe(OnHackRequested);
            if (input == null) return;
            input.HackCancelEvent -= OnCancel;
            input.HackSubmitEvent -= OnSubmit;
            input.HackClickEvent -= OnClick;
            input.HackNavigateEvent -= OnNavigate;
        }

        private void Update()
        {
            if (_closeAt < 0f || Time.unscaledTime < _closeAt) return;
            _closeAt = -1f;
            Close(_lastSuccess, aborted: false);
        }

        private void OnHackRequested(HackRequested request)
        {
            if (IsOpen)
            {
                EventBus<HackResolved>.Publish(new HackResolved(request.RequestId, false, true));
                return;
            }

            _active = Find(request.Minigame) ?? Find(HackingMinigameType.PatternMemory);
            if (_active == null)
            {
                Debug.LogError("[HackingOverlay] No minigame views assigned.");
                EventBus<HackResolved>.Publish(new HackResolved(request.RequestId, false, true));
                return;
            }

            _requestId = request.RequestId;
            if (titleLabel != null) titleLabel.text = Strings.Hacking.Title;
            if (hintLabel != null) hintLabel.text = $"{_active.Hint}\n{Strings.Hacking.CancelHint}";
            if (resultLabel != null) resultLabel.text = string.Empty;
            SetVisible(true);
            EventBus<HackSessionStarted>.Publish(new HackSessionStarted(request.TerminalPosition));

            _active.Finished += OnMinigameFinished;
            _active.Begin(request.Difficulty, request.RequestId * 7919 + Mathf.FloorToInt(Time.unscaledTime * 10f));
        }

        private HackingMinigame Find(HackingMinigameType type)
        {
            foreach (HackingMinigame minigame in minigames)
                if (minigame != null && minigame.Type == type) return minigame;
            return null;
        }

        private void OnMinigameFinished(bool success)
        {
            _lastSuccess = success;
            if (resultLabel != null)
            {
                resultLabel.text = success ? Strings.Hacking.Success : Strings.Hacking.Failure;
                resultLabel.color = success ? successColor : failureColor;
            }

            _closeAt = Time.unscaledTime + resultSeconds;
        }

        private void OnCancel()
        {
            if (!IsOpen || _closeAt >= 0f) return;
            _active.Abort();
            Close(false, aborted: true);
        }

        private void OnSubmit()
        {
            if (IsOpen && _active.IsRunning) _active.OnSubmit();
        }

        private void OnClick()
        {
            if (IsOpen && _active.IsRunning) _active.OnClick();
        }

        private void OnNavigate(Vector2 direction)
        {
            if (IsOpen && _active.IsRunning) _active.OnNavigate(direction);
        }

        private void Close(bool success, bool aborted)
        {
            int requestId = _requestId;
            _requestId = -1;
            if (_active != null)
            {
                _active.Finished -= OnMinigameFinished;
                _active.gameObject.SetActive(false);
                _active = null;
            }

            SetVisible(false);
            EventBus<HackResolved>.Publish(new HackResolved(requestId, success, aborted));
            EventBus<HackSessionEnded>.Publish(new HackSessionEnded(success));
        }

        private void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
