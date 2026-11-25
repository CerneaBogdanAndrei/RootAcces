using System.Collections.Generic;
using System.Text;
using RootAccess.Core;
using RootAccess.Input;
using RootAccess.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RootAccess.UI
{
    /// <summary>
    /// Full-screen personal terminal with tabs: inventory, skill tree (three branches), quest log and, when opened from a
    /// hub console, the shop, the darknet contracts or fast travel (levels unlocked by finishing the previous one). Time stops while it is open (<see cref="GameState.Terminal"/>).
    /// Lists are rebuilt from a button template; the right-hand panel shows details and the tab's action.
    /// </summary>
    public sealed class TerminalScreenView : UIPanel
    {
        [SerializeField] private InputReader input;
        [SerializeField] private Button[] tabButtons = new Button[0];
        [SerializeField] private TMP_Text headerLabel;
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private Button entryTemplate;
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailBody;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Color normalTextColor = new(0.9f, 0.94f, 1f);
        [SerializeField] private Color unlockedColor = new(0.35f, 1f, 0.6f);
        [SerializeField] private Color availableColor = new(0.3f, 0.95f, 1f);
        [SerializeField] private Color lockedColor = new(0.5f, 0.52f, 0.62f);
        [SerializeField] private Color activeTabColor = new(0.3f, 0.95f, 1f);

        private readonly List<Button> _entries = new();
        private readonly List<object> _data = new();
        private readonly StringBuilder _text = new(512);
        private TerminalTab _tab;
        private ShopOffer[] _offers;
        private QuestDefinition[] _contracts;
        private LevelDefinition[] _destinations;
        private bool _allowSkillUnlock;
        private int _selected = -1;

        public TerminalTab CurrentTab => _tab;
        public int EntryCount => _entries.Count;
        public string StatusText => statusLabel != null ? statusLabel.text : string.Empty;
        public bool ActionAvailable => actionButton != null && actionButton.gameObject.activeSelf && actionButton.interactable;

        /// <summary>Test hook: the data behind each listed entry (slots, skills, quests, offers, contracts).</summary>
        internal IReadOnlyList<object> EntryData => _data;

        protected override void Awake()
        {
            base.Awake();
            if (entryTemplate != null) entryTemplate.gameObject.SetActive(false);
            for (int i = 0; i < tabButtons.Length; i++)
            {
                var tab = (TerminalTab)i;
                if (tabButtons[i] != null) tabButtons[i].onClick.AddListener(() => SelectTab(tab));
            }

            if (actionButton != null) actionButton.onClick.AddListener(RunAction);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            EventBus<TerminalScreenRequested>.Subscribe(OnRequested);
            EventBus<GameStateChanged>.Subscribe(OnGameState);
            EventBus<InventoryChanged>.Subscribe(OnDataChanged);
            EventBus<CreditsChanged>.Subscribe(OnDataChanged);
            EventBus<XpChanged>.Subscribe(OnDataChanged);
            EventBus<QuestChanged>.Subscribe(OnDataChanged);
            CommandLineAutomation.CommandRequested += OnAutomationCommand;
            if (input == null) return;
            input.InventoryEvent += OnInventoryKey;
            input.UICancelEvent += OnCancel;
        }

        private void OnDisable()
        {
            EventBus<TerminalScreenRequested>.Unsubscribe(OnRequested);
            EventBus<GameStateChanged>.Unsubscribe(OnGameState);
            EventBus<InventoryChanged>.Unsubscribe(OnDataChanged);
            EventBus<CreditsChanged>.Unsubscribe(OnDataChanged);
            EventBus<XpChanged>.Unsubscribe(OnDataChanged);
            EventBus<QuestChanged>.Unsubscribe(OnDataChanged);
            CommandLineAutomation.CommandRequested -= OnAutomationCommand;
            if (input == null) return;
            input.InventoryEvent -= OnInventoryKey;
            input.UICancelEvent -= OnCancel;
        }

        // ---------------------------------------------------------------- open / close

        public bool Open(in TerminalScreenRequested request)
        {
            if (!ServiceLocator.TryGet(out GameManager gameManager) || !gameManager.EnterTerminal()) return false;
            _offers = request.Offers;
            _contracts = request.Contracts;
            _destinations = request.Destinations;
            _allowSkillUnlock = request.AllowSkillUnlock;
            SetTabVisible(TerminalTab.Shop, _offers is { Length: > 0 });
            SetTabVisible(TerminalTab.Contracts, _contracts is { Length: > 0 });
            SetTabVisible(TerminalTab.Travel, _destinations is { Length: > 0 });
            if (statusLabel != null) statusLabel.text = string.Empty;
            Show();
            SelectTab(request.Tab);
            return true;
        }

        public void Close()
        {
            if (!IsVisible) return;
            Hide();
            _offers = null;
            _contracts = null;
            _destinations = null;
            _allowSkillUnlock = false;
            if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.ExitTerminal();
        }

        public void SelectTab(TerminalTab tab)
        {
            if (!IsTabAvailable(tab)) tab = TerminalTab.Inventory;
            _tab = tab;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                TMP_Text label = tabButtons[i].GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.color = i == (int)tab ? activeTabColor : normalTextColor;
            }

            Rebuild(keepSelection: false);
        }

        /// <summary>Selects the entry at <paramref name="index"/> (also what clicking it does).</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= _entries.Count) return;
            _selected = index;
            RenderDetails();
        }

        /// <summary>Runs the current tab's action (buy, unlock, accept) on the selected entry.</summary>
        public void RunAction()
        {
            if (_selected < 0 || _selected >= _data.Count || !ServiceLocator.TryGet(out SessionService session)) return;
            switch (_tab)
            {
                case TerminalTab.Skills when _data[_selected] is SkillDefinition skill && _allowSkillUnlock:
                    SkillUnlockResult unlock = session.Session.Skills.TryUnlock(skill);
                    SetStatus(unlock == SkillUnlockResult.Success ? string.Format(Strings.Hub.SkillUnlocked, skill.DisplayName) : SkillStateText(unlock));
                    break;
                case TerminalTab.Shop when _data[_selected] is ShopOffer offer:
                    PurchaseResult purchase = session.Buy(offer);
                    SetStatus(purchase switch
                    {
                        PurchaseResult.Success => string.Format(Strings.Hub.Bought, offer.Item.DisplayName, offer.Price),
                        PurchaseResult.NotEnoughCredits => string.Format(Strings.Hub.TooExpensive, session.Session.Wallet.Credits, offer.Price),
                        PurchaseResult.InventoryFull => Strings.Hud.InventoryFull,
                        _ => string.Empty
                    });
                    break;
                case TerminalTab.Contracts when _data[_selected] is QuestDefinition contract:
                    if (session.Session.Quests.Get(contract.QuestId) != null) break;
                    session.StartQuest(contract);
                    SetStatus(string.Format(Strings.Hub.ContractAccepted, contract.Title));
                    break;
                case TerminalTab.Travel when _data[_selected] is LevelDefinition level:
                    if (!CanTravelTo(session.Session, level)) break;
                    Travel(session, level);
                    return;
            }

            Rebuild(keepSelection: true);
        }

        // ---------------------------------------------------------------- events

        private void OnRequested(TerminalScreenRequested request) => Open(request);

        private void OnInventoryKey()
        {
            if (IsVisible) Close();
            else Open(new TerminalScreenRequested(TerminalTab.Inventory));
        }

        private void OnAutomationCommand(string command)
        {
            if (command == AutomationCommands.Inventory) Open(new TerminalScreenRequested(TerminalTab.Inventory));
            else if (command == AutomationCommands.Skills) Open(new TerminalScreenRequested(TerminalTab.Skills));
            else if (command == AutomationCommands.Quests) Open(new TerminalScreenRequested(TerminalTab.Quests));
        }

        private void OnCancel()
        {
            if (IsVisible) Close();
        }

        private void OnGameState(GameStateChanged evt)
        {
            if (IsVisible && evt.Current != GameState.Terminal) Hide();
        }

        private void OnDataChanged<T>(T _) where T : struct, IEvent
        {
            if (IsVisible) Rebuild(keepSelection: true);
        }

        // ---------------------------------------------------------------- lists

        private void Rebuild(bool keepSelection)
        {
            int previous = keepSelection ? _selected : 0;
            ClearEntries();
            if (!ServiceLocator.TryGet(out SessionService session)) return;
            GameSession game = session.Session;
            if (headerLabel != null) headerLabel.text = string.Format(Strings.Panels.Header, game.Wallet.Credits, game.Skills.Xp);

            switch (_tab)
            {
                case TerminalTab.Inventory:
                    foreach (InventorySlot slot in game.Inventory.Slots)
                        if (!slot.IsEmpty) AddEntry(slot, $"{slot.Item.DisplayName}  ×{slot.Quantity}", normalTextColor);
                    break;
                case TerminalTab.Skills:
                    foreach (SkillDefinition skill in SortedSkills(session.Database.Skills))
                        AddEntry(skill, $"[{BranchName(skill.Branch)}] {skill.DisplayName} · {skill.XpCost} XP", SkillColor(game.Skills.CanUnlock(skill)));
                    break;
                case TerminalTab.Quests:
                    foreach (QuestProgress quest in game.Quests.All)
                        AddEntry(quest, $"{quest.Definition.Title} ({QuestStatusText(quest.Status)})",
                            quest.Status == QuestStatus.Completed ? unlockedColor : normalTextColor);
                    break;
                case TerminalTab.Shop:
                    foreach (ShopOffer offer in _offers ?? new ShopOffer[0])
                        if (offer.IsValid)
                            AddEntry(offer, $"{offer.Item.DisplayName} · {offer.Price} cr",
                                Shop.CanBuy(game.Wallet, game.Inventory, offer) == PurchaseResult.Success ? availableColor : lockedColor);
                    break;
                case TerminalTab.Travel:
                    foreach (LevelDefinition level in SortedLevels(_destinations))
                        AddEntry(level, $"{level.Order}. {level.DisplayName} ({LevelStateText(game, level)})",
                            CanTravelTo(game, level) ? availableColor : IsCurrentScene(level) ? unlockedColor : lockedColor);
                    break;
                case TerminalTab.Contracts:
                    foreach (QuestDefinition contract in _contracts ?? new QuestDefinition[0])
                    {
                        if (contract == null) continue;
                        QuestProgress progress = game.Quests.Get(contract.QuestId);
                        string state = progress == null ? Strings.Panels.ContractAvailable : QuestStatusText(progress.Status);
                        AddEntry(contract, $"{contract.Title} ({state})", progress == null ? availableColor : lockedColor);
                    }

                    break;
            }

            if (_entries.Count == 0)
            {
                _selected = -1;
                if (detailTitle != null) detailTitle.text = string.Empty;
                if (detailBody != null) detailBody.text = Strings.Panels.Empty;
                SetAction(false, string.Empty, false);
                return;
            }

            _selected = Mathf.Clamp(previous, 0, _entries.Count - 1);
            RenderDetails();
            if (!keepSelection && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_entries[_selected].gameObject);
        }

        private void AddEntry(object data, string label, Color color)
        {
            if (entryTemplate == null || listRoot == null) return;
            Button entry = Instantiate(entryTemplate, listRoot);
            entry.gameObject.SetActive(true);
            int index = _entries.Count;
            entry.onClick.AddListener(() => Select(index));
            TMP_Text text = entry.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
                text.color = color;
            }

            _entries.Add(entry);
            _data.Add(data);
        }

        private void ClearEntries()
        {
            foreach (Button entry in _entries)
                if (entry != null) Destroy(entry.gameObject);
            _entries.Clear();
            _data.Clear();
        }

        // ---------------------------------------------------------------- details

        private void RenderDetails()
        {
            if (_selected < 0 || _selected >= _data.Count || !ServiceLocator.TryGet(out SessionService session)) return;
            GameSession game = session.Session;
            _text.Clear();
            string title = string.Empty;
            switch (_data[_selected])
            {
                case InventorySlot slot:
                    title = slot.Item.DisplayName;
                    _text.AppendLine(slot.Item.Description).AppendLine()
                        .AppendLine(string.Format(Strings.Panels.Quantity, slot.Quantity))
                        .Append(string.Format(Strings.Panels.Value, slot.Item.CreditValue));
                    SetAction(false, string.Empty, false);
                    break;
                case SkillDefinition skill:
                    title = skill.DisplayName;
                    SkillUnlockResult state = game.Skills.CanUnlock(skill);
                    _text.AppendLine(skill.Description).AppendLine()
                        .AppendLine($"{BranchName(skill.Branch)} · {string.Format(Strings.Panels.Cost, skill.XpCost)}");
                    if (skill.Prerequisites.Length > 0) _text.AppendLine(string.Format(Strings.Panels.Requires, PrerequisiteNames(skill)));
                    _text.Append(SkillStateText(state));
                    if (!_allowSkillUnlock && state != SkillUnlockResult.AlreadyUnlocked) _text.AppendLine().AppendLine().Append(Strings.Panels.SkillOnlyAtTerminal);
                    SetAction(_allowSkillUnlock, Strings.Panels.Unlock, state == SkillUnlockResult.Success);
                    break;
                case QuestProgress quest:
                    title = quest.Definition.Title;
                    AppendQuest(quest.Definition, quest);
                    SetAction(false, string.Empty, false);
                    break;
                case ShopOffer offer:
                    title = offer.Item.DisplayName;
                    _text.AppendLine(offer.Item.Description).AppendLine()
                        .AppendLine(string.Format(Strings.Panels.Price, offer.Price))
                        .Append(string.Format(Strings.Panels.Owned, game.Inventory.Count(offer.Item)));
                    SetAction(true, Strings.Panels.Buy, Shop.CanBuy(game.Wallet, game.Inventory, offer) == PurchaseResult.Success);
                    break;
                case QuestDefinition contract:
                    title = contract.Title;
                    QuestProgress progress = game.Quests.Get(contract.QuestId);
                    AppendQuest(contract, progress);
                    SetAction(true, Strings.Panels.Accept, progress == null);
                    break;
                case LevelDefinition level:
                    title = level.DisplayName;
                    if (!string.IsNullOrEmpty(level.Description)) _text.AppendLine(level.Description).AppendLine();
                    _text.Append(IsCurrentScene(level) ? Strings.Panels.TravelHereHint
                        : IsUnlocked(game, level) ? Strings.Panels.TravelReadyHint
                        : Strings.Panels.TravelLockedHint);
                    SetAction(true, Strings.Panels.TravelAction, CanTravelTo(game, level));
                    break;
            }

            if (detailTitle != null) detailTitle.text = title;
            if (detailBody != null) detailBody.text = _text.ToString();
        }

        private void AppendQuest(QuestDefinition definition, QuestProgress progress)
        {
            _text.AppendLine(definition.Description).AppendLine();
            for (int i = 0; i < definition.Objectives.Length; i++)
            {
                ObjectiveDefinition objective = definition.Objectives[i];
                ObjectiveProgress state = progress != null && i < progress.Objectives.Count ? progress.Objectives[i] : null;
                string marker = state == null ? Strings.Panels.ObjectiveOpen
                    : state.Failed ? Strings.Panels.ObjectiveFailed
                    : state.IsComplete || (objective.Type == ObjectiveType.AvoidDetection && progress.Status == QuestStatus.Completed) ? Strings.Panels.ObjectiveDone
                    : Strings.Panels.ObjectiveOpen;
                _text.Append(marker).Append(objective.Description);
                if (objective.Type != ObjectiveType.AvoidDetection)
                    _text.Append(" (").Append(state?.Count ?? 0).Append('/').Append(objective.RequiredCount).Append(')');
                if (objective.Optional) _text.Append(Strings.Panels.Optional);
                _text.AppendLine();
            }

            _text.AppendLine().Append(string.Format(Strings.Panels.Rewards, definition.RewardCredits, definition.RewardXp));
        }

        private void SetAction(bool visible, string label, bool enabled)
        {
            if (actionButton == null) return;
            actionButton.gameObject.SetActive(visible);
            actionButton.interactable = enabled;
            if (actionLabel != null) actionLabel.text = label;
        }

        private void SetStatus(string text)
        {
            if (statusLabel != null) statusLabel.text = text;
        }

        // ---------------------------------------------------------------- helpers

        private bool IsTabAvailable(TerminalTab tab) => tab switch
        {
            TerminalTab.Shop => _offers is { Length: > 0 },
            TerminalTab.Contracts => _contracts is { Length: > 0 },
            TerminalTab.Travel => _destinations is { Length: > 0 },
            _ => true
        };

        // ---------------------------------------------------------------- travel

        private void Travel(SessionService session, LevelDefinition level)
        {
            session.SaveGame();
            EventBus<ToastRequested>.Publish(new ToastRequested(string.Format(Strings.Panels.TravelStarted, level.DisplayName)));
            Close();
            if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.LoadScene(level.SceneName);
        }

        /// <summary>Unlocked by progress and actually shipped: levels whose scene is not in the build stay locked.</summary>
        private static bool IsUnlocked(GameSession game, LevelDefinition level) =>
            (level.UnlockedByDefault || game.IsLevelUnlocked(level.LevelId)) && Application.CanStreamedLevelBeLoaded(level.SceneName);

        private static bool IsCurrentScene(LevelDefinition level) =>
            level.SceneName == UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        private static bool CanTravelTo(GameSession game, LevelDefinition level) => IsUnlocked(game, level) && !IsCurrentScene(level);

        private static string LevelStateText(GameSession game, LevelDefinition level) =>
            IsCurrentScene(level) ? Strings.Panels.LevelHere : IsUnlocked(game, level) ? Strings.Panels.LevelOpen : Strings.Panels.LevelLocked;

        private static List<LevelDefinition> SortedLevels(LevelDefinition[] levels)
        {
            var sorted = new List<LevelDefinition>();
            foreach (LevelDefinition level in levels ?? new LevelDefinition[0])
                if (level != null) sorted.Add(level);
            sorted.Sort((a, b) => a.Order.CompareTo(b.Order));
            return sorted;
        }

        private void SetTabVisible(TerminalTab tab, bool visible)
        {
            int index = (int)tab;
            if (index < tabButtons.Length && tabButtons[index] != null) tabButtons[index].gameObject.SetActive(visible);
        }

        private Color SkillColor(SkillUnlockResult state) => state switch
        {
            SkillUnlockResult.AlreadyUnlocked => unlockedColor,
            SkillUnlockResult.Success => availableColor,
            _ => lockedColor
        };

        private static string SkillStateText(SkillUnlockResult state) => state switch
        {
            SkillUnlockResult.AlreadyUnlocked => Strings.Panels.SkillUnlocked,
            SkillUnlockResult.Success => Strings.Panels.SkillAvailable,
            SkillUnlockResult.MissingPrerequisite => Strings.Panels.SkillMissingPrerequisite,
            SkillUnlockResult.NotEnoughXp => Strings.Panels.SkillNotEnoughXp,
            _ => string.Empty
        };

        private static string QuestStatusText(QuestStatus status) => status switch
        {
            QuestStatus.Completed => Strings.Panels.QuestCompleted,
            QuestStatus.Failed => Strings.Panels.QuestFailed,
            _ => Strings.Panels.QuestActive
        };

        private static string BranchName(SkillBranch branch) => branch switch
        {
            SkillBranch.Stealth => Strings.Branches.Stealth,
            SkillBranch.Hacking => Strings.Branches.Hacking,
            _ => Strings.Branches.Hardware
        };

        private static string PrerequisiteNames(SkillDefinition skill)
        {
            var names = new List<string>(skill.Prerequisites.Length);
            foreach (SkillDefinition prerequisite in skill.Prerequisites)
                if (prerequisite != null) names.Add(prerequisite.DisplayName);
            return string.Join(", ", names);
        }

        private static List<SkillDefinition> SortedSkills(SkillDefinition[] skills)
        {
            var sorted = new List<SkillDefinition>();
            foreach (SkillDefinition skill in skills)
                if (skill != null) sorted.Add(skill);
            sorted.Sort((a, b) => a.Branch != b.Branch ? a.Branch.CompareTo(b.Branch) : a.XpCost.CompareTo(b.XpCost));
            return sorted;
        }
    }
}
