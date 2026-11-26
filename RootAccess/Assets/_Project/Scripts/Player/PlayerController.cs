using System;
using RootAccess.Core;
using RootAccess.Input;
using RootAccess.Systems;
using RootAccess.VFX;
using UnityEngine;

namespace RootAccess.Player
{
    public enum PlayerStateId
    {
        Grounded,
        Airborne,
        WallSlide,
        Dash,
        Swim,
        Hack,
        Hurt,
        Dead,
        Teleport
    }

    /// <summary>
    /// Rigidbody2D platformer controller. Input is buffered here (jump buffer, coyote time, dash buffer);
    /// each movement mode is an explicit state object driven by a <see cref="StateMachine"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    [DefaultExecutionOrder(-10)]
    public sealed class PlayerController : MonoBehaviour, IPlayerTarget, ITeleportable, IWaterAffectable, IInteractor
    {
        [SerializeField] private InputReader input;
        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private DissolveController dissolve;
        [SerializeField] private PlayerVisibility visibility;

        private readonly StateMachine _fsm = new();
        private PlayerGroundedState _grounded;
        private PlayerAirborneState _airborne;
        private PlayerWallSlideState _wallSlide;
        private PlayerDashState _dash;
        private PlayerSwimState _swim;
        private PlayerHackState _hack;
        private PlayerHurtState _hurt;
        private PlayerDeadState _dead;
        private PlayerTeleportState _teleport;

        private Vector2 _standingSize;
        private Vector2 _standingOffset;
        private float _previousVelocityY;
        private int _waterVolumes;
        private bool _doubleJumpUnlocked = true;

        private const float ServoDashCooldownMultiplier = 0.6f;

        public event Action<PlayerStateId> StateChanged;
        public event Action Jumped;
        public event Action Landed;

        /// <summary>How far a hard landing carries (world units), from the softest counted impact to terminal velocity.</summary>
        private const float LandingNoiseMin = 4f;
        private const float LandingNoiseMax = 9f;

        internal Rigidbody2D Body { get; private set; }
        internal CapsuleCollider2D Capsule { get; private set; }
        internal PlayerSensors Sensors { get; private set; }
        internal InputReader InputReader => input;
        public PlayerMovementConfig Config => config;

        public PlayerStateId StateId { get; private set; }
        public int Facing { get; internal set; } = 1;
        public bool IsCrouching { get; private set; }
        public bool IsGrounded => Sensors != null && Sensors.IsGrounded;
        public bool InWater => _waterVolumes > 0;
        public float WaterSurfaceY { get; private set; }
        public bool IsSubmerged => InWater && Body.position.y < WaterSurfaceY - 0.1f;

        internal float CoyoteTimer { get; set; }
        internal float JumpBufferTimer { get; set; }
        internal float DashBufferTimer { get; set; }
        internal float DashCooldownTimer { get; set; }
        internal float ControlLockTimer { get; set; }
        internal int AirJumpsUsed { get; set; }
        internal bool AirDashUsed { get; set; }
        internal bool IsJumpRising { get; set; }
        internal Vector2 MoveInput => input != null ? input.Move : Vector2.zero;
        internal bool JumpHeld => input != null && input.JumpHeld;
        internal bool CrouchRequested => input != null && (input.CrouchHeld || input.Move.y < -0.6f);
        internal bool CanAirJump => _doubleJumpUnlocked && AirJumpsUsed < config.ExtraAirJumps;
        internal bool CanDash => DashCooldownTimer <= 0f && (IsGrounded || !AirDashUsed);
        internal float DashCooldownMultiplier { get; private set; } = 1f;

        /// <summary>Extra hit points granted by the "armor plating" skill.</summary>
        public int BonusHealth { get; private set; }

        public Transform Transform => transform;
        public GameObject GameObject => gameObject;
        public Vector2 Position => Body != null ? Body.position : (Vector2)transform.position;
        public Vector2 Velocity => Body != null ? Body.linearVelocity : Vector2.zero;
        public bool IsHidden => visibility != null && visibility.IsHidden;
        public bool IsAlive => StateId != PlayerStateId.Dead;
        public bool CanTeleport => IsAlive && StateId != PlayerStateId.Teleport && StateId != PlayerStateId.Hack;

        private void Awake()
        {
            if (input == null || config == null)
            {
                Debug.LogError("[PlayerController] InputReader and PlayerMovementConfig must be assigned.", this);
                enabled = false;
                return;
            }

            Body = GetComponent<Rigidbody2D>();
            Capsule = GetComponent<CapsuleCollider2D>();
            Sensors = new PlayerSensors(Capsule, config);
            _standingSize = Capsule.size;
            _standingOffset = Capsule.offset;

            Body.gravityScale = config.BaseGravityScale;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.freezeRotation = true;

            _grounded = new PlayerGroundedState(this);
            _airborne = new PlayerAirborneState(this);
            _wallSlide = new PlayerWallSlideState(this);
            _dash = new PlayerDashState(this);
            _swim = new PlayerSwimState(this);
            _hack = new PlayerHackState(this);
            _hurt = new PlayerHurtState(this);
            _dead = new PlayerDeadState(this);
            _teleport = new PlayerTeleportState(this);
        }

        private void OnEnable()
        {
            if (input == null || config == null) return;
            ServiceLocator.Register<IPlayerTarget>(this);
            input.JumpStartedEvent += OnJumpPressed;
            input.DashEvent += OnDashPressed;
            EventBus<HackSessionStarted>.Subscribe(OnHackStarted);
            EventBus<HackSessionEnded>.Subscribe(OnHackEnded);
            EventBus<SkillUnlockedEvent>.Subscribe(OnSkillUnlocked);
            _fsm.StateChanged += OnFsmStateChanged;
        }

        private void OnDisable()
        {
            if (input == null || config == null) return;
            ServiceLocator.Unregister<IPlayerTarget>(this);
            input.JumpStartedEvent -= OnJumpPressed;
            input.DashEvent -= OnDashPressed;
            EventBus<HackSessionStarted>.Unsubscribe(OnHackStarted);
            EventBus<HackSessionEnded>.Unsubscribe(OnHackEnded);
            EventBus<SkillUnlockedEvent>.Unsubscribe(OnSkillUnlocked);
            _fsm.StateChanged -= OnFsmStateChanged;
        }

        private void Start()
        {
            RefreshAbilities();
            Sensors.Refresh(Body.linearVelocity.y);
            _fsm.ChangeState(Sensors.IsGrounded ? _grounded : _airborne);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            JumpBufferTimer -= dt;
            DashBufferTimer -= dt;
            DashCooldownTimer -= dt;
            ControlLockTimer -= dt;
            _fsm.Tick(dt);
        }

        private void FixedUpdate()
        {
            _previousVelocityY = Body.linearVelocity.y;
            Sensors.Refresh(Body.linearVelocity.y);
            if (Sensors.JustLanded) Landed?.Invoke();
            _fsm.FixedTick(Time.fixedDeltaTime);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.contactCount == 0 || collision.GetContact(0).normal.y < 0.5f) return;
            if (-_previousVelocityY < config.LandingImpactSpeed) return;

            Vector3 feet = new(Body.position.x, Capsule.bounds.min.y, 0f);
            EventBus<ScreenShakeRequested>.Publish(new ScreenShakeRequested(Mathf.InverseLerp(config.LandingImpactSpeed, config.MaxFallSpeed, -_previousVelocityY) + 0.4f));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.LandingDust, feet));
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Land, feet));
            float impact = Mathf.InverseLerp(config.LandingImpactSpeed, config.MaxFallSpeed, -_previousVelocityY);
            EventBus<PlayerNoise>.Publish(new PlayerNoise(feet, Mathf.Lerp(LandingNoiseMin, LandingNoiseMax, impact)));
        }

        internal void ChangeState(PlayerStateId id) => _fsm.ChangeState(StateFor(id));

        internal void ResolveDefaultState()
        {
            if (InWater && IsSubmerged) ChangeState(PlayerStateId.Swim);
            else ChangeState(Sensors.IsGrounded ? PlayerStateId.Grounded : PlayerStateId.Airborne);
        }

        /// <summary>Velocity of the moving surface under the feet (a gondola); zero on static ground or in the air.</summary>
        internal Vector2 GroundCarryVelocity
        {
            get
            {
                Collider2D ground = Sensors.GroundCollider;
                if (ground == null || !Sensors.IsGrounded) return Vector2.zero;
                Rigidbody2D body = ground.attachedRigidbody;
                return body != null && body.TryGetComponent(out ISurfaceCarrier carrier) ? carrier.CarryVelocity : Vector2.zero;
            }
        }

        internal float JumpVelocity(float height) =>
            Mathf.Sqrt(2f * Mathf.Abs(Physics2D.gravity.y) * config.BaseGravityScale * height);

        internal void PerformJump(float height, bool airJump)
        {
            Body.linearVelocity = new Vector2(Body.linearVelocity.x, JumpVelocity(height));
            JumpBufferTimer = 0f;
            CoyoteTimer = 0f;
            IsJumpRising = true;
            SetCrouch(false);
            if (airJump) AirJumpsUsed++;
            EventBus<SfxRequested>.Publish(new SfxRequested(airJump ? SfxId.DoubleJump : SfxId.Jump, transform.position));
            if (airJump) EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.LandingDust, transform.position));
            Jumped?.Invoke();
        }

        internal void ApplyHorizontal(float targetSpeed, float acceleration, float deceleration, float dt)
        {
            Vector2 velocity = Body.linearVelocity;
            bool accelerating = Mathf.Abs(targetSpeed) > 0.01f &&
                                (Mathf.Approximately(velocity.x, 0f) || Mathf.Sign(targetSpeed) == Mathf.Sign(velocity.x));
            float rate = accelerating ? acceleration : deceleration;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, rate * dt);
            Body.linearVelocity = velocity;
        }

        internal void ApplyAirGravity()
        {
            float vy = Body.linearVelocity.y;
            float scale = config.BaseGravityScale;
            if (vy < 0f)
            {
                scale *= config.FallGravityMultiplier;
                IsJumpRising = false;
            }
            else if (IsJumpRising && !JumpHeld)
            {
                scale *= config.JumpCutGravityMultiplier;
            }

            Body.gravityScale = scale;
            if (vy < -config.MaxFallSpeed) Body.linearVelocity = new Vector2(Body.linearVelocity.x, -config.MaxFallSpeed);
        }

        internal void FaceInput()
        {
            float x = MoveInput.x;
            if (x > 0.1f) Facing = 1;
            else if (x < -0.1f) Facing = -1;
        }

        internal void SetCrouch(bool crouch)
        {
            if (crouch == IsCrouching) return;
            if (!crouch && !Sensors.CanStandUp(_standingSize.y)) return;

            IsCrouching = crouch;
            if (crouch)
            {
                float height = _standingSize.y * config.CrouchHeightRatio;
                Capsule.size = new Vector2(_standingSize.x, height);
                Capsule.offset = new Vector2(_standingOffset.x, _standingOffset.y - (_standingSize.y - height) * 0.5f);
            }
            else
            {
                Capsule.size = _standingSize;
                Capsule.offset = _standingOffset;
            }
        }

        internal void ResetAirAbilities()
        {
            AirJumpsUsed = 0;
            AirDashUsed = false;
        }

        public void OnDamaged(in DamageInfo damage)
        {
            if (!IsAlive) return;
            _hurt.Configure(damage);
            ChangeState(PlayerStateId.Hurt);
        }

        public void OnDied() => ChangeState(PlayerStateId.Dead);

        public void Respawn(Vector3 position)
        {
            Body.simulated = true;
            Body.position = position;
            transform.position = position;
            Body.linearVelocity = Vector2.zero;
            ResetAirAbilities();
            if (dissolve != null) dissolve.SetImmediate(0f);
            ChangeState(PlayerStateId.Airborne);
        }

        public void BeginTeleport()
        {
            if (!CanTeleport) return;
            ChangeState(PlayerStateId.Teleport);
            if (dissolve != null) dissolve.DissolveOut();
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.TeleportOut, transform.position));
        }

        public void CompleteTeleport(Vector3 destination)
        {
            Body.position = destination;
            transform.position = destination;
            Body.linearVelocity = Vector2.zero;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.TeleportIn, destination));
            if (dissolve != null) dissolve.DissolveIn(FinishTeleport);
            else FinishTeleport();
        }

        public void EnterWater(in WaterVolumeInfo water)
        {
            _waterVolumes++;
            WaterSurfaceY = water.SurfaceY;
        }

        public void UpdateWaterSurface(float surfaceY) => WaterSurfaceY = surfaceY;

        public void ExitWater(Collider2D volume)
        {
            _waterVolumes = Mathf.Max(0, _waterVolumes - 1);
            if (_waterVolumes == 0 && StateId == PlayerStateId.Swim) ChangeState(PlayerStateId.Airborne);
        }

        private void FinishTeleport()
        {
            Body.simulated = true;
            ResolveDefaultState();
            EventBus<TeleportCompleted>.Publish(new TeleportCompleted(transform.position));
        }

        private IState StateFor(PlayerStateId id) => id switch
        {
            PlayerStateId.Grounded => _grounded,
            PlayerStateId.Airborne => _airborne,
            PlayerStateId.WallSlide => _wallSlide,
            PlayerStateId.Dash => _dash,
            PlayerStateId.Swim => _swim,
            PlayerStateId.Hack => _hack,
            PlayerStateId.Hurt => _hurt,
            PlayerStateId.Dead => _dead,
            PlayerStateId.Teleport => _teleport,
            _ => _airborne
        };

        private void OnFsmStateChanged(IState previous, IState current)
        {
            StateId = current is PlayerStateBase state ? state.Id : PlayerStateId.Airborne;
            StateChanged?.Invoke(StateId);
        }

        private void OnJumpPressed() => JumpBufferTimer = config.JumpBufferTime;

        private void OnDashPressed() => DashBufferTimer = config.DashInputBuffer;

        private void OnHackStarted(HackSessionStarted _)
        {
            if (IsAlive) ChangeState(PlayerStateId.Hack);
        }

        private void OnHackEnded(HackSessionEnded _)
        {
            if (StateId == PlayerStateId.Hack) ResolveDefaultState();
        }

        private void OnSkillUnlocked(SkillUnlockedEvent _) => RefreshAbilities();

        internal void RefreshAbilities()
        {
            bool hasSession = ServiceLocator.TryGet(out SessionService skills);
            DashCooldownMultiplier = hasSession && skills.HasSkillEffect(SkillEffect.DashCooldown) ? ServoDashCooldownMultiplier : 1f;
            BonusHealth = hasSession && skills.HasSkillEffect(SkillEffect.ArmorPlating) ? 1 : 0;
            if (!config.DoubleJumpRequiresSkill)
            {
                _doubleJumpUnlocked = true;
                return;
            }

            _doubleJumpUnlocked = ServiceLocator.TryGet(out SessionService session) &&
                                  session.Session.Skills.HasEffect(SkillEffect.DoubleJump);
        }
    }
}
