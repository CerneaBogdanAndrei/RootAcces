using System;
using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Systems
{
    public enum ObjectiveType
    {
        Reach,
        Hack,
        Collect,
        AvoidDetection
    }

    [Serializable]
    public sealed class ObjectiveDefinition
    {
        [SerializeField] private string objectiveId;
        [SerializeField] private string description;
        [SerializeField] private string descriptionEn;
        [SerializeField] private ObjectiveType type;
        [SerializeField, Tooltip("Location id, terminal id or item id depending on the objective type.")]
        private string targetId;
        [SerializeField, Min(1)] private int requiredCount = 1;
        [SerializeField, Tooltip("Optional objectives do not block quest completion.")]
        private bool optional;

        public ObjectiveDefinition(string id, string text, ObjectiveType objectiveType, string target, int count = 1, bool isOptional = false)
        {
            objectiveId = id;
            description = text;
            type = objectiveType;
            targetId = target;
            requiredCount = Mathf.Max(1, count);
            optional = isOptional;
        }

        public string ObjectiveId => objectiveId;
        public string Description => Loc.T(description, descriptionEn);
        public ObjectiveType Type => type;
        public string TargetId => targetId;
        public int RequiredCount => requiredCount;
        public bool Optional => optional;

        /// <summary>The Romanian text as authored (the generators look up its English twin from it).</summary>
        public string SourceDescription => description;

        /// <summary>English objective text, shown when the game language is English.</summary>
        public void SetEnglish(string text) => descriptionEn = text;
    }

    [CreateAssetMenu(menuName = "Root Access/Quest Definition", fileName = "Quest_")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string questId;
        [SerializeField] private string title;
        [SerializeField, TextArea] private string description;
        [SerializeField] private string titleEn;
        [SerializeField, TextArea] private string descriptionEn;
        [SerializeField] private bool isMainStory;
        [SerializeField] private ObjectiveDefinition[] objectives = new ObjectiveDefinition[0];
        [SerializeField, Min(0)] private int rewardCredits;
        [SerializeField, Min(0)] private int rewardXp;
        [SerializeField, Tooltip("Started automatically when this quest completes (main story chain).")]
        private QuestDefinition followUpQuest;

        public string QuestId => questId;
        public string Title => Loc.T(title, titleEn);
        public string Description => Loc.T(description, descriptionEn);
        public bool IsMainStory => isMainStory;
        public ObjectiveDefinition[] Objectives => objectives;
        public int RewardCredits => rewardCredits;
        public int RewardXp => rewardXp;
        public QuestDefinition FollowUpQuest => followUpQuest;

        public void Configure(string id, string questTitle, string desc, bool mainStory, int credits, int xp,
            params ObjectiveDefinition[] questObjectives)
        {
            questId = id;
            title = questTitle;
            description = desc;
            isMainStory = mainStory;
            rewardCredits = credits;
            rewardXp = xp;
            objectives = questObjectives ?? new ObjectiveDefinition[0];
        }

        /// <summary>English title and description, shown when the game language is English.</summary>
        public void SetEnglish(string questTitle, string desc)
        {
            titleEn = questTitle;
            descriptionEn = desc;
        }

        /// <summary>Chains this quest to the next one; null ends the chain.</summary>
        public void SetFollowUp(QuestDefinition next) => followUpQuest = next == this ? null : next;
    }
}
