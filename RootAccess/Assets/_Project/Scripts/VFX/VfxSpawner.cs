using RootAccess.Core;
using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>Turns <see cref="VfxRequested"/> events into pooled particle effects.</summary>
    public sealed class VfxSpawner : MonoBehaviour
    {
        [SerializeField] private VfxLibrary library;
        [SerializeField] private PoolService pool;
        [SerializeField, Min(0.1f)] private float fallbackLifetime = 3f;

        private void Start()
        {
            if (library == null || pool == null)
            {
                Debug.LogError("[VfxSpawner] Library or PoolService reference missing.");
                enabled = false;
                return;
            }

            foreach (VfxEntry entry in library.Entries)
                if (entry?.Prefab != null) pool.Prewarm(entry.Prefab, entry.Prewarm);
        }

        private void OnEnable() => EventBus<VfxRequested>.Subscribe(OnVfxRequested);

        private void OnDisable() => EventBus<VfxRequested>.Unsubscribe(OnVfxRequested);

        public GameObject Spawn(VfxId id, Vector3 position, Quaternion rotation)
        {
            if (!enabled || !library.TryGet(id, out VfxEntry entry)) return null;
            GameObject instance = pool.Spawn(entry.Prefab, position, rotation);
            if (instance == null) return null;

            if (instance.TryGetComponent(out ParticleSystem particles))
            {
                particles.Clear(true);
                particles.Play(true);
                ParticleSystem.MainModule main = particles.main;
                if (main.loop && instance.TryGetComponent(out PooledObject loopingPooled)) loopingPooled.ReleaseAfter(fallbackLifetime);
            }
            else if (instance.TryGetComponent(out PooledObject pooled))
            {
                pooled.ReleaseAfter(fallbackLifetime);
            }

            return instance;
        }

        private void OnVfxRequested(VfxRequested evt) => Spawn(evt.Id, evt.Position, evt.Rotation);
    }
}
