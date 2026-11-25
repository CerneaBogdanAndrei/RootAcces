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
        Return,
        Investigate
    }

    /// <summary>
    /// Patrolling guard: Patrol → Suspicious → Chase → Search → Return, driven by an awareness meter fed by its
    /// vision cone. Player noises (<see cref="PlayerNoise"/>) send it to Investigate; a raised alarm inside its response
    /// radius sends it to Search at the alarm. Melee hits resolve on the attack animation's hit frame (Animation Event);
    /// ranged guards (sniper) fire pooled projectiles instead.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class Guard : MonoBehaviour, IEmpTarget, IOperatorTarget
    {
        /// <summary>The Operator's distraction: a fake radio call sends the guard to check this far behind it.</summary>
        private const float DistractionDistance = 5f;

        private const float EmpStunFraction = 0.5f;
        private const float EarHeight = 1.2f;
        private const float ProbeHeight = 0.5f;

        [SerializeField] private GuardConfig config;
        [SerializeField] private VisionCone vision;
        [SerializeField] private Transform[] patrolPoints = new Transform[0];
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer alertIcon;
        [SerializeField] private Color suspiciousColor = new(1f, 0.85f, 0.2f);
        [SerializeField] private Color alertColor = new(1f, 0.2f, 0.25f);
        [SerializeField, Tooltip("Ranged guards only.")] private Projectile projectilePrefab;
        [SerializeField, Tooltip("Ranged guards only: where projectiles leave the rifle.")] private Transform muzzle;

        private readonly StateMachine _fsm = new();
        private GuardPatrolState _patrol;
        private GuardSuspiciousState _suspicious;
        private GuardChaseState _chase;
        private GuardSearchState _search;
        private GuardReturnState _return;
        private GuardInvestigateState _investigate;
        private Rigidbody2D _body;
        private PoolService _pool;
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

        /// <summary>Where the last heard noise came from (the Investigate state walks there).</summary>
        public Vector2 NoisePosition { get; private set; }

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
            _investigate = new GuardInvestigateState(this);
            _fsm.StateChanged += (_, current) => StateId = ((GuardStateBase)current).Id;
        }

        private void Start()
        {
            ServiceLocator.TryGet(out _pool);
            _fsm.ChangeState(_patrol);
        }

        public string OperatorId => GetInstanceID().ToString();
        public OperatorTargetKind OperatorKind => OperatorTargetKind.Guard;
        public Vector2 OperatorPosition => transform.position;
        public bool OperatorActive => StateId != GuardStateId.Patrol;
        public bool CanOperate => isActiveAndEnabled && _body != null && !IsEmpDisabled && StateId != GuardStateId.Chase;

        /// <summary>
        /// Operator distraction: a fake radio call ("check behind you") pulls the guard away from the route ahead. It is
        /// not a sound, so walls do not muffle it.
        /// </summary>
        public void Operate() => Investigate(_body.position + new Vector2(-Facing * DistractionDistance, 0f));

        private void OnEnable()
        {
            OperatorRegistry.Register(this);
            EventBus<PlayerNoise>.Subscribe(OnPlayerNoise);
            EventBus<AlarmRaised>.Subscribe(OnAlarmRaised);
        }

        private void OnDisable()
        {
            OperatorRegistry.Unregister(this);
            EventBus<PlayerNoise>.Unsubscribe(OnPlayerNoise);
            EventBus<AlarmRaised>.Unsubscribe(OnAlarmRaised);
        }

        /// <summary>
        /// Hears a player noise unless busy chasing or stunned. Walls between the guard and the noise muffle it
        /// (<see cref="Hearing.OcclusionFactor"/>). Already searching guards simply retarget.
        /// </summary>
        internal void OnPlayerNoise(PlayerNoise noise)
        {
            if (!isActiveAndEnabled || _body == null || IsEmpDisabled || StateId == GuardStateId.Chase) return;
            Vector2 ears = _body.position + Vector2.up * EarHeight;
            bool occluded = Physics2D.Linecast(ears, noise.Position + Vector2.up * 0.2f, Layers.ObstacleMask).collider != null;
            if (!Hearing.CanHear(ears, noise.Position, noise.Radius, occluded, config.HearingSensitivity)) return;

            Investigate(noise.Position);
        }

        /// <summary>Walks to <paramref name="position"/> and looks around (searching guards just retarget their search).</summary>
        internal void Investigate(Vector2 position)
        {
            NoisePosition = position;
            if (StateId == GuardStateId.Search)
            {
                LastKnownPlayerPosition = position;
                return;
            }

            if (StateId != GuardStateId.Investigate) ChangeState(GuardStateId.Investigate);
            else _investigate.Retarget();
        }

        /// <summary>An alarm (camera, motion sensor) inside the response radius makes the guard search its position.</summary>
        internal void OnAlarmRaised(AlarmRaised alarm)
        {
            if (!isActiveAndEnabled || _body == null || IsEmpDisabled || StateId == GuardStateId.Chase) return;
            if (Vector2.Distance(_body.position, alarm.Position) > config.AlarmResponseRadius) return;
            LastKnownPlayerPosition = alarm.Position;
            if (StateId != GuardStateId.Search) ChangeState(GuardStateId.Search);
        }

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
                GuardStateId.Investigate => _investigate,
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
            if (config.Ranged)
            {
                if (!CanSeePlayer) return;
                _attackReadyAt = Time.time + config.AttackCooldown;
                Shoot();
                return;
            }

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

        private void Shoot()
        {
            if (projectilePrefab == null || _pool == null) return;
            Vector2 origin = muzzle != null ? (Vector2)muzzle.position : _body.position + Vector2.up * EarHeight;
            Vector2 aim = Target.Position + Vector2.up * 0.6f - origin;
            Face(aim.x >= 0f ? 1 : -1);
            Projectile projectile = _pool.Spawn(projectilePrefab, origin, Quaternion.identity);
            if (projectile == null) return;
            projectile.Launch(aim, gameObject);
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.TurretShot, origin));
            if (animator != null) animator.SetTrigger(AnimatorParams.Attack);
        }

        private bool IsBlocked(int direction)
        {
            // Probes start half a unit above the feet: a ray that starts exactly on the floor's surface does not report it.
            Vector2 position = _body.position + Vector2.up * ProbeHeight;
            Vector2 ahead = position + new Vector2(direction * 0.6f, 0f);
            bool wall = Physics2D.Raycast(position, Vector2.right * direction, 0.7f, Layers.ObstacleMask).collider != null;
            bool ground = Physics2D.Raycast(ahead, Vector2.down, 1.6f + ProbeHeight, Layers.SolidMask).collider != null;
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
            bool show = StateId is GuardStateId.Suspicious or GuardStateId.Chase or GuardStateId.Search or GuardStateId.Investigate;
            alertIcon.enabled = show;
            if (show) alertIcon.color = StateId == GuardStateId.Chase ? alertColor : suspiciousColor;
        }
    }
}
