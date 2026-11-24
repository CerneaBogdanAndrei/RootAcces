using RootAccess.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>
    /// Sits on the level's main CinemachineCamera: follows the registered player, zooms in while hacking and
    /// swaps the confiner shape when a <see cref="CameraZoneEntered"/> event arrives.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private float defaultOrthoSize = 6f;
        [SerializeField] private float hackingOrthoSize = 4.2f;
        [SerializeField, Min(0.01f)] private float zoomSpeed = 4f;
        [SerializeField] private CinemachineConfiner2D confiner;

        private CinemachineCamera _camera;
        private float _targetSize;
        private float _baseSize;

        private void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
            _baseSize = defaultOrthoSize;
            _targetSize = defaultOrthoSize;
            if (confiner == null) confiner = GetComponent<CinemachineConfiner2D>();
        }

        private void OnEnable()
        {
            EventBus<HackSessionStarted>.Subscribe(OnHackStarted);
            EventBus<HackSessionEnded>.Subscribe(OnHackEnded);
            EventBus<CameraZoneEntered>.Subscribe(OnZoneEntered);
        }

        private void OnDisable()
        {
            EventBus<HackSessionStarted>.Unsubscribe(OnHackStarted);
            EventBus<HackSessionEnded>.Unsubscribe(OnHackEnded);
            EventBus<CameraZoneEntered>.Unsubscribe(OnZoneEntered);
        }

        private void Start() => AcquireTarget();

        private void LateUpdate()
        {
            if (_camera.Target.TrackingTarget == null) AcquireTarget();
            LensSettings lens = _camera.Lens;
            if (Mathf.Approximately(lens.OrthographicSize, _targetSize)) return;
            lens.OrthographicSize = Mathf.MoveTowards(lens.OrthographicSize, _targetSize, zoomSpeed * Time.unscaledDeltaTime);
            _camera.Lens = lens;
            if (confiner != null) confiner.InvalidateLensCache();
        }

        private void AcquireTarget()
        {
            if (!ServiceLocator.TryGet(out IPlayerTarget player)) return;
            _camera.Target.TrackingTarget = player.Transform;
            _camera.ForceCameraPosition(player.Transform.position + Vector3.back * 10f, Quaternion.identity);
        }

        private void OnHackStarted(HackSessionStarted _) => _targetSize = hackingOrthoSize;

        private void OnHackEnded(HackSessionEnded _) => _targetSize = _baseSize;

        private void OnZoneEntered(CameraZoneEntered evt)
        {
            if (evt.OrthographicSize > 0f)
            {
                _baseSize = evt.OrthographicSize;
                _targetSize = _baseSize;
            }

            if (confiner == null || evt.Bounds == null || confiner.BoundingShape2D == evt.Bounds) return;
            confiner.BoundingShape2D = evt.Bounds;
            confiner.InvalidateBoundingShapeCache();
        }
    }
}
