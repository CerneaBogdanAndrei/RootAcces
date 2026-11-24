using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Player
{
    /// <summary>Standing still at a terminal while the hacking overlay is open.</summary>
    public sealed class PlayerHackState : PlayerStateBase
    {
        public PlayerHackState(PlayerController owner) : base(owner) { }

        public override PlayerStateId Id => PlayerStateId.Hack;

        public override void Enter() => Owner.Body.gravityScale = Config.BaseGravityScale;

        public override void FixedTick(float deltaTime) =>
            Owner.ApplyHorizontal(0f, Config.GroundAcceleration, Config.GroundDeceleration, deltaTime);
    }

    /// <summary>Knockback away from the damage source with a short control lock.</summary>
    public sealed class PlayerHurtState : PlayerStateBase
    {
        private DamageInfo _damage;
        private float _timer;

        public PlayerHurtState(PlayerController owner) : base(owner) { }

        public override PlayerStateId Id => PlayerStateId.Hurt;

        public void Configure(in DamageInfo damage) => _damage = damage;

        public override void Enter()
        {
            _timer = Config.HurtControlLock;
            float direction = Mathf.Sign(Owner.Body.position.x - _damage.SourcePosition.x);
            if (Mathf.Approximately(direction, 0f)) direction = -Owner.Facing;
            Owner.Body.gravityScale = Config.BaseGravityScale;
            Owner.Body.linearVelocity = new Vector2(direction * _damage.Knockback, _damage.Knockback * 0.6f);
            Owner.IsJumpRising = false;
        }

        public override void FixedTick(float deltaTime)
        {
            _timer -= deltaTime;
            Owner.ApplyAirGravity();
            if (_timer <= 0f) Owner.ResolveDefaultState();
        }
    }

    /// <summary>No control; the body settles on the ground until a respawn is requested.</summary>
    public sealed class PlayerDeadState : PlayerStateBase
    {
        public PlayerDeadState(PlayerController owner) : base(owner) { }

        public override PlayerStateId Id => PlayerStateId.Dead;

        public override void Enter()
        {
            Owner.Body.gravityScale = Config.BaseGravityScale;
            Owner.Body.linearVelocity = new Vector2(0f, Mathf.Max(0f, Owner.Body.linearVelocity.y));
        }

        public override void FixedTick(float deltaTime) =>
            Owner.ApplyHorizontal(0f, Config.GroundDeceleration, Config.GroundDeceleration, deltaTime);
    }

    /// <summary>Physics suspended while a network node moves the player.</summary>
    public sealed class PlayerTeleportState : PlayerStateBase
    {
        public PlayerTeleportState(PlayerController owner) : base(owner) { }

        public override PlayerStateId Id => PlayerStateId.Teleport;

        public override void Enter()
        {
            Owner.Body.linearVelocity = Vector2.zero;
            Owner.Body.simulated = false;
        }

        public override void Exit() => Owner.Body.simulated = true;
    }
}
