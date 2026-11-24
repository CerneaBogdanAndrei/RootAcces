using RootAccess.Core;
using UnityEngine;

namespace RootAccess.AI
{
    /// <summary>Pooled straight-flying projectile (turret bolts). Damages any IDamageable it touches.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 3f;
        [SerializeField, Min(1)] private int damage = 1;

        private Rigidbody2D _body;
        private PooledObject _pooled;
        private float _despawnAt;
        private GameObject _owner;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _pooled = GetComponent<PooledObject>();
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Launch(Vector2 direction, GameObject owner)
        {
            _owner = owner;
            _body.linearVelocity = direction.normalized * speed;
            transform.right = direction;
            _despawnAt = Time.time + lifetime;
        }

        private void Update()
        {
            if (Time.time >= _despawnAt) Despawn();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_owner != null && other.transform.IsChildOf(_owner.transform)) return;
            if (other.TryGetComponent(out IDamageable damageable))
            {
                damageable.TakeDamage(new DamageInfo(damage, transform.position, 5f, DamageType.Projectile));
                Despawn();
                return;
            }

            if ((Layers.ObstacleMask & (1 << other.gameObject.layer)) == 0) return;
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.Sparks, transform.position));
            Despawn();
        }

        public void OnSpawned() => _despawnAt = Time.time + lifetime;

        public void OnDespawned()
        {
            _body.linearVelocity = Vector2.zero;
            _owner = null;
        }

        private void Despawn()
        {
            if (_pooled == null) _pooled = GetComponent<PooledObject>();
            if (_pooled != null) _pooled.Release();
            else Destroy(gameObject);
        }
    }
}
