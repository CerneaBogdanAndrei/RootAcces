using System;
using System.IO;
using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>
    /// Development/CI hooks for the built player:
    /// <c>-ra-scene Level01_Rooftops -ra-spawn l1_drone -ra-newgame -ra-screenshot shot.png -ra-quit-after 8</c>.
    /// Lets scripts smoke-test any scene of a real build and capture an in-game screenshot without touching the desktop.
    /// </summary>
    public sealed class CommandLineAutomation : MonoBehaviour
    {
        private const string SceneArg = "-ra-scene";
        private const string SpawnArg = "-ra-spawn";
        private const string NewGameArg = "-ra-newgame";
        private const string ScreenshotArg = "-ra-screenshot";
        private const string ScreenshotDelayArg = "-ra-screenshot-delay";
        private const string QuitAfterArg = "-ra-quit-after";

        private string _targetScene;
        private string _spawnId;
        private string _screenshotPath;
        private float _screenshotDelay = 3f;
        private float _quitAt = -1f;
        private float _screenshotAt = -1f;
        private bool _newGame;
        private bool _redirected;

        public bool IsActive { get; private set; }

        /// <summary>True when Unity's own -screen-fullscreen / -popupwindow switches were passed (scripts keep that mode).</summary>
        public static bool OverridesScreenMode
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs();
                return Array.IndexOf(args, "-screen-fullscreen") >= 0 || Array.IndexOf(args, "-popupwindow") >= 0;
            }
        }

        private void Awake()
        {
            string[] args = Environment.GetCommandLineArgs();
            _targetScene = Value(args, SceneArg);
            _spawnId = Value(args, SpawnArg);
            _screenshotPath = Value(args, ScreenshotArg);
            _newGame = Array.IndexOf(args, NewGameArg) >= 0;
            if (float.TryParse(Value(args, ScreenshotDelayArg), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float delay))
                _screenshotDelay = delay;
            if (float.TryParse(Value(args, QuitAfterArg), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float quitAfter))
                _quitAt = Time.realtimeSinceStartup + quitAfter;
            IsActive = _targetScene != null || _screenshotPath != null || _quitAt > 0f;
            if (IsActive) Debug.Log($"[Automation] scene={_targetScene} spawn={_spawnId} newGame={_newGame} screenshot={_screenshotPath} quitAt={_quitAt}");
        }

        private void OnEnable() => EventBus<SceneLoadCompleted>.Subscribe(OnSceneLoaded);

        private void OnDisable() => EventBus<SceneLoadCompleted>.Unsubscribe(OnSceneLoaded);

        private void Update()
        {
            if (_screenshotAt > 0f && Time.realtimeSinceStartup >= _screenshotAt)
            {
                _screenshotAt = -1f;
                string directory = Path.GetDirectoryName(Path.GetFullPath(_screenshotPath));
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                ScreenCapture.CaptureScreenshot(_screenshotPath);
                Debug.Log($"[Automation] Screenshot requested: {_screenshotPath}");
            }

            if (_quitAt > 0f && Time.realtimeSinceStartup >= _quitAt)
            {
                _quitAt = -1f;
                Debug.Log("[Automation] Quit requested by command line.");
                Application.Quit();
            }
        }

        private void OnSceneLoaded(SceneLoadCompleted evt)
        {
            bool needsRedirect = !string.IsNullOrEmpty(_targetScene) && !_redirected && evt.SceneName != _targetScene;
            if (needsRedirect && evt.SceneName == SceneNames.MainMenu)
            {
                _redirected = true;
                if (_newGame && ServiceLocator.TryGet(out ISessionService session)) session.ResetSession();
                if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.LoadScene(_targetScene, _spawnId);
                return;
            }

            bool arrived = string.IsNullOrEmpty(_targetScene) || evt.SceneName == _targetScene;
            if (arrived && !string.IsNullOrEmpty(_screenshotPath) && _screenshotAt < 0f)
                _screenshotAt = Time.realtimeSinceStartup + _screenshotDelay;
        }

        private static string Value(string[] args, string key)
        {
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
