using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>Marks a collider's surface material so footsteps and landings pick the right sound.</summary>
    [DisallowMultipleComponent]
    public sealed class SurfaceTag : MonoBehaviour
    {
        [SerializeField] private SurfaceType surface = SurfaceType.Concrete;

        public SurfaceType Surface => surface;

        public void SetSurface(SurfaceType value) => surface = value;

        public static SurfaceType Resolve(Collider2D collider, SurfaceType fallback = SurfaceType.Concrete)
        {
            if (collider == null) return fallback;
            if (collider.TryGetComponent(out SurfaceTag tag)) return tag.surface;
            SurfaceTag parentTag = collider.GetComponentInParent<SurfaceTag>();
            return parentTag != null ? parentTag.surface : fallback;
        }
    }
}
