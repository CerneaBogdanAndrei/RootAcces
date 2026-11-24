namespace RootAccess.Player
{
    /// <summary>Jumping and falling: variable jump height, coyote jump, double jump, wall-slide entry, air dash.</summary>
    public sealed class PlayerAirborneState : PlayerStateBase
    {
        public PlayerAirborneState(PlayerController owner) : base(owner) { }

        public override PlayerStateId Id => PlayerStateId.Airborne;

        public override void Tick(float deltaTime)
        {
            Owner.CoyoteTimer -= deltaTime;
            if (Owner.ControlLockTimer <= 0f) Owner.FaceInput();
        }

        public override void FixedTick(float deltaTime)
        {
            if (Owner.InWater && Owner.IsSubmerged)
            {
                Owner.ChangeState(PlayerStateId.Swim);
                return;
            }

            if (Owner.JumpBufferTimer > 0f)
            {
                if (Owner.CoyoteTimer > 0f)
                {
                    Owner.PerformJump(Config.JumpHeight, false);
                }
                else if (Owner.Sensors.WallDirection != 0)
                {
                    PlayerWallSlideState.WallJump(Owner, Owner.Sensors.WallDirection);
                }
                else if (Owner.CanAirJump)
                {
                    Owner.PerformJump(Config.AirJumpHeight, true);
                }
            }

            if (TryStartDash()) return;

            if (Owner.Sensors.IsGrounded && !Owner.IsJumpRising)
            {
                Owner.ChangeState(PlayerStateId.Grounded);
                return;
            }

            int wall = Owner.Sensors.WallDirection;
            if (wall != 0 && Owner.Body.linearVelocity.y <= 0f && Owner.MoveInput.x * wall > 0.2f)
            {
                Owner.ChangeState(PlayerStateId.WallSlide);
                return;
            }

            if (Owner.ControlLockTimer <= 0f)
                Owner.ApplyHorizontal(Owner.MoveInput.x * Config.MaxRunSpeed, Config.AirAcceleration, Config.AirDeceleration, deltaTime);
            Owner.ApplyAirGravity();
        }
    }
}
