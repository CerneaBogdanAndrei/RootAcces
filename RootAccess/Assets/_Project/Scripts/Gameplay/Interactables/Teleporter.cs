using System.Collections;
using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Shared teleport sequence: dissolve the player out, send a pooled particle trail to the destination,
    /// dissolve back in. Concrete teleporters decide when it is usable and where it leads.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class Teleporter : MonoBehaviour, IInteractable
    {
        [SerializeField] private Transform arrivalPoint;
        [SerializeField] private GameObject trailPrefab;
        [SerializeField] private float dissolveSeconds = 0.45f;
        [SerializeField] private float travelSeconds = 0.6f;

        private bool _busy;

        public abstract string Prompt { get; }
        public Vector3 InteractionPosition => transform.position;
        public Vector3 ArrivalPosition => arrivalPoint != null ? arrivalPoint.position : transform.position + Vector3.up * 0.5f;

        protected abstract Teleporter Destination { get; }

        protected virtual void Awake() => GetComponent<Collider2D>().isTrigger = true;

        public virtual bool CanInteract(IInteractor interactor) =>
            !_busy && Destination != null && interactor?.GameObject != null && interactor.GameObject.TryGetComponent(out ITeleportable _);

        public void Interact(IInteractor interactor)
        {
            if (!CanInteract(interactor) || !interactor.GameObject.TryGetComponent(out ITeleportable teleportable)) return;
            if (!teleportable.CanTeleport) return;
            StartCoroutine(TeleportRoutine(teleportable, Destination));
        }

        private IEnumerator TeleportRoutine(ITeleportable traveller, Teleporter destination)
        {
            _busy = true;
            Vector3 from = traveller.Transform.position;
            Vector3 to = destination.ArrivalPosition;
            EventBus<TeleportStarted>.Publish(new TeleportStarted(from, to));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.TeleportOut, from));
            traveller.BeginTeleport();
            yield return new WaitForSeconds(dissolveSeconds);

            GameObject trail = null;
            if (trailPrefab != null && ServiceLocator.TryGet(out PoolService pool)) trail = pool.Spawn(trailPrefab, from, Quaternion.identity);
            float elapsed = 0f;
            while (elapsed < travelSeconds)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / travelSeconds);
                Vector3 arc = Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.5f;
                if (trail != null) trail.transform.position = Vector3.Lerp(from, to, t) + arc;
                yield return null;
            }

            if (trail != null && trail.TryGetComponent(out PooledObject pooledTrail)) pooledTrail.ReleaseAfter(0.5f);
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.TeleportIn, to));
            traveller.CompleteTeleport(to);
            OnTeleported();
            _busy = false;
        }

        protected virtual void OnTeleported() { }
    }
}
