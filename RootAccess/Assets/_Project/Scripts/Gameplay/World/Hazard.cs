using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Damaging surface or beam. Solid hazards (electrified floor) use OnCollisionEnter2D; beams (lasers) use
    /// triggers. Optional on/off cycling; hackable to switch off permanently.
    /// </summary>
    public sealed class Hazard : MonoBehaviour, IHackable
    {
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private DamageType damageType = DamageType.Laser;
        [SerializeField] private float knockback = 6f;
        [SerializeField] private Collider2D hazardCollider;
        [SerializeField] private SpriteRenderer beam;
        [SerializeField, Tooltip("Light of the beam (switched with it).")] private Behaviour glow;
        [SerializeField] private float onSeconds;
        [SerializeField] private float offSeconds;
        [SerializeField] private float phaseOffset;

        private bool _disabled;
        private bool _active = true;

        public string HackLabel => Strings.HackTargets.Laser;
        public bool IsHacked => _disabled;

        private void Awake()
        {
            if (hazardCollider == null) hazardCollider = GetComponent<Collider2D>();
        }

        private void Update()
        {
            bool cycling = onSeconds > 0f && offSeconds > 0f;
            bool active = !_disabled && (!cycling || Mathf.Repeat(Time.time + phaseOffset, onSeconds + offSeconds) < onSeconds);
            if (active == _active) return;
            _active = active;
            if (hazardCollider != null) hazardCollider.enabled = active;
            if (beam != null) beam.enabled = active;
            if (glow != null) glow.enabled = active;
        }

        public void ApplyHack(HackAction action) => _disabled = true;

        private void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision.collider);

        private void OnTriggerEnter2D(Collider2D other) => TryDamage(other);

        private void OnTriggerStay2D(Collider2D other) => TryDamage(other);

        private void TryDamage(Collider2D other)
        {
            if (!_active || !other.TryGetComponent(out IDamageable target) || !target.IsAlive) return;
            target.TakeDamage(new DamageInfo(damage, transform.position, knockback, damageType));
        }
    }
}
