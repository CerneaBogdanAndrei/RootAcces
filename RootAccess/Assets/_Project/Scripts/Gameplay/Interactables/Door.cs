using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Security door opened/closed by hacking. The Animator plays the slide; the blocking collider toggles. In co-op the
    /// Operator can toggle it remotely (<see cref="IOperatorTarget"/>).
    /// </summary>
    public sealed class Door : MonoBehaviour, IHackable, IOperatorTarget
    {
        [SerializeField] private Collider2D blocker;
        [SerializeField] private Animator animator;
        [SerializeField] private bool startOpen;

        public string HackLabel => Strings.HackTargets.Door;
        public bool IsHacked { get; private set; }
        public bool IsOpen { get; private set; }

        public string OperatorId => GetInstanceID().ToString();
        public OperatorTargetKind OperatorKind => OperatorTargetKind.Door;
        public Vector2 OperatorPosition => transform.position;
        public bool OperatorActive => IsOpen;
        public bool CanOperate => isActiveAndEnabled;

        private void Start() => SetOpen(startOpen, silent: true);

        private void OnEnable() => OperatorRegistry.Register(this);

        private void OnDisable() => OperatorRegistry.Unregister(this);

        public void Operate() => SetOpen(!IsOpen);

        public void ApplyHack(HackAction action)
        {
            IsHacked = true;
            switch (action)
            {
                case HackAction.Open:
                case HackAction.Disable:
                case HackAction.Activate:
                    SetOpen(true);
                    break;
                case HackAction.Close:
                    SetOpen(false);
                    break;
                default:
                    SetOpen(!IsOpen);
                    break;
            }
        }

        public void SetOpen(bool open, bool silent = false)
        {
            IsOpen = open;
            if (blocker != null) blocker.enabled = !open;
            if (animator != null) animator.SetBool(AnimatorParams.IsOpen, open);
            if (!silent) EventBus<SfxRequested>.Publish(new SfxRequested(open ? SfxId.DoorOpen : SfxId.DoorClose, transform.position));
        }
    }
}
