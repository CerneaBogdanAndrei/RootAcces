using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Shadow volume: anything <see cref="IHideable"/> inside can become hidden from enemies.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HideZone : MonoBehaviour
    {
        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            IHideable hideable = other.GetComponentInParent<IHideable>();
            hideable?.SetInHideZone(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            IHideable hideable = other.GetComponentInParent<IHideable>();
            hideable?.SetInHideZone(false);
        }
    }
}
