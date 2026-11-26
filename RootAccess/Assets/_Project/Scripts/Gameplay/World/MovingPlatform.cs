using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Kinematic platform shuttling between two points forever (the window-cleaning gondolas of Level 6), pausing at
    /// each end. It reports its velocity as an <see cref="ISurfaceCarrier"/>, so a player standing on it rides along.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MovingPlatform : MonoBehaviour, ISurfaceCarrier
    {
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField, Min(0.1f)] private float speed = 2.4f;
        [SerializeField, Min(0f)] private float waitSeconds = 1f;
        [SerializeField, Min(0f), Tooltip("Seconds before the first trip (staggers platforms that share a pit).")]
        private float startDelay;

        private Rigidbody2D _body;
        private bool _towardsB = true;
        private float _waitUntil;

        public Vector2 CarryVelocity { get; private set; }
        public bool HeadingToB => _towardsB;
        public Vector2 PointA => pointA != null ? (Vector2)pointA.position : (Vector2)transform.position;
        public Vector2 PointB => pointB != null ? (Vector2)pointB.position : (Vector2)transform.position;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _waitUntil = Time.time + startDelay;
            if (pointA == null || pointB == null) Debug.LogError($"[MovingPlatform] '{name}' needs both end points.", this);
        }

        private void FixedUpdate()
        {
            CarryVelocity = Vector2.zero;
            if (pointA == null || pointB == null || Time.time < _waitUntil) return;

            float dt = Time.fixedDeltaTime;
            Vector2 target = _towardsB ? pointB.position : pointA.position;
            Vector2 position = _body.position;
            Vector2 next = Vector2.MoveTowards(position, target, speed * dt);
            CarryVelocity = (next - position) / dt;
            _body.MovePosition(next);
            if ((next - target).sqrMagnitude > 0.0001f) return;

            _towardsB = !_towardsB;
            _waitUntil = Time.time + waitSeconds;
        }
    }
}
