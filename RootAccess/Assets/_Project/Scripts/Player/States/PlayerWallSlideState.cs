using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>Clamped slide down a wall; jump kicks away from it. Brief "stick" time before releasing.</summary>
    public sealed class PlayerWallSlideState : PlayerStateBase
    {
        private int _wallDirection;
        private float _releaseTimer;

        public PlayerWallSlideState(PlayerController owner) : base(owner) { }

        public override PlayerStateId Id => PlayerStateId.WallSlide;

        public static void WallJump(PlayerController owner, int wallDirection)
        {
            Vector2 kick = owner.Config.WallJumpVelocity;
            owner.Body.linearVelocity = new Vector2(-wallDirection * kick.x, kick.y);
            owner.Facing = -wallDirection;
            owner.ControlLockTimer = owner.Config.WallJumpControlLock;
            owner.JumpBufferTimer = 0f;
            owner.IsJumpRising = true;
            owner.AirJumpsUsed = 0;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Jump, owner.transform.position));
            owner.ChangeState(PlayerStateId.Airborne);
        }

        public override void Enter()
        {
            _wallDirection = Owner.Sensors.WallDirection;
            _releaseTimer = Config.WallStickTime;
            Owner.Facing = -_wallDirection;
            Owner.Body.gravityScale = Config.BaseGravityScale;
            Owner.AirJumpsUsed = 0;
            Owner.IsJumpRising = false;
        }

        public override void FixedTick(float deltaTime)
        {
            if (Owner.Sensors.IsGrounded)
            {
                Owner.ChangeState(PlayerStateId.Grounded);
                return;
            }

            if (Owner.JumpBufferTimer > 0f)
            {
                WallJump(Owner, _wallDirection);
                return;
            }

            if (TryStartDash()) return;

            if (Owner.Sensors.WallDirection != _wallDirection)
            {
                Owner.ChangeState(PlayerStateId.Airborne);
                return;
            }

            bool pushingAway = Owner.MoveInput.x * _wallDirection < -0.2f;
            _releaseTimer = pushingAway ? _releaseTimer - deltaTime : Config.WallStickTime;
            if (_releaseTimer <= 0f)
            {
                Owner.ChangeState(PlayerStateId.Airborne);
                return;
            }

            Vector2 velocity = Owner.Body.linearVelocity;
            velocity.x = 0f;
            velocity.y = Mathf.Max(velocity.y, -Config.WallSlideMaxSpeed);
            Owner.Body.linearVelocity = velocity;
        }
    }
}
