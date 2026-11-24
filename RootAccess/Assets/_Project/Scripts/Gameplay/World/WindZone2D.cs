using RootAccess.VFX;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Area that pushes rigidbodies (rooftop gusts, vents) and boosts foliage sway while active.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class WindZone2D : MonoBehaviour
    {
        [SerializeField] private Vector2 direction = Vector2.right;
        [SerializeField] private float force = 12f;
        [SerializeField] private float pulseSeconds = 4f;
        [SerializeField, Range(0f, 1f)] private float pulseDuty = 0.5f;
        [SerializeField] private FoliageWind foliage;

        private bool _blowing;

        public bool IsBlowing => _blowing;

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void Update()
        {
            _blowing = pulseSeconds <= 0f || Mathf.Repeat(Time.time, pulseSeconds) < pulseSeconds * pulseDuty;
            if (_blowing && foliage != null) foliage.AddGust(0.6f);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!_blowing) return;
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic) return;
            body.AddForce(direction.normalized * force, ForceMode2D.Force);
        }
    }
}
