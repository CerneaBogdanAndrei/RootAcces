using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>All movement tunables in one asset so designers can iterate without touching code.</summary>
    [CreateAssetMenu(menuName = "Root Access/Player Movement Config", fileName = "PlayerMovementConfig")]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("Run")]
        [SerializeField] private float maxRunSpeed = 8.5f;
        [SerializeField] private float groundAcceleration = 75f;
        [SerializeField] private float groundDeceleration = 90f;
        [SerializeField] private float airAcceleration = 50f;
        [SerializeField] private float airDeceleration = 30f;
        [SerializeField, Range(0.1f, 1f)] private float crouchSpeedMultiplier = 0.45f;

        [Header("Jump")]
        [SerializeField] private float jumpHeight = 3.4f;
        [SerializeField] private float airJumpHeight = 2.6f;
        [SerializeField] private float baseGravityScale = 3.2f;
        [SerializeField] private float fallGravityMultiplier = 1.7f;
        [SerializeField] private float jumpCutGravityMultiplier = 2.8f;
        [SerializeField] private float maxFallSpeed = 20f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.12f;
        [SerializeField, Min(0)] private int extraAirJumps = 1;
        [SerializeField] private bool doubleJumpRequiresSkill;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 21f;
        [SerializeField] private float dashDuration = 0.16f;
        [SerializeField] private float dashCooldown = 0.45f;
        [SerializeField] private float dashInputBuffer = 0.1f;

        [Header("Wall")]
        [SerializeField] private float wallSlideMaxSpeed = 2.6f;
        [SerializeField] private Vector2 wallJumpVelocity = new(9.5f, 13.5f);
        [SerializeField] private float wallJumpControlLock = 0.16f;
        [SerializeField] private float wallStickTime = 0.12f;

        [Header("Swim")]
        [SerializeField] private float swimSpeed = 4.5f;
        [SerializeField] private float swimAcceleration = 18f;
        [SerializeField] private float waterGravityScale = 0.15f;
        [SerializeField] private float buoyancy = 2.5f;
        [SerializeField] private float swimJumpOutVelocity = 11f;

        [Header("Collision")]
        [SerializeField] private float groundCheckDistance = 0.08f;
        [SerializeField] private float wallCheckDistance = 0.08f;
        [SerializeField, Range(0.3f, 0.9f)] private float crouchHeightRatio = 0.6f;
        [SerializeField] private float landingImpactSpeed = 14f;

        [Header("Damage")]
        [SerializeField, Min(1)] private int maxHealth = 5;
        [SerializeField] private float invulnerabilitySeconds = 1f;
        [SerializeField] private float hurtControlLock = 0.25f;
        [SerializeField] private float killPlaneY = -40f;

        public float MaxRunSpeed => maxRunSpeed;
        public float GroundAcceleration => groundAcceleration;
        public float GroundDeceleration => groundDeceleration;
        public float AirAcceleration => airAcceleration;
        public float AirDeceleration => airDeceleration;
        public float CrouchSpeedMultiplier => crouchSpeedMultiplier;
        public float JumpHeight => jumpHeight;
        public float AirJumpHeight => airJumpHeight;
        public float BaseGravityScale => baseGravityScale;
        public float FallGravityMultiplier => fallGravityMultiplier;
        public float JumpCutGravityMultiplier => jumpCutGravityMultiplier;
        public float MaxFallSpeed => maxFallSpeed;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public int ExtraAirJumps => extraAirJumps;
        public bool DoubleJumpRequiresSkill => doubleJumpRequiresSkill;
        public float DashSpeed => dashSpeed;
        public float DashDuration => dashDuration;
        public float DashCooldown => dashCooldown;
        public float DashInputBuffer => dashInputBuffer;
        public float WallSlideMaxSpeed => wallSlideMaxSpeed;
        public Vector2 WallJumpVelocity => wallJumpVelocity;
        public float WallJumpControlLock => wallJumpControlLock;
        public float WallStickTime => wallStickTime;
        public float SwimSpeed => swimSpeed;
        public float SwimAcceleration => swimAcceleration;
        public float WaterGravityScale => waterGravityScale;
        public float Buoyancy => buoyancy;
        public float SwimJumpOutVelocity => swimJumpOutVelocity;
        public float GroundCheckDistance => groundCheckDistance;
        public float WallCheckDistance => wallCheckDistance;
        public float CrouchHeightRatio => crouchHeightRatio;
        public float LandingImpactSpeed => landingImpactSpeed;
        public int MaxHealth => maxHealth;
        public float InvulnerabilitySeconds => invulnerabilitySeconds;
        public float HurtControlLock => hurtControlLock;
        public float KillPlaneY => killPlaneY;
    }
}
