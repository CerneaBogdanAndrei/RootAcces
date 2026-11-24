using UnityEngine;
using UnityEngine.SceneManagement;

namespace RootAccess.Core
{
    /// <summary>
    /// Owns the high-level game state (menu, playing, paused, hacking, game over...), time scale and
    /// scene transitions. Lives on the persistent SystemsRoot prefab.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private float gameOverDelaySeconds = 1.2f;

        private float _gameOverAt = -1f;

        public GameState State { get; private set; } = GameState.Boot;
        public SettingsService Settings { get; private set; }
        public string PendingSpawnId { get; private set; }
        public bool IsPaused => State == GameState.Paused;

        private void Awake()
        {
            if (sceneLoader == null)
            {
                Debug.LogError("[GameManager] SceneLoader reference missing on SystemsRoot.");
                enabled = false;
                return;
            }

            ServiceLocator.Register(this);
            Settings = new SettingsService(new PlayerPrefsSettingsStore());
            Settings.Load();
            Settings.ApplyDisplay(!CommandLineAutomation.OverridesScreenMode);
            ServiceLocator.Register(Settings);
        }

        private void Start()
        {
            Settings.NotifyChanged();
            SetState(StateForScene(SceneManager.GetActiveScene().name));
        }

        private void OnEnable()
        {
            EventBus<SceneLoadStarted>.Subscribe(OnSceneLoadStarted);
            EventBus<SceneLoadCompleted>.Subscribe(OnSceneLoadCompleted);
            EventBus<PlayerDied>.Subscribe(OnPlayerDied);
            EventBus<PlayerRespawned>.Subscribe(OnPlayerRespawned);
            EventBus<HackSessionStarted>.Subscribe(OnHackStarted);
            EventBus<HackSessionEnded>.Subscribe(OnHackEnded);
            EventBus<LevelCompleted>.Subscribe(OnLevelCompleted);
        }

        private void OnDisable()
        {
            EventBus<SceneLoadStarted>.Unsubscribe(OnSceneLoadStarted);
            EventBus<SceneLoadCompleted>.Unsubscribe(OnSceneLoadCompleted);
            EventBus<PlayerDied>.Unsubscribe(OnPlayerDied);
            EventBus<PlayerRespawned>.Unsubscribe(OnPlayerRespawned);
            EventBus<HackSessionStarted>.Unsubscribe(OnHackStarted);
            EventBus<HackSessionEnded>.Unsubscribe(OnHackEnded);
            EventBus<LevelCompleted>.Unsubscribe(OnLevelCompleted);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
            if (Settings != null) ServiceLocator.Unregister(Settings);
        }

        private void Update()
        {
            if (_gameOverAt > 0f && Time.unscaledTime >= _gameOverAt)
            {
                _gameOverAt = -1f;
                SetState(GameState.GameOver);
            }
        }

        public void SetState(GameState next)
        {
            if (next == State) return;
            GameState previous = State;
            State = next;
            Time.timeScale = next is GameState.Paused or GameState.GameOver or GameState.LevelComplete ? 0f : 1f;
            EventBus<GameStateChanged>.Publish(new GameStateChanged(previous, next));
        }

        public void LoadScene(string sceneName, string spawnId = null)
        {
            PendingSpawnId = spawnId;
            _gameOverAt = -1f;
            Time.timeScale = 1f;
            sceneLoader.Load(sceneName);
        }

        public string ConsumePendingSpawnId()
        {
            string id = PendingSpawnId;
            PendingSpawnId = null;
            return id;
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void ReturnToMainMenu() => LoadScene(SceneNames.MainMenu);

        public void RequestRespawn()
        {
            if (State != GameState.GameOver) return;
            SetState(GameState.Playing);
            EventBus<PlayerRespawnRequested>.Publish(new PlayerRespawnRequested());
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public static GameState StateForScene(string sceneName) =>
            SceneNames.IsGameplayScene(sceneName) ? GameState.Playing : GameState.MainMenu;

        private void OnSceneLoadStarted(SceneLoadStarted _) => SetState(GameState.Loading);

        private void OnSceneLoadCompleted(SceneLoadCompleted evt) => SetState(StateForScene(evt.SceneName));

        private void OnPlayerDied(PlayerDied _) => _gameOverAt = Time.unscaledTime + gameOverDelaySeconds;

        private void OnPlayerRespawned(PlayerRespawned _) => _gameOverAt = -1f;

        private void OnHackStarted(HackSessionStarted _)
        {
            if (State == GameState.Playing) SetState(GameState.Hacking);
        }

        private void OnHackEnded(HackSessionEnded _)
        {
            if (State == GameState.Hacking) SetState(GameState.Playing);
        }

        private void OnLevelCompleted(LevelCompleted _) => SetState(GameState.LevelComplete);
    }
}
