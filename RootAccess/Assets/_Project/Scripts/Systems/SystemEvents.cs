using RootAccess.Core;

namespace RootAccess.Systems
{
    public readonly struct InventoryChanged : IEvent { }

    public readonly struct CreditsChanged : IEvent
    {
        public readonly int Credits;
        public CreditsChanged(int credits) => Credits = credits;
    }

    public readonly struct TraceChanged : IEvent
    {
        public readonly float Value;
        public readonly TraceLevel Level;

        public TraceChanged(float value, TraceLevel level)
        {
            Value = value;
            Level = level;
        }
    }

    public readonly struct TraceLevelChanged : IEvent
    {
        public readonly TraceLevel Previous;
        public readonly TraceLevel Current;

        public TraceLevelChanged(TraceLevel previous, TraceLevel current)
        {
            Previous = previous;
            Current = current;
        }
    }

    public readonly struct QuestChanged : IEvent
    {
        public readonly QuestProgress Quest;
        public QuestChanged(QuestProgress quest) => Quest = quest;
    }

    public readonly struct SkillUnlockedEvent : IEvent
    {
        public readonly SkillDefinition Skill;
        public SkillUnlockedEvent(SkillDefinition skill) => Skill = skill;
    }

    public readonly struct ObjectiveReported : IEvent
    {
        public readonly ObjectiveType Type;
        public readonly string TargetId;
        public readonly int Amount;

        public ObjectiveReported(ObjectiveType type, string targetId, int amount = 1)
        {
            Type = type;
            TargetId = targetId;
            Amount = amount;
        }
    }

    public readonly struct DialogueRequested : IEvent
    {
        public readonly DialogueDefinition Dialogue;
        public DialogueRequested(DialogueDefinition dialogue) => Dialogue = dialogue;
    }

    public readonly struct DialogueLineShown : IEvent
    {
        public readonly DialogueLine Line;
        public DialogueLineShown(DialogueLine line) => Line = line;
    }

    public readonly struct DialogueEnded : IEvent { }

    public readonly struct ToastRequested : IEvent
    {
        public readonly string Message;
        public ToastRequested(string message) => Message = message;
    }
}
