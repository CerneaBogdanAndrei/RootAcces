using RootAccess.Core;
using RootAccess.Systems;
using RootAccess.VFX;
using UnityEngine;

namespace RootAccess.Gameplay
{
    public enum TerminalKind
    {
        Door,
        Camera,
        Data,
        Generic
    }

    /// <summary>
    /// Hackable terminal. Interacting raises a <see cref="HackRequested"/> for the hacking overlay; on success every
    /// linked <see cref="IHackable"/> receives the configured action, quests are notified and rewards granted.
    /// A failed hack adds trace. Variants (Terminal_Door / _Camera / _Data) only differ in serialized data.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Terminal : MonoBehaviour, IInteractable
    {
        private static int _nextRequestId;

        [SerializeField] private string terminalId = "terminal";
        [SerializeField] private TerminalKind kind = TerminalKind.Generic;
        [SerializeField] private HackingMinigameType minigame = HackingMinigameType.PatternMemory;
        [SerializeField, Range(1, 5)] private int difficulty = 1;
        [SerializeField] private MonoBehaviour[] targets = new MonoBehaviour[0];
        [SerializeField] private HackAction actionOnSuccess = HackAction.Open;
        [SerializeField] private float failTrace = 15f;
        [SerializeField, Min(0)] private int rewardCredits;
        [SerializeField, Min(0)] private int rewardXp = 25;
        [SerializeField] private ItemDefinition rewardItem;
        [SerializeField] private SpriteRenderer screen;
        [SerializeField] private Color lockedColor = new(1f, 0.25f, 0.35f);
        [SerializeField] private Color hackedColor = new(0.2f, 1f, 0.6f);
        [SerializeField] private DissolveController glitch;

        private int _pendingRequest = -1;

        public string TerminalId => terminalId;
        public TerminalKind Kind => kind;
        public bool IsHacked { get; private set; }
        public string Prompt => IsHacked ? Strings.Prompts.AlreadyHacked : Strings.Prompts.Hack;
        public Vector3 InteractionPosition => transform.position;

        private void Awake() => GetComponent<Collider2D>().isTrigger = true;

        private void Start()
        {
            if (ServiceLocator.TryGet(out SessionService session) && session.Session.IsTerminalHacked(terminalId))
                MarkHacked(applyTargets: true, announce: false);
            else
                UpdateScreen();
        }

        private void OnEnable() => EventBus<HackResolved>.Subscribe(OnHackResolved);

        private void OnDisable() => EventBus<HackResolved>.Unsubscribe(OnHackResolved);

        private void OnValidate()
        {
            for (int i = 0; i < targets.Length; i++)
                if (targets[i] != null && targets[i] is not IHackable)
                {
                    Debug.LogWarning($"[Terminal] '{name}': target {targets[i].name} does not implement IHackable and was removed.", this);
                    targets[i] = null;
                }
        }

        public bool CanInteract(IInteractor interactor) => !IsHacked && _pendingRequest < 0;

        public void Interact(IInteractor interactor)
        {
            if (!CanInteract(interactor)) return;
            _pendingRequest = ++_nextRequestId;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.HackBeep, transform.position));
            EventBus<HackRequested>.Publish(new HackRequested(_pendingRequest, minigame, difficulty, transform.position));
        }

        private void OnHackResolved(HackResolved evt)
        {
            if (evt.RequestId != _pendingRequest) return;
            _pendingRequest = -1;

            if (evt.Success)
            {
                MarkHacked(applyTargets: true, announce: true);
                return;
            }

            if (evt.Aborted) return;
            EventBus<TraceNoise>.Publish(new TraceNoise(failTrace, TraceSource.FailedHack, transform.position));
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.HackFail, transform.position));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.Sparks, transform.position));
        }

        private void MarkHacked(bool applyTargets, bool announce)
        {
            IsHacked = true;
            UpdateScreen();
            if (applyTargets)
                foreach (MonoBehaviour target in targets)
                    if (target is IHackable hackable) hackable.ApplyHack(actionOnSuccess);

            if (!announce) return;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.HackSuccess, transform.position));
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.DataStream, transform.position));
            EventBus<ObjectiveReported>.Publish(new ObjectiveReported(ObjectiveType.Hack, terminalId));
            if (glitch != null) glitch.DissolveOut(() => glitch.DissolveIn());

            if (!ServiceLocator.TryGet(out SessionService session)) return;
            session.Session.MarkTerminalHacked(terminalId);
            session.Session.Wallet.Add(rewardCredits);
            session.Session.Skills.AddXp(rewardXp);
            if (rewardItem != null) session.TryAddItem(rewardItem);
        }

        private void UpdateScreen()
        {
            if (screen != null) screen.color = IsHacked ? hackedColor : lockedColor;
        }
    }
}
