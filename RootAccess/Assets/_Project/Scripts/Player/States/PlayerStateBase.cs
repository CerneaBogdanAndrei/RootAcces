using RootAccess.Core;

namespace RootAccess.Player
{
    /// <summary>Base for player movement states; exposes the controller and a stable id for animation/UI.</summary>
    public abstract class PlayerStateBase : StateBase<PlayerController>
    {
        protected PlayerStateBase(PlayerController owner) : base(owner) { }

        public abstract PlayerStateId Id { get; }

        protected PlayerMovementConfig Config => Owner.Config;

        /// <summary>Shared transition: buffered dash request with cooldown and one air dash per jump.</summary>
        protected bool TryStartDash()
        {
            if (Owner.DashBufferTimer <= 0f || !Owner.CanDash) return false;
            Owner.ChangeState(PlayerStateId.Dash);
            return true;
        }
    }
}
