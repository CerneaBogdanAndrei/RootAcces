using RootAccess.Core;
using RootAccess.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>In-game HUD: health, trace meter, credits, active contract, interaction prompt, stealth and toasts.</summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text healthLabel;
        [SerializeField] private Image traceFill;
        [SerializeField] private TMP_Text traceLabel;
        [SerializeField] private TMP_Text creditsLabel;
        [SerializeField] private TMP_Text questTitle;
        [SerializeField] private TMP_Text questObjective;
        [SerializeField] private TMP_Text promptLabel;
        [SerializeField] private TMP_Text hiddenLabel;
        [SerializeField] private TMP_Text toastLabel;
        [SerializeField] private TMP_Text gadgetLabel;
        [SerializeField] private ItemDefinition trackedGadget;
        [SerializeField] private Gradient traceGradient;
        [SerializeField] private float toastSeconds = 2.2f;

        private int _credits;
        private int _xp;

        private float _toastUntil;
        private float _healthTarget = 1f;
        private float _traceTarget;

        private void Awake()
        {
            SetVisible(false);
            if (promptLabel != null) promptLabel.text = string.Empty;
            if (toastLabel != null) toastLabel.text = string.Empty;
            if (hiddenLabel != null) hiddenLabel.text = string.Empty;
        }

        private void OnEnable()
        {
            EventBus<GameStateChanged>.Subscribe(OnGameState);
            EventBus<PlayerHealthChanged>.Subscribe(OnHealth);
            EventBus<TraceChanged>.Subscribe(OnTrace);
            EventBus<CreditsChanged>.Subscribe(OnCredits);
            EventBus<XpChanged>.Subscribe(OnXp);
            EventBus<CoopViewChanged>.Subscribe(OnCoopView);
            EventBus<QuestChanged>.Subscribe(OnQuest);
            EventBus<InteractionPromptChanged>.Subscribe(OnPrompt);
            EventBus<PlayerHiddenChanged>.Subscribe(OnHidden);
            EventBus<ToastRequested>.Subscribe(OnToast);
            EventBus<InventoryChanged>.Subscribe(OnInventory);
        }

        private void OnDisable()
        {
            EventBus<GameStateChanged>.Unsubscribe(OnGameState);
            EventBus<PlayerHealthChanged>.Unsubscribe(OnHealth);
            EventBus<TraceChanged>.Unsubscribe(OnTrace);
            EventBus<CreditsChanged>.Unsubscribe(OnCredits);
            EventBus<XpChanged>.Unsubscribe(OnXp);
            EventBus<CoopViewChanged>.Unsubscribe(OnCoopView);
            EventBus<QuestChanged>.Unsubscribe(OnQuest);
            EventBus<InteractionPromptChanged>.Unsubscribe(OnPrompt);
            EventBus<PlayerHiddenChanged>.Unsubscribe(OnHidden);
            EventBus<ToastRequested>.Unsubscribe(OnToast);
            EventBus<InventoryChanged>.Unsubscribe(OnInventory);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (healthFill != null) healthFill.fillAmount = Mathf.MoveTowards(healthFill.fillAmount, _healthTarget, dt * 2f);
            if (traceFill != null)
            {
                traceFill.fillAmount = Mathf.MoveTowards(traceFill.fillAmount, _traceTarget, dt * 1.5f);
                if (traceGradient != null) traceFill.color = traceGradient.Evaluate(traceFill.fillAmount);
            }

            if (toastLabel != null && _toastUntil > 0f && Time.unscaledTime > _toastUntil)
            {
                _toastUntil = 0f;
                toastLabel.text = string.Empty;
            }
        }

        private void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void OnGameState(GameStateChanged evt) =>
            SetVisible(evt.Current is GameState.Playing or GameState.Hacking or GameState.Dialogue or GameState.Paused or GameState.Terminal);

        private void OnHealth(PlayerHealthChanged evt)
        {
            _healthTarget = evt.Max > 0 ? (float)evt.Current / evt.Max : 0f;
            if (healthLabel != null) healthLabel.text = $"{Strings.Hud.Health} {evt.Current}/{evt.Max}";
        }

        private void OnTrace(TraceChanged evt)
        {
            _traceTarget = evt.Value / TraceMeter.Max;
            if (traceLabel == null) return;
            traceLabel.text = evt.Level == TraceLevel.Lockdown
                ? Strings.Hud.Lockdown
                : $"{Strings.Hud.Trace} {Mathf.RoundToInt(evt.Value)}%";
        }

        private void OnCredits(CreditsChanged evt)
        {
            _credits = evt.Credits;
            RenderWallet();
        }

        /// <summary>Split-screen co-op: the HUD follows the gameplay camera onto the left half of the screen.</summary>
        private void OnCoopView(CoopViewChanged evt)
        {
            var rect = (RectTransform)transform;
            rect.anchorMax = new Vector2(evt.Split ? 0.5f : 1f, 1f);
        }

        private void OnXp(XpChanged evt)
        {
            _xp = evt.Xp;
            RenderWallet();
        }

        private void RenderWallet()
        {
            if (creditsLabel != null) creditsLabel.text = string.Format(Strings.Hud.CreditsAndXp, _credits, _xp);
        }

        private void OnQuest(QuestChanged evt)
        {
            QuestProgress quest = evt.Quest;
            if (quest == null || quest.Status != QuestStatus.Active)
            {
                if (ServiceLocator.TryGet(out SessionService session)) quest = session.Session.Quests.ActiveMainQuest;
            }

            if (questTitle != null) questTitle.text = quest != null ? quest.Definition.Title : Strings.Hud.NoActiveQuest;
            if (questObjective == null) return;
            ObjectiveProgress objective = quest?.FirstIncomplete();
            questObjective.text = objective != null
                ? $"> {objective.Definition.Description} ({objective.Count}/{objective.Definition.RequiredCount})"
                : string.Empty;
        }

        private void OnPrompt(InteractionPromptChanged evt)
        {
            if (promptLabel != null) promptLabel.text = evt.Visible ? evt.Prompt : string.Empty;
        }

        private void OnHidden(PlayerHiddenChanged evt)
        {
            if (hiddenLabel != null) hiddenLabel.text = evt.IsHidden ? Strings.Hud.Hidden : string.Empty;
        }

        private void OnInventory(InventoryChanged _)
        {
            if (gadgetLabel == null || trackedGadget == null || !ServiceLocator.TryGet(out SessionService session)) return;
            gadgetLabel.text = string.Format(Strings.Hud.EmpCount, session.Session.Inventory.Count(trackedGadget));
        }

        private void OnToast(ToastRequested evt)
        {
            if (toastLabel == null) return;
            toastLabel.text = evt.Message;
            _toastUntil = Time.unscaledTime + toastSeconds;
        }
    }
}
