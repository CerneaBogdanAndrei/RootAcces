using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Entering this volume re-targets the camera confiner (and optionally the zoom) to this room.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class CameraZone : MonoBehaviour
    {
        [SerializeField] private Collider2D confinerShape;
        [SerializeField] private float orthographicSize;

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<IPlayerTarget>() == null) return;
            EventBus<CameraZoneEntered>.Publish(new CameraZoneEntered(confinerShape, orthographicSize));
        }
    }
}
