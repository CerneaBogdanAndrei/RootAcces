using System;
using RootAccess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>
    /// Co-op menu: local split-screen (both players on this PC), LAN host (this PC plays the Infiltrator) or join a LAN
    /// host by IP (this PC becomes the Operator console). Local and host start a new game in that mode.
    /// </summary>
    public sealed class CoopMenuView : UIPanel
    {
        [SerializeField] private Button localButton;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_InputField addressField;
        [SerializeField] private TMP_Text statusLabel;

        private Action _onClosed;

        protected override void Awake()
        {
            base.Awake();
            localButton.onClick.AddListener(OnLocal);
            hostButton.onClick.AddListener(OnHost);
            joinButton.onClick.AddListener(OnJoin);
            backButton.onClick.AddListener(Close);
        }

        public void Open(Action onClosed)
        {
            _onClosed = onClosed;
            SetStatus(string.Empty);
            Show();
        }

        public void Close()
        {
            if (!IsVisible) return;
            Hide();
            _onClosed?.Invoke();
            _onClosed = null;
        }

        private void OnLocal()
        {
            if (!ServiceLocator.TryGet(out IOperatorConsole console) || !ServiceLocator.TryGet(out ISessionService session)) return;
            console.StartLocal();
            session.NewGame();
        }

        private void OnHost()
        {
            if (!ServiceLocator.TryGet(out IOperatorConsole console) || !ServiceLocator.TryGet(out ISessionService session)) return;
            if (console.Host()) session.NewGame();
            else SetStatus(console.Status);
        }

        private void OnJoin()
        {
            if (!ServiceLocator.TryGet(out IOperatorConsole console)) return;
            string address = addressField != null ? addressField.text : string.Empty;
            if (!console.Join(address))
            {
                SetStatus(console.Status);
                return;
            }

            if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.LoadScene(SceneNames.OperatorConsole);
        }

        private void SetStatus(string text)
        {
            if (statusLabel != null) statusLabel.text = text;
        }
    }
}
