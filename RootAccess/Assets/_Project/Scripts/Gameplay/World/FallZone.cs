using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Trigger at the bottom of a pit (the gap between two rooftops, an elevator shaft). The player loses one point of
    /// health and reappears on <see cref="returnPoint"/>, the ledge before the pit; anything else that falls in is
    /// left alone (guards stop at ledges by design).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class FallZone : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField, Tooltip("Where the player reappears; falls back to this object's position + 3 units up.")]
        private Transform returnPoint;

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out IFallRecoverable recoverable)) return;
            Vector3 safe = returnPoint != null ? returnPoint.position : transform.position + Vector3.up * 3f;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Hurt, other.transform.position));
            recoverable.RecoverFromFall(damage, safe);
        }
    }
}
