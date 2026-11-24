using RootAccess.Core;
using RootAccess.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>Mission summary after a completing <see cref="LevelCompleted"/>: credits, trace, continue.</summary>
    public sealed class LevelCompleteView : UIPanel
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button mainMenuButton;

        private string _nextScene = SceneNames.Hub;

        protected override void Awake()
        {
            base.Awake();
            if (titleLabel != null) titleLabel.text = Strings.Menu.LevelComplete;
            continueButton.onClick.AddListener(() => { if (ServiceLocator.TryGet(out GameManager gm)) gm.LoadScene(_nextScene); });
            mainMenuButton.onClick.AddListener(() => { if (ServiceLocator.TryGet(out GameManager gm)) gm.ReturnToMainMenu(); });
        }

        private void OnEnable()
        {
            EventBus<LevelCompleted>.Subscribe(OnLevelCompleted);
            EventBus<GameStateChanged>.Subscribe(OnGameState);
        }

        private void OnDisable()
        {
            EventBus<LevelCompleted>.Unsubscribe(OnLevelCompleted);
            EventBus<GameStateChanged>.Unsubscribe(OnGameState);
        }

        private void OnLevelCompleted(LevelCompleted evt)
        {
            _nextScene = string.IsNullOrEmpty(evt.NextScene) ? SceneNames.Hub : evt.NextScene;
            if (summaryLabel != null && ServiceLocator.TryGet(out SessionService session))
            {
                GameSession s = session.Session;
                summaryLabel.text = $"{Strings.Hud.Credits}: {s.Wallet.Credits}\nXP: {s.Skills.Xp}\n{Strings.Hud.Trace}: {Mathf.RoundToInt(s.Trace.Value)}%";
            }

            Show();
        }

        private void OnGameState(GameStateChanged evt)
        {
            if (evt.Current != GameState.LevelComplete && IsVisible) Hide();
        }
    }
}
