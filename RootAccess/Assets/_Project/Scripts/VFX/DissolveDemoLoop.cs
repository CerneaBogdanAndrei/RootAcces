using UnityEngine;

namespace RootAccess.VFX
{
    /// <summary>Showcase helper: dissolves a renderer out and back in forever.</summary>
    [RequireComponent(typeof(DissolveController))]
    public sealed class DissolveDemoLoop : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float holdSeconds = 1f;

        private DissolveController _dissolve;
        private float _next;
        private bool _out;

        private void Awake() => _dissolve = GetComponent<DissolveController>();

        private void Update()
        {
            if (_dissolve.IsRunning || Time.time < _next) return;
            _next = Time.time + holdSeconds;
            _out = !_out;
            if (_out) _dissolve.DissolveOut(null, 0.9f);
            else _dissolve.DissolveIn(null, 0.9f);
        }
    }
}
