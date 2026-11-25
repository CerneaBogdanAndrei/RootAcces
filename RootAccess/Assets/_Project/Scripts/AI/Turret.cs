using RootAccess.Core;
using UnityEngine;

namespace RootAccess.AI
{
    /// <summary>Stationary turret: tracks a visible player and fires pooled projectiles. Hackable (disabled).</summary>
    public sealed class Turret : MonoBehaviour, IHackable, IEmpTarget
    {
        [SerializeField] private VisionCone vision;
        [SerializeField] private Transform head;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private float fireInterval = 1.2f;
        [SerializeField] private float turnSpeed = 120f;
        [SerializeField] private float hackedDuration = 15f;

        private PoolService _pool;
        private float _nextShot;
        private float _disabledUntil;

        public string HackLabel => Strings.HackTargets.Turret;
        public bool IsHacked => Time.time < _disabledUntil;
        public bool IsEmpDisabled => IsHacked;

        public void ApplyEmp(float seconds) => _disabledUntil = Mathf.Max(_disabledUntil, Time.time + seconds);

        private void Start() => ServiceLocator.TryGet(out _pool);

        private void Update()
        {
            if (vision != null) vision.SetActive(!IsHacked);
            if (IsHacked || vision == null || head == null) return;

            ServiceLocator.TryGet(out IPlayerTarget player);
            if (!vision.CanSee(player, out _)) return;

            Vector2 toPlayer = player.Position - (Vector2)head.position;
            float angle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            head.rotation = Quaternion.RotateTowards(head.rotation, Quaternion.Euler(0f, 0f, angle), turnSpeed * Time.deltaTime);
            vision.SetAlert(1f);
            if (Time.time < _nextShot) return;
            _nextShot = Time.time + fireInterval;
            Fire(toPlayer);
        }

        public void ApplyHack(HackAction action) => _disabledUntil = Time.time + hackedDuration;

        private void Fire(Vector2 direction)
        {
            if (projectilePrefab == null || _pool == null) return;
            Transform origin = muzzle != null ? muzzle : head;
            Projectile projectile = _pool.Spawn(projectilePrefab, origin.position, Quaternion.identity);
            if (projectile == null) return;
            projectile.Launch(direction, gameObject);
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.TurretShot, origin.position));
        }
    }
}
