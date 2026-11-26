using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>Idle / run / crouch on the ground. Refreshes coyote time and air abilities every tick.</summary>
    public sealed class PlayerGroundedState : PlayerStateBase
    {
        public PlayerGroundedState(PlayerController owner) : base(owner) { }

        public override PlayerStateId Id => PlayerStateId.Grounded;

        public override void Enter()
        {
            Owner.Body.gravityScale = Config.BaseGravityScale;
            Owner.IsJumpRising = false;
            Owner.ResetAirAbilities();
        }

        public override void Exit()
        {
            if (Owner.IsCrouching) Owner.SetCrouch(false);
        }

        public override void Tick(float deltaTime)
        {
            Owner.CoyoteTimer = Config.CoyoteTime;
            Owner.SetCrouch(Owner.CrouchRequested);
            Owner.FaceInput();
        }

        public override void FixedTick(float deltaTime)
        {
            if (Owner.InWater && Owner.IsSubmerged)
            {
                Owner.ChangeState(PlayerStateId.Swim);
                return;
            }

            if (Owner.JumpBufferTimer > 0f && !Owner.IsCrouching)
            {
                Owner.PerformJump(Config.JumpHeight, false);
                Owner.ChangeState(PlayerStateId.Airborne);
                return;
            }

            if (TryStartDash()) return;

            if (!Owner.Sensors.IsGrounded)
            {
                Owner.ChangeState(PlayerStateId.Airborne);
                return;
            }

            // On a gondola the run speed is relative to the platform, so standing still rides along with it.
            Vector2 carry = Owner.GroundCarryVelocity;
            float speed = Config.MaxRunSpeed * (Owner.IsCrouching ? Config.CrouchSpeedMultiplier : 1f);
            Owner.ApplyHorizontal(Owner.MoveInput.x * speed + carry.x, Config.GroundAcceleration, Config.GroundDeceleration, deltaTime);

            Vector2 velocity = Owner.Body.linearVelocity;
            if (velocity.y > 0f && !Owner.IsJumpRising) Owner.Body.linearVelocity = new Vector2(velocity.x, Mathf.Min(velocity.y, Mathf.Max(0.5f, carry.y)));
            else if (carry.y < 0f && !Owner.IsJumpRising) Owner.Body.linearVelocity = new Vector2(velocity.x, Mathf.Min(velocity.y, carry.y));
        }
    }
}
