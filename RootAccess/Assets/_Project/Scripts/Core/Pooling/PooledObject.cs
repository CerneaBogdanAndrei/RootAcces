using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>Added automatically to pooled instances; knows how to return itself to its pool.</summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        private PoolService _owner;
        private int _prefabId;
        private float _releaseAt = -1f;
        private IPoolable[] _poolables;

        public int PrefabId => _prefabId;
        public bool IsSpawned { get; private set; }

        internal void Bind(PoolService owner, int prefabId)
        {
            _owner = owner;
            _prefabId = prefabId;
            _poolables = GetComponentsInChildren<IPoolable>(true);
        }

        internal void NotifySpawned()
        {
            IsSpawned = true;
            _releaseAt = -1f;
            for (int i = 0; i < _poolables.Length; i++) _poolables[i].OnSpawned();
        }

        internal void NotifyDespawned()
        {
            IsSpawned = false;
            _releaseAt = -1f;
            for (int i = 0; i < _poolables.Length; i++) _poolables[i].OnDespawned();
        }

        public void Release()
        {
            if (!IsSpawned) return;
            if (_owner != null) _owner.Despawn(this);
            else Destroy(gameObject);
        }

        public void ReleaseAfter(float seconds) => _releaseAt = Time.time + Mathf.Max(0f, seconds);

        private void Update()
        {
            if (_releaseAt >= 0f && Time.time >= _releaseAt) Release();
        }

        private void OnParticleSystemStopped() => Release();
    }
}
