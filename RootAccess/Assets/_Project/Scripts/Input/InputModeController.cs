using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Input
{
    /// <summary>Switches the active action map whenever the game state changes; applies saved rebinds.</summary>
    public sealed class InputModeController : MonoBehaviour
    {
        [SerializeField] private InputReader inputReader;

        private bool _overridesApplied;

        private void Awake()
        {
            if (inputReader == null)
            {
                Debug.LogError("[InputModeController] InputReader reference missing.");
                enabled = false;
                return;
            }

            ServiceLocator.Register(inputReader);
        }

        private void OnEnable()
        {
            EventBus<GameStateChanged>.Subscribe(OnGameStateChanged);
            EventBus<SettingsChanged>.Subscribe(OnSettingsChanged);
        }

        private void OnDisable()
        {
            EventBus<GameStateChanged>.Unsubscribe(OnGameStateChanged);
            EventBus<SettingsChanged>.Unsubscribe(OnSettingsChanged);
        }

        private void OnDestroy()
        {
            if (inputReader != null) ServiceLocator.Unregister(inputReader);
        }

        public static InputMode ModeFor(GameState state) => state switch
        {
            GameState.Playing => InputMode.Gameplay,
            GameState.Hacking => InputMode.Hacking,
            GameState.Loading or GameState.Boot => InputMode.None,
            _ => InputMode.UI
        };

        private void OnGameStateChanged(GameStateChanged evt) => inputReader.SetMode(ModeFor(evt.Current));

        private void OnSettingsChanged(SettingsChanged evt)
        {
            if (_overridesApplied || evt.Settings == null) return;
            inputReader.LoadBindingOverrides(evt.Settings.bindingOverridesJson);
            _overridesApplied = true;
        }
    }
}
