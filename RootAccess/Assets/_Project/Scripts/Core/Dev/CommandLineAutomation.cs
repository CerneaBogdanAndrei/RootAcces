using System;
using System.IO;
using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>
    /// Development/CI hooks for the built player:
    /// <c>-ra-scene Level01_Rooftops -ra-spawn l1_drone -ra-newgame -ra-screenshot shot.png -ra-quit-after 8</c>.
    /// Lets scripts smoke-test any scene of a real build and capture an in-game screenshot without touching the desktop.
    /// <c>-ra-interact ShopTerminal</c> uses a named interactable as the player would (E), and <c>-ra-command a,b</c>
    /// broadcasts commands (<see cref="AutomationCommands"/>) that screens handle themselves, e.g. to photograph menus.
    /// </summary>
    public sealed class CommandLineAutomation : MonoBehaviour
    {
        private const string SceneArg = "-ra-scene";
        private const string SpawnArg = "-ra-spawn";
        private const string NewGameArg = "-ra-newgame";
        private const string ScreenshotArg = "-ra-screenshot";
        private const string ScreenshotDelayArg = "-ra-screenshot-delay";
        private const string QuitAfterArg = "-ra-quit-after";
        private const string InteractArg = "-ra-interact";
        private const string CommandArg = "-ra-command";
        private const string CommandDelayArg = "-ra-command-delay";
        private const string DefaultSettingsArg = "-ra-default-settings";
        private const string CoopArg = "-ra-coop";
        private const string CoopAddressArg = "-ra-coop-address";
        private const string LanguageArg = "-ra-language";

        private string _targetScene;
        private string _spawnId;
        private string _screenshotPath;
        private float _screenshotDelay = 3f;
        private float _quitAt = -1f;
        private float _screenshotAt = -1f;
        private bool _newGame;
        private bool _redirected;
        private string _interactTarget;
        private string[] _commands = new string[0];
        private float _commandDelay = 1.5f;
        private float _commandAt = -1f;
        private string _coop;
        private string _coopAddress;

        /// <summary>Raised for each <c>-ra-command</c> entry once the target scene is ready (see <see cref="AutomationCommands"/>).</summary>
        public static event Action<string> CommandRequested;

        public bool IsActive { get; private set; }

        /// <summary>True with -ra-default-settings: settings live in memory only (defaults, nothing written to PlayerPrefs).</summary>
        public static bool UsesDefaultSettings => Array.IndexOf(Environment.GetCommandLineArgs(), DefaultSettingsArg) >= 0;

        /// <summary><c>-ra-language en|ro</c>: start in that language (screenshots of the English version).</summary>
        public static Language? LanguageOverride
        {
            get
            {
                string value = Value(Environment.GetCommandLineArgs(), LanguageArg);
                if (string.IsNullOrEmpty(value)) return null;
                return value.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? Language.English : Language.Romanian;
            }
        }

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
            _interactTarget = Value(args, InteractArg);
            string commands = Value(args, CommandArg);
            if (!string.IsNullOrEmpty(commands)) _commands = commands.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (float.TryParse(Value(args, CommandDelayArg), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float commandDelay))
                _commandDelay = commandDelay;
            _coop = Value(args, CoopArg);
            _coopAddress = Value(args, CoopAddressArg);
            if (_coop == "join" && string.IsNullOrEmpty(_targetScene)) _targetScene = SceneNames.OperatorConsole;
            IsActive = _targetScene != null || _screenshotPath != null || _quitAt > 0f || _interactTarget != null || _commands.Length > 0 || _coop != null;
            if (IsActive) Debug.Log($"[Automation] scene={_targetScene} spawn={_spawnId} newGame={_newGame} screenshot={_screenshotPath} quitAt={_quitAt}");
        }

        private void OnEnable() => EventBus<SceneLoadCompleted>.Subscribe(OnSceneLoaded);

        private void OnDisable() => EventBus<SceneLoadCompleted>.Unsubscribe(OnSceneLoaded);

        private void Update()
        {
            if (_commandAt > 0f && Time.realtimeSinceStartup >= _commandAt)
            {
                _commandAt = -1f;
                RunCommands();
            }

            if (_screenshotAt > 0f && Time.realtimeSinceStartup >= _screenshotAt)
            {
                _screenshotAt = -1f;
                string directory = Path.GetDirectoryName(Path.GetFullPath(_screenshotPath));
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                ScreenCapture.CaptureScreenshot(_screenshotPath);
                Debug.Log($"[Automation] Screenshot requested: {_screenshotPath}");
                LogPlayerDiagnostics("screenshot");
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
                StartCoop();
                if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.LoadScene(_targetScene, _spawnId);
                return;
            }

            bool arrived = string.IsNullOrEmpty(_targetScene) || evt.SceneName == _targetScene;
            if (arrived && IsActive) LogPlayerDiagnostics($"arrived in {evt.SceneName}");
            if (arrived && (_interactTarget != null || _commands.Length > 0) && _commandAt < 0f)
                _commandAt = Time.realtimeSinceStartup + _commandDelay;
            if (arrived && !string.IsNullOrEmpty(_screenshotPath) && _screenshotAt < 0f)
                _screenshotAt = Time.realtimeSinceStartup + _screenshotDelay;
        }

        /// <summary><c>-ra-coop local|host|join</c> (+ <c>-ra-coop-address ip</c>): start co-op before the target scene loads.</summary>
        private void StartCoop()
        {
            if (string.IsNullOrEmpty(_coop) || !ServiceLocator.TryGet(out IOperatorConsole console)) return;
            switch (_coop)
            {
                case "local":
                    console.StartLocal();
                    break;
                case "host":
                    console.Host();
                    break;
                case "join":
                    console.Join(_coopAddress);
                    break;
                default:
                    Debug.LogWarning($"[Automation] Unknown -ra-coop mode '{_coop}' (local, host, join).");
                    return;
            }

            Debug.Log($"[Automation] co-op '{_coop}': {console.Mode} · {console.Status}");
        }

        private void RunCommands()
        {
            if (!string.IsNullOrEmpty(_interactTarget))
            {
                GameObject target = GameObject.Find(_interactTarget);
                bool used = target != null && target.TryGetComponent(out IInteractable interactable) &&
                            ServiceLocator.TryGet(out IPlayerTarget player) && player is IInteractor actor && Use(interactable, actor);
                Debug.Log($"[Automation] interact '{_interactTarget}': {(used ? "ok" : "not found or unavailable")}");
            }

            foreach (string command in _commands)
            {
                Debug.Log($"[Automation] command '{command}'");
                CommandRequested?.Invoke(command.Trim());
            }
        }

        private static bool Use(IInteractable interactable, IInteractor actor)
        {
            if (!interactable.CanInteract(actor)) return false;
            interactable.Interact(actor);
            return true;
        }

        private static void LogPlayerDiagnostics(string moment)
        {
            if (!ServiceLocator.TryGet(out IPlayerTarget player))
            {
                Debug.Log($"[Automation] {moment}: no player registered.");
                return;
            }

            Vector2 origin = player.Position + Vector2.up;
            RaycastHit2D ground = Physics2D.Raycast(origin, Vector2.down, 60f, Layers.SolidMask);
            string groundInfo = ground.collider != null ? $"{ground.collider.name} at y={ground.point.y:0.00}" : "none";
            Debug.Log($"[Automation] {moment}: player pos={player.Position} vel={player.Velocity} alive={player.IsAlive} ground={groundInfo}");
        }

        private static string Value(string[] args, string key)
        {
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
