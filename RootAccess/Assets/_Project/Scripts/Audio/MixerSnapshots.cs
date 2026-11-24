using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;
using UnityEngine.Audio;

namespace RootAccess.Audio
{
    /// <summary>Blends AudioMixer snapshots: Normal, Underwater (swimming) and Alarm (lockdown / alert).</summary>
    public sealed class MixerSnapshots : MonoBehaviour
    {
        [SerializeField] private AudioMixerSnapshot normal;
        [SerializeField] private AudioMixerSnapshot underwater;
        [SerializeField] private AudioMixerSnapshot alarm;
        [SerializeField, Min(0f)] private float transitionSeconds = 0.4f;

        private bool _underwater;
        private bool _alarm;

        private void OnEnable()
        {
            EventBus<UnderwaterChanged>.Subscribe(OnUnderwaterChanged);
            EventBus<TraceLevelChanged>.Subscribe(OnTraceLevelChanged);
            EventBus<SceneLoadStarted>.Subscribe(OnSceneLoadStarted);
        }

        private void OnDisable()
        {
            EventBus<UnderwaterChanged>.Unsubscribe(OnUnderwaterChanged);
            EventBus<TraceLevelChanged>.Unsubscribe(OnTraceLevelChanged);
            EventBus<SceneLoadStarted>.Unsubscribe(OnSceneLoadStarted);
        }

        private void OnUnderwaterChanged(UnderwaterChanged evt)
        {
            _underwater = evt.IsUnderwater;
            Apply();
        }

        private void OnTraceLevelChanged(TraceLevelChanged evt)
        {
            _alarm = evt.Current == TraceLevel.Lockdown;
            Apply();
        }

        private void OnSceneLoadStarted(SceneLoadStarted _)
        {
            _underwater = false;
            _alarm = false;
            Apply();
        }

        private void Apply()
        {
            AudioMixerSnapshot target = _underwater ? underwater : _alarm ? alarm : normal;
            if (target != null) target.TransitionTo(transitionSeconds);
        }
    }
}
