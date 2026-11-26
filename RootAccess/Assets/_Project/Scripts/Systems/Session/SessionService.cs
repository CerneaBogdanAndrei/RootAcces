using RootAccess.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RootAccess.Systems
{
    /// <summary>
    /// MonoBehaviour host for the <see cref="GameSession"/> model: ticks the trace meter, routes gameplay events
    /// into the models, mirrors model changes onto the EventBus for the UI, and handles new game / continue / save.
    /// </summary>
    public sealed class SessionService : MonoBehaviour, ISessionService
    {
        [SerializeField] private GameDatabase database;
        [SerializeField, Tooltip("New Game opens the intro cutscene, which then loads the hideout.")]
        private string newGameScene = SceneNames.Intro;

        /// <summary>"Optical camouflage" skill: detection noise raises the trace meter this much slower.</summary>
        private const float CamouflageTraceMultiplier = 0.7f;

        /// <summary>Reward for each data log found (exploration pays a little, the archivist contract pays the rest).</summary>
        private const int LoreCreditReward = 25;
        private const int LoreXpReward = 20;

        private SaveSystem _saveSystem;
        private GameManager _gameManager;
        private float _levelStartedAt;

        public GameSession Session { get; private set; }
        public GameDatabase Database => database;
        public bool HasSave => _saveSystem != null && _saveSystem.Exists(SaveSlots.Main);

        /// <summary>Play time spent in the current level since it was loaded (pauses, terminals and menus excluded).</summary>
        public float CurrentLevelSeconds => Session != null ? Mathf.Max(0f, Session.PlayTimeSeconds - _levelStartedAt) : 0f;

        private void Awake()
        {
            if (database == null)
            {
                Debug.LogError("[SessionService] GameDatabase reference missing.");
                enabled = false;
                return;
            }

            _saveSystem = SaveSystem.CreateDefault();
            CreateSession();
            ServiceLocator.Register(this);
            ServiceLocator.Register<ISessionService>(this);
        }

        private void Start() => ServiceLocator.TryGet(out _gameManager);

        private void OnEnable()
        {
            EventBus<TraceNoise>.Subscribe(OnTraceNoise);
            EventBus<CheckpointReached>.Subscribe(OnCheckpointReached);
            EventBus<ObjectiveReported>.Subscribe(OnObjectiveReported);
            EventBus<SceneLoadCompleted>.Subscribe(OnSceneLoaded);
            EventBus<LevelCompleted>.Subscribe(OnLevelCompleted);
        }

        private void OnDisable()
        {
            EventBus<TraceNoise>.Unsubscribe(OnTraceNoise);
            EventBus<CheckpointReached>.Unsubscribe(OnCheckpointReached);
            EventBus<ObjectiveReported>.Unsubscribe(OnObjectiveReported);
            EventBus<SceneLoadCompleted>.Unsubscribe(OnSceneLoaded);
            EventBus<LevelCompleted>.Unsubscribe(OnLevelCompleted);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
            ServiceLocator.Unregister<ISessionService>(this);
        }

        private void Update()
        {
            if (Session == null || _gameManager == null || _gameManager.State != GameState.Playing) return;
            Session.Trace.Tick(Time.deltaTime);
            Session.PlayTimeSeconds += Time.deltaTime;
        }

        public void ResetSession()
        {
            CreateSession();
            foreach (LevelDefinition level in database.Levels)
                if (level != null && level.UnlockedByDefault) Session.UnlockLevel(level.LevelId);
            if (database.StartingQuest != null) Session.Quests.Start(database.StartingQuest);
            foreach (ItemGrant grant in database.StartingItems)
                if (grant.Item != null) Session.Inventory.Add(grant.Item, grant.Quantity);
            PublishAll();
        }

        public void NewGame()
        {
            ResetSession();
            LoadScene(newGameScene);
        }

        public bool ContinueGame()
        {
            SaveLoadStatus status = _saveSystem.TryLoad(SaveSlots.Main, out SaveData data);
            if (status != SaveLoadStatus.Success)
            {
                Debug.LogWarning($"[SessionService] Continue failed: {status}.");
                return false;
            }

            CreateSession();
            Session.LoadFrom(data, database.FindItem, database.FindQuest, database.FindSkill);
            PublishAll();
            LoadScene(string.IsNullOrEmpty(data.sceneName) ? newGameScene : data.sceneName);
            return true;
        }

        public void SaveGame()
        {
            if (Session == null) return;
            if (string.IsNullOrEmpty(Session.SceneName)) Session.SceneName = SceneManager.GetActiveScene().name;
            if (_saveSystem.Save(SaveSlots.Main, Session.ToSaveData()))
                Debug.Log($"[SessionService] Saved to {_saveSystem.GetPath(SaveSlots.Main)}");
        }

        /// <summary>Test hook: keep automated runs (exits, checkpoints, travel) away from the developer's real save.</summary>
        internal void UseSaveDirectory(string directory) => _saveSystem = new SaveSystem(directory);

        public bool TryAddItem(ItemDefinition item, int quantity = 1)
        {
            if (Session == null || item == null) return false;
            if (!Session.Inventory.CanAdd(item, quantity))
            {
                EventBus<ToastRequested>.Publish(new ToastRequested(Strings.Hud.InventoryFull));
                return false;
            }

            Session.Inventory.Add(item, quantity);
            Session.Quests.Report(ObjectiveType.Collect, item.ItemId, quantity);
            EventBus<ToastRequested>.Publish(new ToastRequested(string.Format(Strings.Hud.ItemPickedUp, item.DisplayName)));
            return true;
        }

        public bool HasSkillEffect(SkillEffect effect) => Session != null && Session.Skills.HasEffect(effect);

        /// <summary>Buys one unit of <paramref name="offer"/>; the result tells the UI why a purchase was refused.</summary>
        public PurchaseResult Buy(in ShopOffer offer)
        {
            if (Session == null) return PurchaseResult.InvalidOffer;
            PurchaseResult result = Shop.TryBuy(Session.Wallet, Session.Inventory, offer);
            if (result == PurchaseResult.Success)
                EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.Pickup));
            return result;
        }

        public void StartQuest(QuestDefinition quest)
        {
            if (Session == null || !Session.Quests.Start(quest)) return;
            // Data logs found before the contract was accepted still count towards it.
            if (Session.Lore.Count > 0) Session.Quests.ReportTo(quest.QuestId, ObjectiveType.Collect, LoreTargets.AnyLog, Session.Lore.Count);
            EventBus<QuestChanged>.Publish(new QuestChanged(Session.Quests.Get(quest.QuestId)));
        }

        /// <summary>
        /// Records a data log found in a level: small credit/XP reward, archivist progress, toast. False when it was already
        /// found (or there is no session), so the pickup stays put.
        /// </summary>
        public bool CollectLore(LoreDefinition log)
        {
            if (Session == null || log == null || !Session.Lore.Collect(log.LoreId)) return false;
            Session.Wallet.Add(LoreCreditReward);
            Session.Skills.AddXp(LoreXpReward);
            Session.Quests.Report(ObjectiveType.Collect, LoreTargets.AnyLog);
            int total = Mathf.Max(database.Lore.Length, Session.Lore.Count);
            EventBus<LoreCollected>.Publish(new LoreCollected(log, Session.Lore.Count, total));
            EventBus<ToastRequested>.Publish(new ToastRequested(string.Format(Strings.Lore.Found, log.Title, Session.Lore.Count, total)));
            return true;
        }

        private void CreateSession()
        {
            if (Session != null) Unbind(Session);
            Session = new GameSession(database.TraceSettings, GameSession.DefaultInventoryCapacity, database.StartingCredits);
            Bind(Session);
        }

        private void Bind(GameSession session)
        {
            session.Inventory.Changed += OnInventoryChanged;
            session.Wallet.Changed += OnCreditsChanged;
            session.Trace.ValueChanged += OnTraceValueChanged;
            session.Trace.LevelChanged += OnTraceLevelChanged;
            session.Quests.QuestStarted += OnQuestChanged;
            session.Quests.QuestUpdated += OnQuestUpdated;
            session.Quests.QuestCompleted += OnQuestCompleted;
            session.Skills.SkillUnlocked += OnSkillUnlocked;
            session.Skills.XpChanged += OnXpChanged;
        }

        private void Unbind(GameSession session)
        {
            session.Inventory.Changed -= OnInventoryChanged;
            session.Wallet.Changed -= OnCreditsChanged;
            session.Trace.ValueChanged -= OnTraceValueChanged;
            session.Trace.LevelChanged -= OnTraceLevelChanged;
            session.Quests.QuestStarted -= OnQuestChanged;
            session.Quests.QuestUpdated -= OnQuestUpdated;
            session.Quests.QuestCompleted -= OnQuestCompleted;
            session.Skills.SkillUnlocked -= OnSkillUnlocked;
            session.Skills.XpChanged -= OnXpChanged;
        }

        private void PublishAll()
        {
            EventBus<InventoryChanged>.Publish(new InventoryChanged());
            EventBus<CreditsChanged>.Publish(new CreditsChanged(Session.Wallet.Credits));
            EventBus<TraceChanged>.Publish(new TraceChanged(Session.Trace.Value, Session.Trace.Level));
            EventBus<QuestChanged>.Publish(new QuestChanged(Session.Quests.ActiveMainQuest));
            EventBus<XpChanged>.Publish(new XpChanged(Session.Skills.Xp));
        }

        private void LoadScene(string sceneName)
        {
            if (_gameManager == null && !ServiceLocator.TryGet(out _gameManager))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            _gameManager.LoadScene(sceneName);
        }

        private void OnTraceNoise(TraceNoise evt)
        {
            if (Session == null) return;
            float multiplier = Session.Skills.HasEffect(SkillEffect.LongerShadows) ? CamouflageTraceMultiplier : 1f;
            Session.Trace.Add(evt.Amount * multiplier);
        }

        private void OnCheckpointReached(CheckpointReached evt)
        {
            if (Session == null) return;
            Session.SetCheckpoint(SceneManager.GetActiveScene().name, evt.CheckpointId, evt.Position);
            SaveGame();
            EventBus<ToastRequested>.Publish(new ToastRequested(Strings.Hud.CheckpointSaved));
        }

        private void OnObjectiveReported(ObjectiveReported evt) => Session?.Quests.Report(evt.Type, evt.TargetId, evt.Amount);

        private void OnSceneLoaded(SceneLoadCompleted evt)
        {
            if (Session == null) return;
            if (Session.SceneName != evt.SceneName) Session.ClearCheckpoint();
            Session.SceneName = evt.SceneName;
            _levelStartedAt = Session.PlayTimeSeconds;
            Session.Trace.ResetMeter();
            PublishAll();
        }

        private void OnLevelCompleted(LevelCompleted evt)
        {
            if (Session == null) return;
            if (Session.RecordLevelTime(evt.LevelId, CurrentLevelSeconds))
                Debug.Log($"[SessionService] New best time for {evt.LevelId}: {TimeFormat.Clock(CurrentLevelSeconds)}.");
        }

        private void OnInventoryChanged() => EventBus<InventoryChanged>.Publish(new InventoryChanged());
        private void OnCreditsChanged(int credits) => EventBus<CreditsChanged>.Publish(new CreditsChanged(credits));
        private void OnTraceValueChanged(float value) => EventBus<TraceChanged>.Publish(new TraceChanged(value, Session.Trace.Level));

        private void OnTraceLevelChanged(TraceLevel previous, TraceLevel current)
        {
            EventBus<TraceLevelChanged>.Publish(new TraceLevelChanged(previous, current));
            if (current >= TraceLevel.Alert) Session.Quests.ReportDetected();
        }

        private void OnQuestChanged(QuestProgress quest) => EventBus<QuestChanged>.Publish(new QuestChanged(quest));

        private void OnQuestUpdated(QuestProgress quest)
        {
            if (quest.RequiredObjectivesDone && Session.Quests.TryComplete(quest.Definition.QuestId)) return;
            EventBus<QuestChanged>.Publish(new QuestChanged(quest));
        }

        private void OnQuestCompleted(QuestProgress quest)
        {
            Session.Wallet.Add(quest.Definition.RewardCredits);
            Session.Skills.AddXp(quest.Definition.RewardXp);
            EventBus<QuestChanged>.Publish(new QuestChanged(quest));
        }

        private void OnSkillUnlocked(SkillDefinition skill) => EventBus<SkillUnlockedEvent>.Publish(new SkillUnlockedEvent(skill));

        private void OnXpChanged(int xp) => EventBus<XpChanged>.Publish(new XpChanged(xp));
    }
}
