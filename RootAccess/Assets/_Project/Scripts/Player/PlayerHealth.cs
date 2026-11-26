using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>Hit points, invulnerability frames, death and checkpoint respawn for the player.</summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerHealth : MonoBehaviour, IDamageable, IFallRecoverable
    {
        [SerializeField] private SpriteRenderer flashRenderer;

        private PlayerController _controller;
        private Vector3 _respawnPoint;
        private float _invulnerableUntil;
        private bool _ignoreDamage;

        public int Current { get; private set; }
        public int Max => _controller.Config.MaxHealth + _controller.BonusHealth;
        public bool IsAlive => Current > 0;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _ignoreDamage = CommandLineAutomation.Invulnerable;
        }

        private void Start()
        {
            if (_controller.Config == null)
            {
                enabled = false;
                return;
            }

            Current = Max;
            _respawnPoint = transform.position;
            Publish();
        }

        private void OnEnable()
        {
            EventBus<CheckpointReached>.Subscribe(OnCheckpoint);
            EventBus<PlayerRespawnRequested>.Subscribe(OnRespawnRequested);
            EventBus<SkillUnlockedEvent>.Subscribe(OnSkillUnlocked);
        }

        private void OnDisable()
        {
            EventBus<CheckpointReached>.Unsubscribe(OnCheckpoint);
            EventBus<PlayerRespawnRequested>.Unsubscribe(OnRespawnRequested);
            EventBus<SkillUnlockedEvent>.Unsubscribe(OnSkillUnlocked);
        }

        private void Update()
        {
            if (IsAlive && transform.position.y < _controller.Config.KillPlaneY)
                TakeDamage(new DamageInfo(Current, transform.position, 0f, DamageType.Fall));

            if (flashRenderer == null) return;
            bool blinkOff = IsAlive && IsInvulnerable && Mathf.Repeat(Time.time * 12f, 1f) > 0.5f;
            if (flashRenderer.enabled == blinkOff) flashRenderer.enabled = !blinkOff;
        }

        public void TakeDamage(in DamageInfo damage)
        {
            if (!IsAlive || damage.Amount <= 0) return;
            if (IsInvulnerable && damage.Type != DamageType.Fall) return;
            if (_ignoreDamage && damage.Type != DamageType.Fall)
            {
                Debug.Log($"[PlayerHealth] Hit ignored (automation): {damage.Type} x{damage.Amount} at {transform.position}.", this);
                _invulnerableUntil = Time.time + _controller.Config.InvulnerabilitySeconds;
                return;
            }

            Current = Mathf.Max(0, Current - damage.Amount);
            _invulnerableUntil = Time.time + _controller.Config.InvulnerabilitySeconds;
            Publish();
            EventBus<ScreenShakeRequested>.Publish(new ScreenShakeRequested(0.8f));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.Sparks, transform.position));

            if (Current > 0)
            {
                EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Hurt, transform.position));
                _controller.OnDamaged(damage);
                return;
            }

            Debug.Log($"[PlayerHealth] Died from {damage.Type} at {transform.position}.", this);
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Death, transform.position));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.Death, transform.position));
            _controller.OnDied();
            EventBus<PlayerDied>.Publish(new PlayerDied(transform.position));
        }

        /// <summary>Pit fall: one hit of damage (ignores invulnerability), then back on the ledge the pit belongs to.</summary>
        public void RecoverFromFall(int damage, Vector3 safePosition)
        {
            if (!IsAlive) return;
            _invulnerableUntil = 0f;
            TakeDamage(new DamageInfo(Mathf.Max(1, damage), transform.position, 0f, DamageType.Generic));
            if (!IsAlive) return;
            _controller.Respawn(safePosition);
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.TeleportIn, safePosition));
        }

        public void Heal(int amount)
        {
            if (!IsAlive || amount <= 0) return;
            Current = Mathf.Min(Max, Current + amount);
            Publish();
        }

        private void Publish() => EventBus<PlayerHealthChanged>.Publish(new PlayerHealthChanged(Current, Max));

        private void OnCheckpoint(CheckpointReached evt) => _respawnPoint = evt.Position;

        private void OnSkillUnlocked(SkillUnlockedEvent evt)
        {
            if (!IsAlive || evt.Skill == null || evt.Skill.Effect != SkillEffect.ArmorPlating) return;
            _controller.RefreshAbilities();
            Current = Mathf.Min(Max, Current + 1);
            Publish();
        }

        private void OnRespawnRequested(PlayerRespawnRequested _)
        {
            Current = Max;
            _invulnerableUntil = Time.time + _controller.Config.InvulnerabilitySeconds;
            _controller.Respawn(_respawnPoint);
            Publish();
            EventBus<PlayerRespawned>.Publish(new PlayerRespawned(_respawnPoint));
        }
    }
}
