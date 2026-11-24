using System.Collections.Generic;
using RootAccess.Core;
using UnityEngine;

namespace RootAccess.AI
{
    /// <summary>
    /// Flying security drone. Chases the player through the level with our A* grid (requests are queued and
    /// spread over frames), smooths the path with line-of-sight skipping and damages on contact (OnCollisionEnter2D).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Drone : MonoBehaviour, IHackable
    {
        private enum Mode { Idle, Chase, Return, Disabled }

        [SerializeField] private float moveSpeed = 3.6f;
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float aggroRange = 9f;
        [SerializeField] private float giveUpRange = 16f;
        [SerializeField] private float repathInterval = 0.35f;
        [SerializeField] private float waypointTolerance = 0.3f;
        [SerializeField] private float hoverAmplitude = 0.15f;
        [SerializeField, Min(1)] private int contactDamage = 1;
        [SerializeField] private float tracePerSecond = 10f;
        [SerializeField] private float disabledSeconds = 8f;
        [SerializeField] private SpriteRenderer eye;
        [SerializeField] private Color idleEyeColor = new(0.3f, 0.9f, 1f);
        [SerializeField] private Color chaseEyeColor = new(1f, 0.2f, 0.3f);

        private readonly List<Vector2> _path = new(64);
        private Rigidbody2D _body;
        private PathRequestQueue _queue;
        private Vector2 _home;
        private Mode _mode = Mode.Idle;
        private int _pathIndex;
        private float _nextRepath;
        private float _disabledUntil;
        private bool _awaitingPath;
        private int _requesterId;

        public string HackLabel => Strings.HackTargets.Drone;
        public bool IsHacked => _mode == Mode.Disabled;
        public bool IsChasing => _mode == Mode.Chase;
        public IReadOnlyList<Vector2> CurrentPath => _path;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _home = _body.position;
            _requesterId = GetInstanceID();
        }

        private void Start()
        {
            if (!ServiceLocator.TryGet(out _queue))
                Debug.LogWarning($"[Drone] '{name}' found no PathRequestQueue in the scene; it will fly straight at targets.", this);
        }

        private void OnDisable()
        {
            if (_queue != null) _queue.Cancel(_requesterId);
        }

        private void Update()
        {
            ServiceLocator.TryGet(out IPlayerTarget player);
            UpdateMode(player);

            if (eye != null) eye.color = _mode == Mode.Chase ? chaseEyeColor : idleEyeColor;
            if (_mode == Mode.Chase && player != null)
                EventBus<TraceNoise>.Publish(new TraceNoise(tracePerSecond * Time.deltaTime, TraceSource.Drone, player.Position));

            if (_mode is Mode.Idle or Mode.Disabled || Time.time < _nextRepath) return;
            _nextRepath = Time.time + repathInterval;
            Vector2 goal = _mode == Mode.Chase && player != null ? player.Position + Vector2.up * 0.5f : _home;
            RequestPath(goal);
        }

        private void FixedUpdate()
        {
            Vector2 desired = Vector2.zero;
            if (_mode is Mode.Chase or Mode.Return)
            {
                desired = SteerAlongPath();
            }
            else if (_mode == Mode.Idle)
            {
                float bob = Mathf.Sin(Time.time * 2f) * hoverAmplitude;
                desired = (_home + Vector2.up * bob - _body.position) * 2f;
            }

            _body.linearVelocity = Vector2.MoveTowards(_body.linearVelocity, Vector2.ClampMagnitude(desired, moveSpeed), acceleration * Time.fixedDeltaTime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_mode == Mode.Disabled) return;
            if (!collision.collider.TryGetComponent(out IDamageable damageable) || collision.collider.GetComponent<Drone>() != null) return;
            damageable.TakeDamage(new DamageInfo(contactDamage, _body.position, 8f, DamageType.Contact));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.Sparks, collision.GetContact(0).point));
        }

        public void ApplyHack(HackAction action)
        {
            _mode = Mode.Disabled;
            _disabledUntil = Time.time + disabledSeconds;
            _path.Clear();
            _body.gravityScale = 1f;
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.Sparks, transform.position));
        }

        private void UpdateMode(IPlayerTarget player)
        {
            if (_mode == Mode.Disabled)
            {
                if (Time.time < _disabledUntil) return;
                _body.gravityScale = 0f;
                _mode = Mode.Return;
                return;
            }

            bool targetValid = player != null && player.IsAlive && !player.IsHidden;
            float distance = targetValid ? Vector2.Distance(player.Position, _body.position) : float.MaxValue;
            switch (_mode)
            {
                case Mode.Idle when targetValid && distance <= aggroRange && HasLineOfSight(player.Position):
                    _mode = Mode.Chase;
                    _nextRepath = 0f;
                    EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Spotted, transform.position));
                    break;
                case Mode.Chase when !targetValid || distance > giveUpRange:
                    _mode = Mode.Return;
                    _nextRepath = 0f;
                    break;
                case Mode.Return when Vector2.Distance(_home, _body.position) < waypointTolerance * 2f:
                    _mode = Mode.Idle;
                    _path.Clear();
                    break;
                case Mode.Return when targetValid && distance <= aggroRange * 0.8f && HasLineOfSight(player.Position):
                    _mode = Mode.Chase;
                    _nextRepath = 0f;
                    break;
            }
        }

        private void RequestPath(Vector2 goal)
        {
            if (_queue == null)
            {
                _path.Clear();
                _path.Add(goal);
                _pathIndex = 0;
                return;
            }

            if (_awaitingPath) return;
            _awaitingPath = true;
            _queue.Request(_requesterId, _body.position, goal, _path, OnPathReady);
        }

        private void OnPathReady(bool found)
        {
            _awaitingPath = false;
            _pathIndex = 0;
            if (!found) _path.Clear();
        }

        private Vector2 SteerAlongPath()
        {
            if (_path.Count == 0) return Vector2.zero;
            Vector2 position = _body.position;
            while (_pathIndex < _path.Count - 1 && HasClearSegment(position, _path[_pathIndex + 1])) _pathIndex++;
            if (_pathIndex >= _path.Count) return Vector2.zero;

            Vector2 toWaypoint = _path[_pathIndex] - position;
            if (toWaypoint.magnitude < waypointTolerance && _pathIndex < _path.Count - 1) _pathIndex++;
            return toWaypoint.normalized * moveSpeed;
        }

        private bool HasLineOfSight(Vector2 target) => HasClearSegment(_body.position, target);

        private static bool HasClearSegment(Vector2 from, Vector2 to) =>
            Physics2D.Linecast(from, to, Layers.ObstacleMask).collider == null;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            for (int i = 1; i < _path.Count; i++) Gizmos.DrawLine(_path[i - 1], _path[i]);
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, aggroRange);
        }
    }
}
