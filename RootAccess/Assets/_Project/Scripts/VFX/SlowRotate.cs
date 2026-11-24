using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>Slow continuous rotation (main menu camera panning across the skybox).</summary>
    public sealed class SlowRotate : MonoBehaviour
    {
        [SerializeField] private Vector3 degreesPerSecond = new(0f, 2.5f, 0f);

        private void Update() => transform.Rotate(degreesPerSecond * Time.unscaledDeltaTime, Space.World);
    }
}
