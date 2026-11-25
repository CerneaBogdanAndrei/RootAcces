using System;
using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.Systems
{
    /// <summary>
    /// Plain C# aggregate of all mutable gameplay state. Presentation listens to its models; save/load and
    /// (later) network replication work on this object only.
    /// </summary>
    public sealed class GameSession
    {
        public const int DefaultInventoryCapacity = 12;

        private readonly HashSet<string> _unlockedLevels = new();
        private readonly HashSet<string> _hackedTerminals = new();
        private readonly Dictionary<string, float> _levelTimes = new();

        public GameSession(TraceSettings traceSettings, int inventoryCapacity = DefaultInventoryCapacity, int startingCredits = 0)
        {
            Inventory = new Inventory(inventoryCapacity);
            Wallet = new Wallet(startingCredits);
            Trace = new TraceMeter(traceSettings);
            Skills = new SkillTree();
            Quests = new QuestLog();
        }

        public Inventory Inventory { get; }
        public Wallet Wallet { get; }
        public TraceMeter Trace { get; }
        public SkillTree Skills { get; }
        public QuestLog Quests { get; }

        public string SceneName { get; set; }
        public string CheckpointId { get; private set; }
        public Vector2 CheckpointPosition { get; private set; }
        public bool HasCheckpoint { get; private set; }
        public float PlayTimeSeconds { get; set; }

        public IReadOnlyCollection<string> UnlockedLevels => _unlockedLevels;

        /// <summary>Best completion time per level id, in seconds of play time.</summary>
        public IReadOnlyDictionary<string, float> LevelTimes => _levelTimes;

        /// <summary>Records a completion; keeps the best (shortest) time. Returns true for a new best.</summary>
        public bool RecordLevelTime(string levelId, float seconds)
        {
            if (string.IsNullOrEmpty(levelId) || seconds <= 0f) return false;
            if (_levelTimes.TryGetValue(levelId, out float best) && best <= seconds) return false;
            _levelTimes[levelId] = seconds;
            return true;
        }

        public void SetCheckpoint(string sceneName, string checkpointId, Vector2 position)
        {
            SceneName = sceneName;
            CheckpointId = checkpointId;
            CheckpointPosition = position;
            HasCheckpoint = true;
        }

        public void ClearCheckpoint()
        {
            CheckpointId = null;
            HasCheckpoint = false;
        }

        public bool UnlockLevel(string levelId) => !string.IsNullOrEmpty(levelId) && _unlockedLevels.Add(levelId);
        public bool IsLevelUnlocked(string levelId) => _unlockedLevels.Contains(levelId);
        public bool MarkTerminalHacked(string terminalId) => !string.IsNullOrEmpty(terminalId) && _hackedTerminals.Add(terminalId);
        public bool IsTerminalHacked(string terminalId) => _hackedTerminals.Contains(terminalId);

        public SaveData ToSaveData()
        {
            return new SaveData
            {
                sceneName = SceneName,
                checkpointId = CheckpointId,
                checkpointX = CheckpointPosition.x,
                checkpointY = CheckpointPosition.y,
                hasCheckpoint = HasCheckpoint,
                credits = Wallet.Credits,
                xp = Skills.Xp,
                playTimeSeconds = PlayTimeSeconds,
                unlockedSkills = new List<string>(Skills.UnlockedIds),
                inventory = Inventory.ToData(),
                quests = Quests.ToData(),
                unlockedLevels = new List<string>(_unlockedLevels),
                hackedTerminals = new List<string>(_hackedTerminals),
                levelTimes = LevelTimesData()
            };
        }

        private List<LevelTimeData> LevelTimesData()
        {
            var list = new List<LevelTimeData>(_levelTimes.Count);
            foreach (KeyValuePair<string, float> entry in _levelTimes) list.Add(new LevelTimeData { levelId = entry.Key, seconds = entry.Value });
            return list;
        }

        public void LoadFrom(SaveData data, Func<string, ItemDefinition> items, Func<string, QuestDefinition> quests,
            Func<string, SkillDefinition> skills)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            SceneName = data.sceneName;
            CheckpointId = data.checkpointId;
            CheckpointPosition = new Vector2(data.checkpointX, data.checkpointY);
            HasCheckpoint = data.hasCheckpoint;
            PlayTimeSeconds = data.playTimeSeconds;
            Wallet.Set(data.credits);
            Skills.LoadFrom(data.xp, data.unlockedSkills, skills);
            Inventory.LoadFrom(data.inventory, items);
            Quests.LoadFrom(data.quests, quests);
            _unlockedLevels.Clear();
            if (data.unlockedLevels != null) _unlockedLevels.UnionWith(data.unlockedLevels);
            _hackedTerminals.Clear();
            if (data.hackedTerminals != null) _hackedTerminals.UnionWith(data.hackedTerminals);
            _levelTimes.Clear();
            if (data.levelTimes != null)
                foreach (LevelTimeData entry in data.levelTimes)
                    RecordLevelTime(entry.levelId, entry.seconds);
            Trace.ResetMeter();
        }
    }
}
