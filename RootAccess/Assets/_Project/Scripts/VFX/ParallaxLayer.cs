using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>
    /// Moves a background/foreground layer relative to the camera. factor 0 = fixed to world, 1 = glued to camera,
    /// negative = foreground moving faster than the world.
    /// </summary>
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Vector2 factor = new(0.5f, 0.2f);
        [SerializeField] private Transform cameraTransform;

        private Vector3 _startPosition;
        private Vector3 _cameraStart;

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            _startPosition = transform.position;
            if (cameraTransform != null) _cameraStart = cameraTransform.position;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;
            Vector3 delta = cameraTransform.position - _cameraStart;
            transform.position = new Vector3(
                _startPosition.x + delta.x * factor.x,
                _startPosition.y + delta.y * factor.y,
                _startPosition.z);
        }
    }
}
