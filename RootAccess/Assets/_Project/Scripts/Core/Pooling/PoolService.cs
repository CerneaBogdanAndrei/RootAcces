using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace RootAccess.Core
{
    /// <summary>
    /// Prefab-keyed object pools (UnityEngine.Pool under the hood). Spawned objects live under this persistent
    /// transform and are all reclaimed when a scene load starts, so no pooled instance dangles across scenes.
    /// </summary>
    public sealed class PoolService : MonoBehaviour
    {
        [SerializeField] private int defaultCapacity = 8;
        [SerializeField] private int maxSize = 128;

        private readonly Dictionary<int, ObjectPool<PooledObject>> _pools = new();
        private readonly Dictionary<int, Transform> _containers = new();
        private readonly List<PooledObject> _active = new();

        private void Awake() => ServiceLocator.Register(this);

        private void OnEnable() => EventBus<SceneLoadStarted>.Subscribe(OnSceneLoadStarted);

        private void OnDisable() => EventBus<SceneLoadStarted>.Unsubscribe(OnSceneLoadStarted);

        private void OnDestroy() => ServiceLocator.Unregister(this);

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("[PoolService] Spawn called with a null prefab.");
                return null;
            }

            PooledObject pooled = GetPool(prefab).Get();
            Transform t = pooled.transform;
            t.SetPositionAndRotation(position, rotation);
            pooled.gameObject.SetActive(true);
            pooled.NotifySpawned();
            _active.Add(pooled);
            return pooled.gameObject;
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogError($"[PoolService] Spawn<{typeof(T).Name}> called with a null prefab.");
                return null;
            }

            GameObject instance = Spawn(prefab.gameObject, position, rotation);
            return instance != null ? instance.GetComponent<T>() : null;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;
            if (instance.TryGetComponent(out PooledObject pooled)) Despawn(pooled);
            else Destroy(instance);
        }

        public void Despawn(PooledObject pooled)
        {
            if (pooled == null || !pooled.IsSpawned) return;
            if (!_pools.TryGetValue(pooled.PrefabId, out ObjectPool<PooledObject> pool))
            {
                Destroy(pooled.gameObject);
                return;
            }

            _active.Remove(pooled);
            pooled.NotifyDespawned();
            pool.Release(pooled);
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null) return;
            ObjectPool<PooledObject> pool = GetPool(prefab);
            var buffer = ListPool<PooledObject>.Get();
            for (int i = 0; i < count; i++) buffer.Add(pool.Get());
            for (int i = 0; i < buffer.Count; i++) pool.Release(buffer[i]);
            ListPool<PooledObject>.Release(buffer);
        }

        public void DespawnAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                PooledObject pooled = _active[i];
                if (pooled == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }
                Despawn(pooled);
            }
        }

        private void OnSceneLoadStarted(SceneLoadStarted _) => DespawnAll();

        private ObjectPool<PooledObject> GetPool(GameObject prefab)
        {
            int id = prefab.GetInstanceID();
            if (_pools.TryGetValue(id, out ObjectPool<PooledObject> pool)) return pool;

            var container = new GameObject($"Pool_{prefab.name}").transform;
            container.SetParent(transform, false);
            _containers[id] = container;

            pool = new ObjectPool<PooledObject>(
                createFunc: () => CreateInstance(prefab, id, container),
                actionOnGet: null,
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => { if (p != null) Destroy(p.gameObject); },
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
            _pools[id] = pool;
            return pool;
        }

        private PooledObject CreateInstance(GameObject prefab, int prefabId, Transform container)
        {
            GameObject instance = Instantiate(prefab, container);
            instance.SetActive(false);
            if (!instance.TryGetComponent(out PooledObject pooled)) pooled = instance.AddComponent<PooledObject>();
            pooled.Bind(this, prefabId);
            return pooled;
        }
    }
}
