using System;
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
        public string Description => description;
        public ObjectiveType Type => type;
        public string TargetId => targetId;
        public int RequiredCount => requiredCount;
        public bool Optional => optional;
    }

    [CreateAssetMenu(menuName = "Root Access/Quest Definition", fileName = "Quest_")]
    public sealed class QuestDefinition : ScriptableObject
    {
        [SerializeField] private string questId;
        [SerializeField] private string title;
        [SerializeField, TextArea] private string description;
        [SerializeField] private bool isMainStory;
        [SerializeField] private ObjectiveDefinition[] objectives = new ObjectiveDefinition[0];
        [SerializeField, Min(0)] private int rewardCredits;
        [SerializeField, Min(0)] private int rewardXp;

        public string QuestId => questId;
        public string Title => title;
        public string Description => description;
        public bool IsMainStory => isMainStory;
        public ObjectiveDefinition[] Objectives => objectives;
        public int RewardCredits => rewardCredits;
        public int RewardXp => rewardXp;

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
    }
}
