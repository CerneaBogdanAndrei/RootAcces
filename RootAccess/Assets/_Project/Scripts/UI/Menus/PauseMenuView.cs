using RootAccess.Core;
using RootAccess.Input;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>Pause menu: Esc / Start toggles; Resume, Settings, Main Menu, Quit.</summary>
    public sealed class PauseMenuView : UIPanel
    {
        [SerializeField] private InputReader input;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button quitButton;

        private GameManager _gameManager;
        private SettingsMenuView _settings;

        protected override void Awake()
        {
            base.Awake();
            resumeButton.onClick.AddListener(Resume);
            settingsButton.onClick.AddListener(OpenSettings);
            mainMenuButton.onClick.AddListener(() => _gameManager?.ReturnToMainMenu());
            quitButton.onClick.AddListener(() => _gameManager?.QuitGame());
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _gameManager);
            ServiceLocator.TryGet(out _settings);
        }

        private void OnEnable()
        {
            EventBus<GameStateChanged>.Subscribe(OnGameStateChanged);
            if (input == null) return;
            input.PauseEvent += OnPausePressed;
            input.UICancelEvent += OnCancelPressed;
        }

        private void OnDisable()
        {
            EventBus<GameStateChanged>.Unsubscribe(OnGameStateChanged);
            if (input == null) return;
            input.PauseEvent -= OnPausePressed;
            input.UICancelEvent -= OnCancelPressed;
        }

        private void OnPausePressed()
        {
            if (_gameManager != null && _gameManager.State == GameState.Playing) _gameManager.TogglePause();
        }

        private void OnCancelPressed()
        {
            if (_gameManager == null || _gameManager.State != GameState.Paused) return;
            if (_settings != null && _settings.IsVisible) _settings.Close();
            else Resume();
        }

        private void Resume() => _gameManager?.Resume();

        private void OpenSettings()
        {
            if (_settings == null) return;
            Hide();
            _settings.Open(Show);
        }

        private void OnGameStateChanged(GameStateChanged evt)
        {
            if (evt.Current == GameState.Paused)
            {
                Show();
                EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.UiBack));
            }
            else if (IsVisible)
            {
                Hide();
            }
        }
    }
}
