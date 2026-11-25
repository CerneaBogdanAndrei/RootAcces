using RootAccess.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace RootAccess.Audio
{
    /// <summary>
    /// Plays one-shots from a fixed pool of AudioSources (no runtime allocation), routes them to the mixer
    /// groups and keeps exposed mixer volumes in sync with the player's settings.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const float MinLinearVolume = 0.0001f;

        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup uiGroup;
        [SerializeField] private AudioMixerGroup ambienceGroup;
        [SerializeField] private SfxLibrary library;
        [SerializeField, Range(4, 64)] private int poolSize = 20;
        [SerializeField] private float spatialMaxDistance = 25f;

        private AudioSource[] _sources;
        private int _nextSource;

        public AudioMixer Mixer => mixer;

        private void Awake()
        {
            _sources = new AudioSource[poolSize];
            for (int i = 0; i < poolSize; i++)
            {
                var child = new GameObject($"Sfx_{i:00}");
                child.transform.SetParent(transform, false);
                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.maxDistance = spatialMaxDistance;
                source.minDistance = 2f;
                source.dopplerLevel = 0f;
                _sources[i] = source;
            }

            ServiceLocator.Register(this);
        }

        private void OnEnable()
        {
            EventBus<SfxRequested>.Subscribe(OnSfxRequested);
            EventBus<FootstepRequested>.Subscribe(OnFootstepRequested);
            EventBus<SettingsChanged>.Subscribe(OnSettingsChanged);
        }

        private void OnDisable()
        {
            EventBus<SfxRequested>.Unsubscribe(OnSfxRequested);
            EventBus<FootstepRequested>.Unsubscribe(OnFootstepRequested);
            EventBus<SettingsChanged>.Unsubscribe(OnSettingsChanged);
        }

        private void OnDestroy() => ServiceLocator.Unregister(this);

        public void Play(SfxId id, Vector3 position, bool spatial, float volumeScale = 1f)
        {
            if (id == SfxId.None || library == null || !library.TryGet(id, out SfxEntry entry)) return;
            AudioClip clip = entry.Clips[Random.Range(0, entry.Clips.Length)];
            float pitch = 1f + Random.Range(-entry.PitchVariance, entry.PitchVariance);
            PlayClip(clip, position, spatial, entry.Volume * volumeScale, pitch, GroupFor(entry.Channel));
        }

        public void PlayClip(AudioClip clip, Vector3 position, bool spatial, float volume, float pitch, AudioMixerGroup group)
        {
            if (clip == null) return;
            AudioSource source = NextSource();
            source.transform.position = position;
            source.spatialBlend = spatial ? 1f : 0f;
            source.outputAudioMixerGroup = group;
            source.pitch = pitch;
            source.volume = volume;
            source.clip = clip;
            source.Play();
        }

        public static float LinearToDecibels(float linear) => Mathf.Log10(Mathf.Max(MinLinearVolume, linear)) * 20f;

        private AudioSource NextSource()
        {
            for (int i = 0; i < _sources.Length; i++)
            {
                int index = (_nextSource + i) % _sources.Length;
                if (_sources[index].isPlaying) continue;
                _nextSource = (index + 1) % _sources.Length;
                return _sources[index];
            }

            AudioSource oldest = _sources[_nextSource];
            _nextSource = (_nextSource + 1) % _sources.Length;
            return oldest;
        }

        private AudioMixerGroup GroupFor(AudioChannel channel) => channel switch
        {
            AudioChannel.UI => uiGroup,
            AudioChannel.Ambience => ambienceGroup,
            _ => sfxGroup
        };

        private void OnSfxRequested(SfxRequested evt) => Play(evt.Id, evt.Position, evt.Spatial, evt.Volume);

        private void OnFootstepRequested(FootstepRequested evt)
        {
            FootstepEntry entry = library != null ? library.GetFootsteps(evt.Surface) : null;
            if (entry == null || entry.Clips.Length == 0) return;
            AudioClip clip = entry.Clips[Random.Range(0, entry.Clips.Length)];
            PlayClip(clip, evt.Position, true, entry.Volume * evt.Volume, Random.Range(0.92f, 1.08f), sfxGroup);
        }

        private void OnSettingsChanged(SettingsChanged evt)
        {
            if (mixer == null || evt.Settings == null) return;
            SetMixerVolume(MixerParameters.Master, evt.Settings.masterVolume);
            SetMixerVolume(MixerParameters.Music, evt.Settings.musicVolume);
            SetMixerVolume(MixerParameters.Sfx, evt.Settings.sfxVolume);
            SetMixerVolume(MixerParameters.UI, evt.Settings.uiVolume);
        }

        private void SetMixerVolume(string parameter, float linear)
        {
            if (!mixer.SetFloat(parameter, LinearToDecibels(linear)))
                Debug.LogWarning($"[AudioManager] Mixer parameter '{parameter}' is not exposed.");
        }
    }
}
