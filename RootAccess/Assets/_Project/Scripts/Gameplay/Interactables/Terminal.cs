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
    public sealed class Terminal : MonoBehaviour, IInteractable, IRemoteInteractable
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

        private const float RetryDelaySeconds = 0.35f;

        private int _pendingRequest = -1;
        private bool _extraAttemptUsed;
        private float _retryAt = -1f;

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

        public bool CanInteract(IInteractor interactor) => !IsHacked && _pendingRequest < 0 && _retryAt < 0f;

        public void Interact(IInteractor interactor)
        {
            if (!CanInteract(interactor)) return;
            _extraAttemptUsed = false;
            RequestHack();
        }

        private void Update()
        {
            if (_retryAt < 0f || Time.unscaledTime < _retryAt) return;
            _retryAt = -1f;
            RequestHack();
        }

        /// <summary>"Overclock" skill: every terminal is one difficulty step easier (minimum 1).</summary>
        private void RequestHack()
        {
            int effectiveDifficulty = HasSkill(SkillEffect.FasterHacking) ? Mathf.Max(1, difficulty - 1) : difficulty;
            _pendingRequest = ++_nextRequestId;
            EventBus<SfxRequested>.Publish(new SfxRequested(SfxId.HackBeep, transform.position));
            EventBus<HackRequested>.Publish(new HackRequested(_pendingRequest, minigame, effectiveDifficulty, transform.position));
        }

        private static bool HasSkill(SkillEffect effect) =>
            ServiceLocator.TryGet(out SessionService session) && session.HasSkillEffect(effect);

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
            if (!_extraAttemptUsed && HasSkill(SkillEffect.ExtraHackAttempt))
            {
                // The overlay is still closing: retry a moment later so the new session starts from a clean state.
                _extraAttemptUsed = true;
                _retryAt = Time.unscaledTime + RetryDelaySeconds;
                EventBus<ToastRequested>.Publish(new ToastRequested(Strings.Hud.ExtraAttempt));
                return;
            }

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
