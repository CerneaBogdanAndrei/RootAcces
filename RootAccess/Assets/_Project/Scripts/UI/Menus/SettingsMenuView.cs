using System;
using System.Collections.Generic;
using RootAccess.Core;
using RootAccess.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>
    /// Shared settings screen (main menu and pause menu): language, mixer volumes, resolution, fullscreen, VSync and
    /// key rebinding. Changes apply immediately and are persisted when the screen closes.
    /// </summary>
    public sealed class SettingsMenuView : UIPanel
    {
        [SerializeField] private InputReader input;
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider uiSlider;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private RebindActionButton[] rebindButtons = new RebindActionButton[0];
        [SerializeField] private Button resetBindingsButton;
        [SerializeField] private Button backButton;

        private readonly List<Resolution> _resolutions = new();
        private SettingsService _service;
        private Action _onClosed;
        private bool _refreshing;

        protected override void Awake()
        {
            base.Awake();
            ServiceLocator.Register(this);
            masterSlider.onValueChanged.AddListener(v => SetVolume(AudioChannel.Master, v));
            musicSlider.onValueChanged.AddListener(v => SetVolume(AudioChannel.Music, v));
            sfxSlider.onValueChanged.AddListener(v => SetVolume(AudioChannel.Sfx, v));
            uiSlider.onValueChanged.AddListener(v => SetVolume(AudioChannel.UI, v));
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            fullscreenToggle.onValueChanged.AddListener(v => { if (!_refreshing) _service?.SetFullscreen(v); });
            vsyncToggle.onValueChanged.AddListener(v => { if (!_refreshing) _service?.SetVSync(v); });
            resetBindingsButton.onClick.AddListener(OnResetBindings);
            backButton.onClick.AddListener(Close);
            foreach (RebindActionButton button in rebindButtons)
                if (button != null) button.Rebound += SaveBindings;
        }

        private void OnEnable()
        {
            EventBus<GameStateChanged>.Subscribe(OnGameStateChanged);
            CommandLineAutomation.CommandRequested += OnAutomationCommand;
        }

        private void OnDisable()
        {
            EventBus<GameStateChanged>.Unsubscribe(OnGameStateChanged);
            CommandLineAutomation.CommandRequested -= OnAutomationCommand;
        }

        private void OnAutomationCommand(string command)
        {
            if (command == AutomationCommands.Settings && !IsVisible) Open(null);
        }

        private void OnDestroy() => ServiceLocator.Unregister(this);

        public void Open(Action onClosed)
        {
            _onClosed = onClosed;
            ServiceLocator.TryGet(out _service);
            Refresh();
            Show();
        }

        public void Close()
        {
            if (!IsVisible) return;
            RebindService.Cancel();
            _service?.Save();
            Hide();
            Action callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }

        private void Refresh()
        {
            if (_service == null) return;
            _refreshing = true;
            GameSettings s = _service.Current;
            masterSlider.SetValueWithoutNotify(s.masterVolume);
            musicSlider.SetValueWithoutNotify(s.musicVolume);
            sfxSlider.SetValueWithoutNotify(s.sfxVolume);
            uiSlider.SetValueWithoutNotify(s.uiVolume);
            fullscreenToggle.SetIsOnWithoutNotify(s.fullscreen);
            vsyncToggle.SetIsOnWithoutNotify(s.vSync);
            BuildLanguages(s);
            BuildResolutions(s);
            foreach (RebindActionButton button in rebindButtons)
                if (button != null) button.Bind(input);
            _refreshing = false;
        }

        /// <summary>Each language is listed under its own name, so it can be found whichever one is active.</summary>
        private void BuildLanguages(GameSettings settings)
        {
            if (languageDropdown == null) return;
            var labels = new List<string>();
            foreach (Language language in (Language[])Enum.GetValues(typeof(Language)))
                labels.Add(Loc.NativeName(language));
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(labels);
            languageDropdown.SetValueWithoutNotify((int)settings.Language);
        }

        private void OnLanguageChanged(int index)
        {
            if (_refreshing || _service == null || !Enum.IsDefined(typeof(Language), index)) return;
            _service.SetLanguage((Language)index);
        }

        private void BuildResolutions(GameSettings settings)
        {
            _resolutions.Clear();
            var labels = new List<string>();
            int selected = 0;
            int currentWidth = settings.resolutionWidth > 0 ? settings.resolutionWidth : Screen.width;
            int currentHeight = settings.resolutionHeight > 0 ? settings.resolutionHeight : Screen.height;
            foreach (Resolution resolution in Screen.resolutions)
            {
                bool duplicate = _resolutions.Exists(r => r.width == resolution.width && r.height == resolution.height);
                if (duplicate || resolution.width < 1024) continue;
                _resolutions.Add(resolution);
                labels.Add($"{resolution.width} × {resolution.height}");
                if (resolution.width == currentWidth && resolution.height == currentHeight) selected = labels.Count - 1;
            }

            if (_resolutions.Count == 0)
            {
                _resolutions.Add(new Resolution { width = currentWidth, height = currentHeight });
                labels.Add($"{currentWidth} × {currentHeight}");
            }

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
            resolutionDropdown.SetValueWithoutNotify(selected);
        }

        private void SetVolume(AudioChannel channel, float value)
        {
            if (!_refreshing) _service?.SetVolume(channel, value);
        }

        private void OnResolutionChanged(int index)
        {
            if (_refreshing || _service == null || index < 0 || index >= _resolutions.Count) return;
            _service.SetResolution(_resolutions[index].width, _resolutions[index].height);
        }

        private void OnResetBindings()
        {
            if (input == null) return;
            RebindService.ResetAll(input.Asset);
            SaveBindings();
            foreach (RebindActionButton button in rebindButtons)
                if (button != null) button.RefreshLabel();
        }

        private void SaveBindings()
        {
            if (input != null) _service?.SetBindingOverrides(input.SaveBindingOverrides());
        }

        private void OnGameStateChanged(GameStateChanged evt)
        {
            if (IsVisible && evt.Current is GameState.Loading or GameState.Playing) Close();
        }
    }
}
