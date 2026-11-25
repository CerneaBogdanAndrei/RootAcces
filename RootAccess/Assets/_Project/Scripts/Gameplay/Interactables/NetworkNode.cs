using RootAccess.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RootAccess.Gameplay
{
    /// <summary>Network node linked to another node. Locked nodes must be hacked (via a terminal) before use.</summary>
    public sealed class NetworkNode : Teleporter, IHackable
    {
        [SerializeField] private NetworkNode linkedNode;
        [SerializeField] private bool requiresHack = true;
        [SerializeField] private Light2D glow;
        [SerializeField] private Animator animator;
        [SerializeField] private Color lockedColor = new(1f, 0.3f, 0.4f);
        [SerializeField] private Color unlockedColor = new(0.3f, 0.9f, 1f);

        private bool _unlocked;

        public string HackLabel => Strings.HackTargets.NetworkNode;
        public bool IsHacked => _unlocked;
        public bool IsUnlocked => _unlocked || !requiresHack;
        public NetworkNode LinkedNode => linkedNode;
        public override string Prompt => IsUnlocked ? Strings.Prompts.Teleport : Strings.Prompts.NodeLocked;
        protected override Teleporter Destination => linkedNode;

        private void Start() => Refresh();

        public override bool CanInteract(IInteractor interactor) => IsUnlocked && linkedNode != null && linkedNode.IsUnlocked && base.CanInteract(interactor);

        public void ApplyHack(HackAction action)
        {
            _unlocked = true;
            if (linkedNode != null) linkedNode.UnlockFromLink();
            Refresh();
        }

        private void UnlockFromLink()
        {
            _unlocked = true;
            Refresh();
        }

        private void Refresh()
        {
            if (glow != null) glow.color = IsUnlocked ? unlockedColor : lockedColor;
            if (animator != null) animator.SetBool(AnimatorParams.Active, IsUnlocked);
        }
    }
}
