using System.Collections.Generic;
using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Thrown EMP charge (pooled). Detonates after a short fuse or on contact with an enemy and knocks out every
    /// <see cref="IEmpTarget"/> in range: cameras, drones, turrets, guard visors. The "EMP capacitors" skill widens the blast.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EmpGrenade : MonoBehaviour, IPoolable
    {
        private const float CapacitorRadiusMultiplier = 1.5f;

        [SerializeField, Min(0.1f)] private float fuseSeconds = 1.1f;
        [SerializeField, Min(0.5f)] private float radius = 3.2f;
        [SerializeField, Min(0.5f)] private float disableSeconds = 6f;
        [SerializeField, Min(0f)] private float traceNoise = 6f;

        private readonly List<Collider2D> _hits = new(32);
        private readonly HashSet<IEmpTarget> _affected = new();
        private float _detonateAt = -1f;

        public float Radius => radius;

        private void OnEnable() => _detonateAt = Time.time + fuseSeconds;

        public void OnSpawned() => _detonateAt = Time.time + fuseSeconds;

        public void OnDespawned() => _detonateAt = -1f;

        private void Update()
        {
            if (_detonateAt > 0f && Time.time >= _detonateAt) Detonate();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponentInParent<IEmpTarget>() != null) Detonate();
        }

        /// <summary>Applies the blast immediately; returns how many targets were disabled.</summary>
        public int Detonate()
        {
            if (_detonateAt < 0f) return 0;
            _detonateAt = -1f;
            Vector2 center = transform.position;
            float blastRadius = radius * (HasCapacitors() ? CapacitorRadiusMultiplier : 1f);
            ContactFilter2D filter = ContactFilter2D.noFilter;
            Physics2D.OverlapCircle(center, blastRadius, filter, _hits);

            _affected.Clear();
            foreach (Collider2D hit in _hits)
            {
                IEmpTarget target = hit.GetComponentInParent<IEmpTarget>();
                if (target == null || !_affected.Add(target)) continue;
                target.ApplyEmp(disableSeconds);
            }

            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.EmpExplosion, center));
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.EmpBlast, center));
            EventBus<ScreenShakeRequested>.Publish(new ScreenShakeRequested(0.7f));
            EventBus<TraceNoise>.Publish(new TraceNoise(traceNoise, TraceSource.Noise, center));
            if (_affected.Count > 0) EventBus<ToastRequested>.Publish(new ToastRequested(string.Format(Strings.Hud.EmpDetonated, _affected.Count)));

            int affected = _affected.Count;
            if (TryGetComponent(out PooledObject pooled)) pooled.Release();
            else gameObject.SetActive(false);
            return affected;
        }

        private static bool HasCapacitors() =>
            ServiceLocator.TryGet(out SessionService session) && session.HasSkillEffect(SkillEffect.EmpCapacity);
    }
}
