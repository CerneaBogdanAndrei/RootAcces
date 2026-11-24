using RootAccess.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>Converts <see cref="ScreenShakeRequested"/> into Cinemachine impulses (listened to by the scene vcams).</summary>
    [RequireComponent(typeof(CinemachineImpulseSource))]
    public sealed class ScreenShake : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float maxForce = 3f;

        private CinemachineImpulseSource _source;

        private void Awake() => _source = GetComponent<CinemachineImpulseSource>();

        private void OnEnable() => EventBus<ScreenShakeRequested>.Subscribe(OnShakeRequested);

        private void OnDisable() => EventBus<ScreenShakeRequested>.Unsubscribe(OnShakeRequested);

        private void OnShakeRequested(ScreenShakeRequested evt)
        {
            if (evt.Force <= 0f) return;
            _source.GenerateImpulseWithForce(Mathf.Min(evt.Force, maxForce));
        }
    }
}
