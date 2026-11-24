using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>Fixed-duration horizontal burst with gravity disabled; one air dash per airtime.</summary>
    public sealed class PlayerDashState : PlayerStateBase
    {
        private float _timer;
        private int _direction;

        public PlayerDashState(PlayerController owner) : base(owner) { }

        public override PlayerStateId Id => PlayerStateId.Dash;

        public override void Enter()
        {
            Owner.DashBufferTimer = 0f;
            Owner.DashCooldownTimer = Config.DashCooldown;
            if (!Owner.IsGrounded) Owner.AirDashUsed = true;

            float x = Owner.MoveInput.x;
            _direction = Mathf.Abs(x) > 0.1f ? (int)Mathf.Sign(x) : Owner.Facing;
            Owner.Facing = _direction;
            _timer = Config.DashDuration;
            Owner.Body.gravityScale = 0f;
            Owner.Body.linearVelocity = new Vector2(_direction * Config.DashSpeed, 0f);
            Owner.IsJumpRising = false;

            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Dash, Owner.transform.position));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.LandingDust, Owner.transform.position));
        }

        public override void Exit()
        {
            Owner.Body.gravityScale = Config.BaseGravityScale;
            Vector2 velocity = Owner.Body.linearVelocity;
            Owner.Body.linearVelocity = new Vector2(velocity.x * 0.45f, velocity.y);
        }

        public override void FixedTick(float deltaTime)
        {
            _timer -= deltaTime;
            Owner.Body.linearVelocity = new Vector2(_direction * Config.DashSpeed, 0f);
            bool blocked = Owner.Sensors.WallDirection == _direction;
            if (_timer > 0f && !blocked) return;
            Owner.ResolveDefaultState();
        }
    }
}
