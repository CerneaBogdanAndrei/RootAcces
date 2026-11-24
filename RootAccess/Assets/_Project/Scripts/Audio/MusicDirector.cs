using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;
using UnityEngine.Audio;

namespace RootAccess.Audio
{
    /// <summary>
    /// Adaptive music: crossfades between a stealth layer and an alert layer as the trace level changes,
    /// plays the menu theme outside gameplay and keeps a per-scene ambience loop running.
    /// </summary>
    public sealed class MusicDirector : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;
        [SerializeField] private AudioClip menuTheme;
        [SerializeField] private AudioClip stealthLayer;
        [SerializeField] private AudioClip alertLayer;
        [SerializeField] private AudioClip ambienceLoop;
        [SerializeField, Min(0.05f)] private float crossfadeSeconds = 1.5f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.6f;

        private AudioSource _menu;
        private AudioSource _stealth;
        private AudioSource _alert;
        private AudioSource _ambience;
        private MusicMood _mood = MusicMood.Silence;

        public MusicMood Mood => _mood;

        private void Awake()
        {
            _menu = CreateLoop("Music_Menu", menuTheme, musicGroup);
            _stealth = CreateLoop("Music_Stealth", stealthLayer, musicGroup);
            _alert = CreateLoop("Music_Alert", alertLayer, musicGroup);
            _ambience = CreateLoop("Ambience", ambienceLoop, ambienceGroup);
        }

        private void OnEnable()
        {
            EventBus<GameStateChanged>.Subscribe(OnGameStateChanged);
            EventBus<TraceLevelChanged>.Subscribe(OnTraceLevelChanged);
            EventBus<MusicMoodRequested>.Subscribe(OnMoodRequested);
        }

        private void OnDisable()
        {
            EventBus<GameStateChanged>.Unsubscribe(OnGameStateChanged);
            EventBus<TraceLevelChanged>.Unsubscribe(OnTraceLevelChanged);
            EventBus<MusicMoodRequested>.Unsubscribe(OnMoodRequested);
        }

        private void Update()
        {
            float step = Time.unscaledDeltaTime / crossfadeSeconds;
            Fade(_menu, _mood == MusicMood.Menu ? musicVolume : 0f, step);
            Fade(_stealth, _mood == MusicMood.Stealth ? musicVolume : 0f, step);
            Fade(_alert, _mood == MusicMood.Alert ? musicVolume : 0f, step);
            bool ambienceOn = _mood == MusicMood.Stealth || _mood == MusicMood.Alert;
            Fade(_ambience, ambienceOn ? ambienceVolume : 0f, step);
        }

        public void SetMood(MusicMood mood) => _mood = mood;

        private AudioSource CreateLoop(string objectName, AudioClip clip, AudioMixerGroup group)
        {
            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.outputAudioMixerGroup = group;
            if (clip != null) source.Play();
            return source;
        }

        private static void Fade(AudioSource source, float target, float step)
        {
            if (source.clip == null) return;
            source.volume = Mathf.MoveTowards(source.volume, target, step);
        }

        private void OnGameStateChanged(GameStateChanged evt)
        {
            switch (evt.Current)
            {
                case GameState.MainMenu:
                    _mood = MusicMood.Menu;
                    break;
                case GameState.Playing when _mood is MusicMood.Menu or MusicMood.Silence:
                    _mood = MusicMood.Stealth;
                    break;
                case GameState.Loading:
                    _mood = MusicMood.Silence;
                    break;
            }
        }

        private void OnTraceLevelChanged(TraceLevelChanged evt)
        {
            if (_mood is MusicMood.Menu or MusicMood.Silence) return;
            _mood = evt.Current >= TraceLevel.Alert ? MusicMood.Alert : MusicMood.Stealth;
        }

        private void OnMoodRequested(MusicMoodRequested evt) => _mood = evt.Mood;
    }
}
