using RootAccess.Core;
using UnityEngine;

namespace RootAccess.AI
{
    public enum GuardStateId
    {
        Patrol,
        Suspicious,
        Chase,
        Search,
        Return
    }

    /// <summary>
    /// Patrolling guard: Patrol → Suspicious → Chase → Search → Return, driven by an awareness meter fed by its
    /// vision cone. Melee hits resolve on the attack animation's hit frame (Animation Event).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Guard : MonoBehaviour, IEmpTarget
    {
        private const float EmpStunFraction = 0.5f;

        [SerializeField] private GuardConfig config;
        [SerializeField] private VisionCone vision;
        [SerializeField] private Transform[] patrolPoints = new Transform[0];
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer alertIcon;
        [SerializeField] private Color suspiciousColor = new(1f, 0.85f, 0.2f);
        [SerializeField] private Color alertColor = new(1f, 0.2f, 0.25f);

        private readonly StateMachine _fsm = new();
        private GuardPatrolState _patrol;
        private GuardSuspiciousState _suspicious;
        private GuardChaseState _chase;
        private GuardSearchState _search;
        private GuardReturnState _return;
        private Rigidbody2D _body;
        private float _attackReadyAt;
        private float _stunnedUntil;
        private bool _attackPending;
        private Vector2 _patrolStart;

        public GuardConfig Config => config;
        public GuardStateId StateId { get; private set; }
        public float Awareness { get; private set; }
        public bool CanSeePlayer { get; private set; }
        public Vector2 LastKnownPlayerPosition { get; private set; }
        public int Facing { get; private set; } = 1;
        public Vector2 Position => _body.position;
        public IPlayerTarget Target { get; private set; }

        /// <summary>An EMP fries the guard's visor: blind and dazed for a fraction of the blast duration.</summary>
        public bool IsEmpDisabled => Time.time < _stunnedUntil;
        internal Transform[] PatrolPoints => patrolPoints;
        internal Vector2 PatrolStart => _patrolStart;

        private void Awake()
        {
            if (config == null || vision == null)
            {
                Debug.LogError($"[Guard] '{name}' needs a GuardConfig and a VisionCone.", this);
                enabled = false;
                return;
            }

            _body = GetComponent<Rigidbody2D>();
            _body.freezeRotation = true;
            _patrolStart = _body.position;
            vision.Configure(config.ViewDistance, config.ViewAngle);

            _patrol = new GuardPatrolState(this);
            _suspicious = new GuardSuspiciousState(this);
            _chase = new GuardChaseState(this);
            _search = new GuardSearchState(this);
            _return = new GuardReturnState(this);
            _fsm.StateChanged += (_, current) => StateId = ((GuardStateBase)current).Id;
        }

        private void Start() => _fsm.ChangeState(_patrol);

        public void ApplyEmp(float seconds)
        {
            _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + seconds * EmpStunFraction);
            CanSeePlayer = false;
            vision.SetActive(false);
            Stop();
        }

        private void Update()
        {
            ServiceLocator.TryGet(out IPlayerTarget player);
            Target = player;

            if (IsEmpDisabled)
            {
                Stop();
                UpdatePresentation();
                return;
            }

            if (_stunnedUntil > 0f)
            {
                _stunnedUntil = 0f;
                vision.SetActive(true);
                Awareness = Mathf.Max(Awareness, 0.5f);
                ChangeState(GuardStateId.Search);
            }

            float dt = Time.deltaTime;
            CanSeePlayer = vision.CanSee(Target, out float normalizedDistance);
            if (CanSeePlayer)
            {
                LastKnownPlayerPosition = Target.Position;
                float proximityBoost = Mathf.Lerp(2.5f, 0.6f, normalizedDistance);
                Awareness = Mathf.Min(1f, Awareness + config.AwarenessGainPerSecond * proximityBoost * dt);
                if (StateId == GuardStateId.Chase)
                    EventBus<TraceNoise>.Publish(new TraceNoise(config.TracePerSecondWhileSeen * dt, TraceSource.Guard, Target.Position));
            }
            else if (StateId != GuardStateId.Chase)
            {
                Awareness = Mathf.Max(0f, Awareness - config.AwarenessDecayPerSecond * dt);
            }

            _fsm.Tick(dt);
            UpdatePresentation();
        }

        private void FixedUpdate()
        {
            if (!IsEmpDisabled) _fsm.FixedTick(Time.fixedDeltaTime);
        }

        internal void ChangeState(GuardStateId id)
        {
            IState next = id switch
            {
                GuardStateId.Patrol => _patrol,
                GuardStateId.Suspicious => _suspicious,
                GuardStateId.Chase => _chase,
                GuardStateId.Search => _search,
                _ => _return
            };
            _fsm.ChangeState(next);
        }

        internal float TimeInState => _fsm.TimeInState;

        internal void SetAwareness(float value) => Awareness = Mathf.Clamp01(value);

        /// <summary>Walks horizontally toward <paramref name="targetX"/>; stops at ledges and walls. Returns true when arrived.</summary>
        internal bool MoveTowardsX(float targetX, float speed, float tolerance = 0.15f)
        {
            float dx = targetX - _body.position.x;
            if (Mathf.Abs(dx) <= tolerance)
            {
                Stop();
                return true;
            }

            int direction = dx > 0f ? 1 : -1;
            Face(direction);
            if (IsBlocked(direction))
            {
                Stop();
                return true;
            }

            _body.linearVelocity = new Vector2(direction * speed, _body.linearVelocity.y);
            return false;
        }

        internal void Stop() => _body.linearVelocity = new Vector2(0f, _body.linearVelocity.y);

        internal void Face(int direction)
        {
            if (direction == 0 || direction == Facing) return;
            Facing = direction;
            vision.transform.localRotation = Quaternion.Euler(0f, 0f, direction > 0 ? 0f : 180f);
            if (body != null) body.flipX = direction < 0;
        }

        internal void TryAttack()
        {
            if (Target == null || Time.time < _attackReadyAt) return;
            if (Vector2.Distance(Target.Position, _body.position) > config.AttackRange) return;
            _attackReadyAt = Time.time + config.AttackCooldown;
            _attackPending = true;
            if (animator != null) animator.SetTrigger(AnimatorParams.Attack);
            else OnHitFrame();
        }

        /// <summary>Called by the attack animation through <see cref="GuardAnimationEvents"/>.</summary>
        public void OnHitFrame()
        {
            if (!_attackPending) return;
            _attackPending = false;
            if (Target == null || !Target.IsAlive) return;
            if (Vector2.Distance(Target.Position, _body.position) > config.AttackRange * 1.3f) return;
            if (Target.Transform.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(new DamageInfo(config.AttackDamage, _body.position, 7f, DamageType.Contact));
        }

        private bool IsBlocked(int direction)
        {
            Vector2 position = _body.position;
            Vector2 ahead = position + new Vector2(direction * 0.6f, 0f);
            bool wall = Physics2D.Raycast(position, Vector2.right * direction, 0.7f, Layers.ObstacleMask).collider != null;
            bool ground = Physics2D.Raycast(ahead, Vector2.down, 1.6f, Layers.SolidMask).collider != null;
            return wall || !ground;
        }

        private void UpdatePresentation()
        {
            vision.SetAlert(StateId == GuardStateId.Chase ? 1f : Awareness);
            if (animator != null)
            {
                animator.SetFloat(AnimatorParams.Speed, Mathf.Abs(_body.linearVelocity.x) / Mathf.Max(0.01f, config.ChaseSpeed));
                animator.SetBool(AnimatorParams.Alert, StateId == GuardStateId.Chase);
            }

            if (alertIcon == null) return;
            bool show = StateId is GuardStateId.Suspicious or GuardStateId.Chase or GuardStateId.Search;
            alertIcon.enabled = show;
            if (show) alertIcon.color = StateId == GuardStateId.Chase ? alertColor : suspiciousColor;
        }
    }
}
