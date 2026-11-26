using System;
using System.Collections.Generic;

namespace RootAccess.Systems
{
    public enum QuestStatus
    {
        Active,
        Completed,
        Failed
    }

    public sealed class ObjectiveProgress
    {
        public ObjectiveProgress(ObjectiveDefinition definition) => Definition = definition;

        public ObjectiveDefinition Definition { get; }
        public int Count { get; internal set; }
        public bool Failed { get; internal set; }
        public bool IsComplete => !Failed && Definition.Type != ObjectiveType.AvoidDetection && Count >= Definition.RequiredCount;
    }

    public sealed class QuestProgress
    {
        public QuestProgress(QuestDefinition definition)
        {
            Definition = definition;
            Objectives = new List<ObjectiveProgress>();
            foreach (ObjectiveDefinition objective in definition.Objectives)
                if (objective != null) Objectives.Add(new ObjectiveProgress(objective));
        }

        public QuestDefinition Definition { get; }
        public List<ObjectiveProgress> Objectives { get; }
        public QuestStatus Status { get; internal set; } = QuestStatus.Active;

        /// <summary>All required, non-stealth objectives done. Stealth objectives are judged on completion.</summary>
        public bool RequiredObjectivesDone
        {
            get
            {
                foreach (ObjectiveProgress objective in Objectives)
                {
                    if (objective.Definition.Optional || objective.Definition.Type == ObjectiveType.AvoidDetection) continue;
                    if (!objective.IsComplete) return false;
                }

                return true;
            }
        }

        public ObjectiveProgress FirstIncomplete()
        {
            foreach (ObjectiveProgress objective in Objectives)
                if (!objective.Definition.Optional && objective.Definition.Type != ObjectiveType.AvoidDetection && !objective.IsComplete)
                    return objective;
            return null;
        }
    }

    /// <summary>
    /// Tracks quest objectives (reach / hack / collect / avoid detection). Gameplay reports facts
    /// ("hacked terminal X"); the log decides what they mean for every active quest.
    /// </summary>
    public sealed class QuestLog
    {
        private readonly Dictionary<string, QuestProgress> _quests = new();

        public event Action<QuestProgress> QuestStarted;
        public event Action<QuestProgress> QuestUpdated;
        public event Action<QuestProgress> QuestCompleted;

        public IEnumerable<QuestProgress> All => _quests.Values;

        public QuestProgress ActiveMainQuest
        {
            get
            {
                QuestProgress fallback = null;
                foreach (QuestProgress quest in _quests.Values)
                {
                    if (quest.Status != QuestStatus.Active) continue;
                    if (quest.Definition.IsMainStory) return quest;
                    fallback ??= quest;
                }

                return fallback;
            }
        }

        public bool Start(QuestDefinition quest)
        {
            if (quest == null || string.IsNullOrEmpty(quest.QuestId) || _quests.ContainsKey(quest.QuestId)) return false;
            var progress = new QuestProgress(quest);
            _quests.Add(quest.QuestId, progress);
            QuestStarted?.Invoke(progress);
            return true;
        }

        public QuestProgress Get(string questId) =>
            questId != null && _quests.TryGetValue(questId, out QuestProgress progress) ? progress : null;

        public QuestStatus? GetStatus(string questId) => Get(questId)?.Status;

        /// <summary>Reports a gameplay fact; returns true if any objective advanced.</summary>
        public bool Report(ObjectiveType type, string targetId, int amount = 1)
        {
            if (type == ObjectiveType.AvoidDetection || amount <= 0) return false;
            var changed = new List<QuestProgress>();
            foreach (QuestProgress quest in _quests.Values)
                if (quest.Status == QuestStatus.Active && Advance(quest, type, targetId, amount))
                    changed.Add(quest);

            // Listeners may complete a quest and start its follow-up, so notify after the iteration.
            foreach (QuestProgress quest in changed) QuestUpdated?.Invoke(quest);
            return changed.Count > 0;
        }

        /// <summary>
        /// Like <see cref="Report"/> but for one active quest only, e.g. counting data logs found before a contract was
        /// accepted without inflating other quests that track the same target.
        /// </summary>
        public bool ReportTo(string questId, ObjectiveType type, string targetId, int amount = 1)
        {
            QuestProgress quest = Get(questId);
            if (quest == null || quest.Status != QuestStatus.Active || type == ObjectiveType.AvoidDetection || amount <= 0) return false;
            if (!Advance(quest, type, targetId, amount)) return false;
            QuestUpdated?.Invoke(quest);
            return true;
        }

        private static bool Advance(QuestProgress quest, ObjectiveType type, string targetId, int amount)
        {
            bool changed = false;
            foreach (ObjectiveProgress objective in quest.Objectives)
            {
                if (objective.Definition.Type != type || objective.IsComplete) continue;
                if (!string.Equals(objective.Definition.TargetId, targetId, StringComparison.Ordinal)) continue;
                objective.Count = Math.Min(objective.Definition.RequiredCount, objective.Count + amount);
                changed = true;
            }

            return changed;
        }

        /// <summary>Marks "avoid detection" objectives of active quests as failed.</summary>
        public void ReportDetected()
        {
            var changed = new List<QuestProgress>();
            foreach (QuestProgress quest in _quests.Values)
            {
                if (quest.Status != QuestStatus.Active) continue;
                bool failed = false;
                foreach (ObjectiveProgress objective in quest.Objectives)
                {
                    if (objective.Definition.Type != ObjectiveType.AvoidDetection || objective.Failed) continue;
                    objective.Failed = true;
                    failed = true;
                }

                if (failed) changed.Add(quest);
            }

            foreach (QuestProgress quest in changed) QuestUpdated?.Invoke(quest);
        }

        /// <summary>
        /// Completes the quest if its required objectives are done (failed stealth objectives only forfeit bonus rewards),
        /// then starts its follow-up quest, if any: the main story is a chain, one quest per level.
        /// </summary>
        public bool TryComplete(string questId)
        {
            QuestProgress quest = Get(questId);
            if (quest == null || quest.Status != QuestStatus.Active || !quest.RequiredObjectivesDone) return false;
            quest.Status = QuestStatus.Completed;
            foreach (ObjectiveProgress objective in quest.Objectives)
                if (objective.Definition.Type == ObjectiveType.AvoidDetection && !objective.Failed)
                    objective.Count = objective.Definition.RequiredCount;
            QuestCompleted?.Invoke(quest);
            Start(quest.Definition.FollowUpQuest);
            return true;
        }

        public bool StealthBonusEarned(string questId)
        {
            QuestProgress quest = Get(questId);
            if (quest == null) return false;
            foreach (ObjectiveProgress objective in quest.Objectives)
                if (objective.Definition.Type == ObjectiveType.AvoidDetection && objective.Failed) return false;
            return true;
        }

        public List<QuestSaveData> ToData()
        {
            var list = new List<QuestSaveData>();
            foreach (QuestProgress quest in _quests.Values)
            {
                var data = new QuestSaveData
                {
                    questId = quest.Definition.QuestId,
                    status = (int)quest.Status,
                    objectiveCounts = new List<int>(),
                    objectiveFailed = new List<bool>()
                };
                foreach (ObjectiveProgress objective in quest.Objectives)
                {
                    data.objectiveCounts.Add(objective.Count);
                    data.objectiveFailed.Add(objective.Failed);
                }

                list.Add(data);
            }

            return list;
        }

        public void LoadFrom(IEnumerable<QuestSaveData> saved, Func<string, QuestDefinition> resolve)
        {
            _quests.Clear();
            if (saved == null) return;
            foreach (QuestSaveData data in saved)
            {
                QuestDefinition definition = resolve?.Invoke(data.questId);
                if (definition == null) continue;
                var progress = new QuestProgress(definition) { Status = (QuestStatus)data.status };
                for (int i = 0; i < progress.Objectives.Count; i++)
                {
                    if (data.objectiveCounts != null && i < data.objectiveCounts.Count) progress.Objectives[i].Count = data.objectiveCounts[i];
                    if (data.objectiveFailed != null && i < data.objectiveFailed.Count) progress.Objectives[i].Failed = data.objectiveFailed[i];
                }

                _quests[definition.QuestId] = progress;
            }
        }
    }

    [Serializable]
    public sealed class QuestSaveData
    {
        public string questId;
        public int status;
        public List<int> objectiveCounts;
        public List<bool> objectiveFailed;
    }
}
