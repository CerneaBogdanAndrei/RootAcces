using System;
using UnityEngine;

namespace RootAccess.Core
{
    [Serializable]
    public sealed class GameSettings
    {
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 0.9f;
        public float uiVolume = 0.8f;
        public float ambienceVolume = 0.7f;
        public int resolutionWidth;
        public int resolutionHeight;
        public bool fullscreen = true;
        public bool vSync = true;
        public string bindingOverridesJson = string.Empty;
        /// <summary><see cref="Language"/> as an int (JSON); settings saved before the switch existed read as Romanian.</summary>
        public int language;

        public Language Language => System.Enum.IsDefined(typeof(Language), language) ? (Language)language : Language.Romanian;

        public GameSettings Clone() => (GameSettings)MemberwiseClone();

        public float GetVolume(AudioChannel channel) => channel switch
        {
            AudioChannel.Master => masterVolume,
            AudioChannel.Music => musicVolume,
            AudioChannel.Sfx => sfxVolume,
            AudioChannel.UI => uiVolume,
            AudioChannel.Ambience => ambienceVolume,
            _ => 1f
        };

        public void SetVolume(AudioChannel channel, float value)
        {
            value = Mathf.Clamp01(value);
            switch (channel)
            {
                case AudioChannel.Master: masterVolume = value; break;
                case AudioChannel.Music: musicVolume = value; break;
                case AudioChannel.Sfx: sfxVolume = value; break;
                case AudioChannel.UI: uiVolume = value; break;
                case AudioChannel.Ambience: ambienceVolume = value; break;
            }
        }
    }

    /// <summary>Key/value persistence used by <see cref="SettingsService"/>; swapped for an in-memory store in tests.</summary>
    public interface ISettingsStore
    {
        bool HasKey(string key);
        string GetString(string key);
        void SetString(string key, string value);
        void Flush();
    }

    /// <summary>Volatile store: automation runs use it so screenshots show defaults and never overwrite the player's prefs.</summary>
    public sealed class MemorySettingsStore : ISettingsStore
    {
        private readonly System.Collections.Generic.Dictionary<string, string> _values = new();

        public bool HasKey(string key) => _values.ContainsKey(key);
        public string GetString(string key) => _values.TryGetValue(key, out string value) ? value : string.Empty;
        public void SetString(string key, string value) => _values[key] = value;
        public void Flush() { }
    }

    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public string GetString(string key) => PlayerPrefs.GetString(key, string.Empty);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void Flush() => PlayerPrefs.Save();
    }

    /// <summary>Loads, mutates, applies and persists player settings (PlayerPrefs, JSON blob).</summary>
    public sealed class SettingsService
    {
        private readonly ISettingsStore _store;

        public SettingsService(ISettingsStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            Current = new GameSettings();
        }

        public GameSettings Current { get; private set; }

        public void Load()
        {
            if (!_store.HasKey(PlayerPrefsKeys.Settings)) return;
            try
            {
                GameSettings loaded = JsonUtility.FromJson<GameSettings>(_store.GetString(PlayerPrefsKeys.Settings));
                if (loaded != null) Current = loaded;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Settings] Stored settings are unreadable, using defaults: {exception.Message}");
                Current = new GameSettings();
            }
        }

        public void Save()
        {
            _store.SetString(PlayerPrefsKeys.Settings, JsonUtility.ToJson(Current));
            _store.Flush();
        }

        public void SetVolume(AudioChannel channel, float value)
        {
            Current.SetVolume(channel, value);
            NotifyChanged();
        }

        public void SetResolution(int width, int height)
        {
            Current.resolutionWidth = width;
            Current.resolutionHeight = height;
            ApplyDisplay();
            NotifyChanged();
        }

        public void SetFullscreen(bool fullscreen)
        {
            Current.fullscreen = fullscreen;
            ApplyDisplay();
            NotifyChanged();
        }

        public void SetVSync(bool enabled)
        {
            Current.vSync = enabled;
            ApplyDisplay();
            NotifyChanged();
        }

        /// <summary>Switches the game language (labels and open screens redraw at once) and remembers it.</summary>
        public void SetLanguage(Language value)
        {
            Current.language = (int)value;
            ApplyLanguage();
            NotifyChanged();
        }

        /// <summary>Makes the stored language the current one (after <see cref="Load"/>).</summary>
        public void ApplyLanguage() => Loc.Set(Current.Language);

        public void SetBindingOverrides(string json)
        {
            Current.bindingOverridesJson = json ?? string.Empty;
            NotifyChanged();
        }

        /// <param name="applyScreenMode">False when the command line already dictates the window mode (-screen-fullscreen).</param>
        public void ApplyDisplay(bool applyScreenMode = true)
        {
            QualitySettings.vSyncCount = Current.vSync ? 1 : 0;
            if (!Current.vSync) Application.targetFrameRate = 60;
            else Application.targetFrameRate = -1;
            if (!applyScreenMode) return;

            FullScreenMode mode = Current.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Current.resolutionWidth > 0 && Current.resolutionHeight > 0)
                Screen.SetResolution(Current.resolutionWidth, Current.resolutionHeight, mode);
            else
                Screen.fullScreenMode = mode;
        }

        public void NotifyChanged() => EventBus<SettingsChanged>.Publish(new SettingsChanged(Current));
    }
}
