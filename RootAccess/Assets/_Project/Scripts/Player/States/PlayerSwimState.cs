using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>
    /// Swim skeleton: low gravity, free 2D movement, buoyancy toward the surface and a jump out near it.
    /// Full water level tuning (currents, breath) is planned for the Sewers level (ROADMAP P4).
    /// </summary>
    public sealed class PlayerSwimState : PlayerStateBase
    {
        public PlayerSwimState(PlayerController owner) : base(owner) { }

        public override PlayerStateId Id => PlayerStateId.Swim;

        public override void Enter()
        {
            Owner.Body.gravityScale = Config.WaterGravityScale;
            Owner.Body.linearVelocity *= 0.4f;
            Owner.ResetAirAbilities();
            Owner.IsJumpRising = false;
            EventBus<UnderwaterChanged>.Publish(new UnderwaterChanged(true));
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.Splash, Owner.transform.position));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.Splash, new Vector3(Owner.transform.position.x, Owner.WaterSurfaceY, 0f)));
        }

        public override void Exit()
        {
            Owner.Body.gravityScale = Config.BaseGravityScale;
            EventBus<UnderwaterChanged>.Publish(new UnderwaterChanged(false));
        }

        public override void Tick(float deltaTime) => Owner.FaceInput();

        public override void FixedTick(float deltaTime)
        {
            if (!Owner.InWater)
            {
                Owner.ChangeState(PlayerStateId.Airborne);
                return;
            }

            bool nearSurface = Owner.Body.position.y > Owner.WaterSurfaceY - 0.6f;
            if (nearSurface && Owner.JumpBufferTimer > 0f)
            {
                Owner.Body.linearVelocity = new Vector2(Owner.Body.linearVelocity.x, Config.SwimJumpOutVelocity);
                Owner.JumpBufferTimer = 0f;
                Owner.IsJumpRising = true;
                Owner.ChangeState(PlayerStateId.Airborne);
                return;
            }

            Vector2 target = Owner.MoveInput * Config.SwimSpeed;
            if (Owner.MoveInput.sqrMagnitude < 0.01f && !nearSurface) target.y = Config.Buoyancy * 0.5f;
            Vector2 velocity = Vector2.MoveTowards(Owner.Body.linearVelocity, target, Config.SwimAcceleration * deltaTime);
            Owner.Body.linearVelocity = velocity;
        }
    }
}
