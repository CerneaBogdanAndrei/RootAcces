using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Pressure plate / motion sensor volume: entering it while visible spikes the trace meter.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class AlarmZone : MonoBehaviour, IHackable
    {
        [SerializeField] private float traceAmount = 35f;
        [SerializeField] private float cooldownSeconds = 3f;
        [SerializeField] private SpriteRenderer indicator;

        private float _readyAt;
        private bool _disabled;

        public string HackLabel => Strings.HackTargets.MotionSensor;
        public bool IsHacked => _disabled;

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        public void ApplyHack(HackAction action)
        {
            _disabled = true;
            if (indicator != null) indicator.enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_disabled || Time.time < _readyAt) return;
            IPlayerTarget player = other.GetComponentInParent<IPlayerTarget>();
            if (player == null || player.IsHidden) return;
            _readyAt = Time.time + cooldownSeconds;
            EventBus<TraceNoise>.Publish(new TraceNoise(traceAmount, TraceSource.Alarm, player.Position));
            EventBus<AlarmRaised>.Publish(new AlarmRaised(player.Position));
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Alarm, transform.position));
        }
    }
}
