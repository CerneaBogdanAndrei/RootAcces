using System.Text;
using RootAccess.Core;
using RootAccess.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>
    /// In-game HUD: health, trace meter, credits, the objective list of the active contract (with the next step and the
    /// side contracts), interaction prompt, stealth and toasts. In split-screen co-op a <see cref="SplitScreenRegion"/>
    /// keeps it on the Infiltrator's half.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private const string DoneColor = "#7C86A3";
        private const string CurrentColor = "#2EF2FF";
        private const string PendingColor = "#D7DCEB";
        private const string BonusColor = "#FFD24A";
        private const string FailedColor = "#FF4A5A";
        private const string SideColor = "#A9B2CC";
        private const int MaxSideContracts = 2;

        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text healthLabel;
        [SerializeField] private Image traceFill;
        [SerializeField] private TMP_Text traceLabel;
        [SerializeField] private TMP_Text creditsLabel;
        [SerializeField] private TMP_Text questTitle;
        [SerializeField, Tooltip("Every objective of the active contract, ticked off as they are done.")]
        private TMP_Text questObjective;
        [SerializeField] private TMP_Text promptLabel;
        [SerializeField] private TMP_Text hiddenLabel;
        [SerializeField] private TMP_Text toastLabel;
        [SerializeField] private TMP_Text gadgetLabel;
        [SerializeField] private ItemDefinition trackedGadget;
        [SerializeField] private Gradient traceGradient;
        [SerializeField] private float toastSeconds = 2.2f;
        [SerializeField, Tooltip("Level 6 broadcast progress (hidden unless a broadcast is running).")]
        private GameObject uplinkPanel;
        [SerializeField] private Image uplinkFill;
        [SerializeField] private TMP_Text uplinkLabel;

        private int _credits;
        private int _xp;
        private PlayerHealthChanged? _lastHealth;
        private TraceChanged? _lastTrace;
        private UplinkProgressChanged? _lastUplink;
        private bool _hidden;
        private readonly StringBuilder _text = new();

        private float _toastUntil;
        private float _healthTarget = 1f;
        private float _traceTarget;

        private void Awake()
        {
            SetVisible(false);
            if (promptLabel != null) promptLabel.text = string.Empty;
            if (toastLabel != null) toastLabel.text = string.Empty;
            if (hiddenLabel != null) hiddenLabel.text = string.Empty;
            if (uplinkPanel != null) uplinkPanel.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus<GameStateChanged>.Subscribe(OnGameState);
            EventBus<PlayerHealthChanged>.Subscribe(OnHealth);
            EventBus<TraceChanged>.Subscribe(OnTrace);
            EventBus<CreditsChanged>.Subscribe(OnCredits);
            EventBus<XpChanged>.Subscribe(OnXp);
            EventBus<QuestChanged>.Subscribe(OnQuest);
            EventBus<InteractionPromptChanged>.Subscribe(OnPrompt);
            EventBus<PlayerHiddenChanged>.Subscribe(OnHidden);
            EventBus<ToastRequested>.Subscribe(OnToast);
            EventBus<InventoryChanged>.Subscribe(OnInventory);
            EventBus<UplinkProgressChanged>.Subscribe(OnUplink);
            EventBus<SceneLoadStarted>.Subscribe(OnSceneLoad);
            EventBus<SceneLoadCompleted>.Subscribe(OnSceneLoaded);
            EventBus<LanguageChanged>.Subscribe(OnLanguageChanged);
        }

        private void OnDisable()
        {
            EventBus<GameStateChanged>.Unsubscribe(OnGameState);
            EventBus<PlayerHealthChanged>.Unsubscribe(OnHealth);
            EventBus<TraceChanged>.Unsubscribe(OnTrace);
            EventBus<CreditsChanged>.Unsubscribe(OnCredits);
            EventBus<XpChanged>.Unsubscribe(OnXp);
            EventBus<QuestChanged>.Unsubscribe(OnQuest);
            EventBus<InteractionPromptChanged>.Unsubscribe(OnPrompt);
            EventBus<PlayerHiddenChanged>.Unsubscribe(OnHidden);
            EventBus<ToastRequested>.Unsubscribe(OnToast);
            EventBus<InventoryChanged>.Unsubscribe(OnInventory);
            EventBus<UplinkProgressChanged>.Unsubscribe(OnUplink);
            EventBus<SceneLoadStarted>.Unsubscribe(OnSceneLoad);
            EventBus<SceneLoadCompleted>.Unsubscribe(OnSceneLoaded);
            EventBus<LanguageChanged>.Unsubscribe(OnLanguageChanged);
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

        /// <summary>Redraws every composed text in the new language (switched from the pause menu's settings).</summary>
        private void OnLanguageChanged(LanguageChanged _)
        {
            if (_lastHealth is PlayerHealthChanged health) OnHealth(health);
            if (_lastTrace is TraceChanged trace) OnTrace(trace);
            if (_lastUplink is UplinkProgressChanged uplink) OnUplink(uplink);
            RenderWallet();
            OnQuest(default);
            OnHidden(new PlayerHiddenChanged(_hidden));
            OnInventory(default);
            if (promptLabel != null) promptLabel.text = string.Empty;
        }

        private void OnHealth(PlayerHealthChanged evt)
        {
            _lastHealth = evt;
            _healthTarget = evt.Max > 0 ? (float)evt.Current / evt.Max : 0f;
            if (healthLabel != null) healthLabel.text = $"{Strings.Hud.Health} {evt.Current}/{evt.Max}";
        }

        private void OnTrace(TraceChanged evt)
        {
            _lastTrace = evt;
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

        private void OnXp(XpChanged evt)
        {
            _xp = evt.Xp;
            RenderWallet();
        }

        private void RenderWallet()
        {
            if (creditsLabel != null) creditsLabel.text = string.Format(Strings.Hud.CreditsAndXp, _credits, _xp);
        }

        /// <summary>The story contract stays on screen; a side contract is shown only while no story contract runs.</summary>
        private void OnQuest(QuestChanged evt)
        {
            ServiceLocator.TryGet(out SessionService session);
            QuestProgress quest = session != null && session.Session != null ? session.Session.Quests.ActiveMainQuest : null;
            if (quest == null && evt.Quest is { Status: QuestStatus.Active }) quest = evt.Quest;
            RenderQuest(quest, session);
        }

        /// <summary>The next step depends on where the player is (the travel hint only makes sense in the hideout).</summary>
        private void OnSceneLoaded(SceneLoadCompleted _) => OnQuest(default);

        private void RenderQuest(QuestProgress quest, SessionService session)
        {
            if (questTitle != null) questTitle.text = quest != null ? quest.Definition.Title : Strings.Hud.NoActiveQuest;
            if (questObjective == null) return;
            _text.Clear();
            foreach (ChecklistLine line in ObjectiveChecklist.For(quest)) AppendObjective(line);
            AppendTravelHint(quest, session);
            AppendSideContracts(quest, session);
            questObjective.text = _text.ToString();
        }

        private void AppendObjective(ChecklistLine line)
        {
            string counter = line.ShowsCounter ? $" ({line.Count}/{line.Required})" : string.Empty;
            switch (line.Mark)
            {
                case ChecklistMark.Done:
                    AppendLine(DoneColor, $"<s>{Strings.Panels.ObjectiveDone}{line.Text}</s>");
                    break;
                case ChecklistMark.Current:
                    AppendLine(CurrentColor, $"{Strings.Hud.ObjectiveCurrent}{line.Text}{counter}");
                    break;
                case ChecklistMark.Bonus:
                    AppendLine(BonusColor, $"{Strings.Panels.ObjectiveOpen}{line.Text}{counter}");
                    break;
                case ChecklistMark.Failed:
                    AppendLine(FailedColor, $"{Strings.Panels.ObjectiveFailed}{line.Text}");
                    break;
                default:
                    AppendLine(PendingColor, $"{Strings.Panels.ObjectiveOpen}{line.Text}{counter}");
                    break;
            }
        }

        /// <summary>In the hideout, "reach level X" means: take the travel terminal there.</summary>
        private void AppendTravelHint(QuestProgress quest, SessionService session)
        {
            ObjectiveProgress next = quest?.FirstIncomplete();
            if (next == null || next.Definition.Type != ObjectiveType.Reach || session == null || session.Database == null) return;
            if (SceneManager.GetActiveScene().name != SceneNames.Hub) return;
            foreach (LevelDefinition level in session.Database.Levels)
            {
                if (level == null || level.LevelId != next.Definition.TargetId) continue;
                AppendLine(BonusColor, string.Format(Strings.Hud.TravelHint, level.DisplayName));
                return;
            }
        }

        /// <summary>Other running contracts, one line each: what to do next for them.</summary>
        private void AppendSideContracts(QuestProgress shown, SessionService session)
        {
            if (session == null || session.Session == null) return;
            int listed = 0;
            foreach (QuestProgress other in session.Session.Quests.All)
            {
                if (listed >= MaxSideContracts) break;
                if (other == shown || other.Status != QuestStatus.Active) continue;
                ObjectiveProgress next = other.FirstIncomplete();
                if (next == null) continue;
                string counter = next.Definition.RequiredCount > 1 ? $" ({next.Count}/{next.Definition.RequiredCount})" : string.Empty;
                AppendLine(SideColor, $"<size=85%>{string.Format(Strings.Hud.SideContract, other.Definition.Title, next.Definition.Description + counter)}</size>");
                listed++;
            }
        }

        private void AppendLine(string color, string body)
        {
            if (_text.Length > 0) _text.Append('\n');
            _text.Append("<color=").Append(color).Append('>').Append(body).Append("</color>");
        }

        private void OnPrompt(InteractionPromptChanged evt)
        {
            if (promptLabel != null) promptLabel.text = evt.Visible ? evt.Prompt : string.Empty;
        }

        private void OnHidden(PlayerHiddenChanged evt)
        {
            _hidden = evt.IsHidden;
            if (hiddenLabel != null) hiddenLabel.text = evt.IsHidden ? Strings.Hud.Hidden : string.Empty;
        }

        private void OnInventory(InventoryChanged _)
        {
            if (gadgetLabel == null || trackedGadget == null || !ServiceLocator.TryGet(out SessionService session)) return;
            gadgetLabel.text = string.Format(Strings.Hud.EmpCount, session.Session.Inventory.Count(trackedGadget));
        }

        private void OnUplink(UplinkProgressChanged evt)
        {
            _lastUplink = evt;
            if (uplinkPanel == null) return;
            uplinkPanel.SetActive(evt.Active);
            if (!evt.Active) return;
            int percent = Mathf.FloorToInt(evt.Progress * 100f);
            if (uplinkFill != null)
            {
                uplinkFill.fillAmount = evt.Progress;
                uplinkFill.color = evt.PlayerPresent ? new Color(0.3f, 1f, 0.75f) : new Color(1f, 0.3f, 0.35f);
            }

            if (uplinkLabel != null) uplinkLabel.text = string.Format(evt.PlayerPresent ? Strings.Hud.Uplink : Strings.Hud.UplinkPaused, percent);
        }

        private void OnSceneLoad(SceneLoadStarted _)
        {
            _lastUplink = null;
            if (uplinkPanel != null) uplinkPanel.SetActive(false);
        }

        private void OnToast(ToastRequested evt)
        {
            if (toastLabel == null) return;
            toastLabel.text = evt.Message;
            _toastUntil = Time.unscaledTime + toastSeconds;
        }
    }
}
