using RootAccess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>Shown on GameState.GameOver: retry from the last checkpoint or return to the main menu.</summary>
    public sealed class GameOverView : UIPanel
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        protected override void Awake()
        {
            base.Awake();
            if (titleLabel != null) titleLabel.text = Strings.Menu.GameOver;
            if (hintLabel != null) hintLabel.text = Strings.Menu.GameOverHint;
            retryButton.onClick.AddListener(() => { if (ServiceLocator.TryGet(out GameManager gm)) gm.RequestRespawn(); });
            mainMenuButton.onClick.AddListener(() => { if (ServiceLocator.TryGet(out GameManager gm)) gm.ReturnToMainMenu(); });
        }

        private void OnEnable() => EventBus<GameStateChanged>.Subscribe(OnGameState);

        private void OnDisable() => EventBus<GameStateChanged>.Unsubscribe(OnGameState);

        private void OnGameState(GameStateChanged evt)
        {
            if (evt.Current == GameState.GameOver) Show();
            else if (IsVisible) Hide();
        }
    }
}
