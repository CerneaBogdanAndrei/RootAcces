using System;
using System.Collections.Generic;
using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Audio
{
    [Serializable]
    public sealed class SfxEntry
    {
        [SerializeField] private SfxId id;
        [SerializeField] private AudioClip[] clips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0f, 0.5f)] private float pitchVariance = 0.05f;
        [SerializeField] private AudioChannel channel = AudioChannel.Sfx;

        public SfxEntry(SfxId sfxId, AudioClip[] sfxClips, float sfxVolume = 1f, float variance = 0.05f, AudioChannel sfxChannel = AudioChannel.Sfx)
        {
            id = sfxId;
            clips = sfxClips ?? new AudioClip[0];
            volume = sfxVolume;
            pitchVariance = variance;
            channel = sfxChannel;
        }

        public SfxId Id => id;
        public AudioClip[] Clips => clips;
        public float Volume => volume;
        public float PitchVariance => pitchVariance;
        public AudioChannel Channel => channel;
    }

    [Serializable]
    public sealed class FootstepEntry
    {
        [SerializeField] private SurfaceType surface;
        [SerializeField] private AudioClip[] clips = new AudioClip[0];
        [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

        public FootstepEntry(SurfaceType surfaceType, AudioClip[] stepClips, float stepVolume = 0.6f)
        {
            surface = surfaceType;
            clips = stepClips ?? new AudioClip[0];
            volume = stepVolume;
        }

        public SurfaceType Surface => surface;
        public AudioClip[] Clips => clips;
        public float Volume => volume;
    }

    /// <summary>Maps <see cref="SfxId"/> and surfaces to clips (random variation per play).</summary>
    [CreateAssetMenu(menuName = "Root Access/Audio/Sfx Library", fileName = "SfxLibrary")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [SerializeField] private SfxEntry[] entries = new SfxEntry[0];
        [SerializeField] private FootstepEntry[] footsteps = new FootstepEntry[0];

        private Dictionary<SfxId, SfxEntry> _lookup;

        public IReadOnlyList<SfxEntry> Entries => entries;
        public IReadOnlyList<FootstepEntry> Footsteps => footsteps;

        public bool TryGet(SfxId id, out SfxEntry entry)
        {
            if (_lookup == null || _lookup.Count != entries.Length) BuildLookup();
            return _lookup.TryGetValue(id, out entry) && entry.Clips.Length > 0;
        }

        public FootstepEntry GetFootsteps(SurfaceType surface)
        {
            FootstepEntry fallback = null;
            foreach (FootstepEntry entry in footsteps)
            {
                if (entry.Surface == surface && entry.Clips.Length > 0) return entry;
                if (entry.Surface == SurfaceType.Concrete) fallback = entry;
            }

            return fallback;
        }

        public void SetEntries(SfxEntry[] sfx, FootstepEntry[] steps)
        {
            entries = sfx ?? new SfxEntry[0];
            footsteps = steps ?? new FootstepEntry[0];
            _lookup = null;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<SfxId, SfxEntry>();
            foreach (SfxEntry entry in entries)
                if (entry != null) _lookup[entry.Id] = entry;
        }
    }
}
