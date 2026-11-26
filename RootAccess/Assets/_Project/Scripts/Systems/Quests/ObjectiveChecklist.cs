using System.Collections.Generic;

namespace RootAccess.Systems
{
    /// <summary>How one objective is shown in the HUD's objective list.</summary>
    public enum ChecklistMark
    {
        /// <summary>Done: ticked and greyed out.</summary>
        Done,
        /// <summary>The next thing to do (the first required objective still open).</summary>
        Current,
        /// <summary>Required, but later in the order (a required "stay undetected" stays here until the quest ends).</summary>
        Pending,
        /// <summary>Optional (the "don't trigger the alarm" bonus): judged when the quest ends.</summary>
        Bonus,
        /// <summary>Failed (a stealth bonus after the alarm went off).</summary>
        Failed
    }

    /// <summary>One line of the objective list: the objective, its mark and its counter.</summary>
    public readonly struct ChecklistLine
    {
        public readonly ObjectiveProgress Objective;
        public readonly ChecklistMark Mark;

        public ChecklistLine(ObjectiveProgress objective, ChecklistMark mark)
        {
            Objective = objective;
            Mark = mark;
        }

        public string Text => Objective.Definition.Description;
        public int Count => Objective.Count;
        public int Required => Objective.Definition.RequiredCount;
        /// <summary>Counters only make sense for "find 10 logs"-style objectives.</summary>
        public bool ShowsCounter => Required > 1 && Mark != ChecklistMark.Done;
    }

    /// <summary>
    /// The HUD's "what to do now" list for a quest, as plain data (the view only colours it): every objective in order,
    /// the first open required one marked as current, bonuses and failures apart.
    /// </summary>
    public static class ObjectiveChecklist
    {
        public static List<ChecklistLine> For(QuestProgress quest)
        {
            var lines = new List<ChecklistLine>();
            if (quest == null) return lines;
            ObjectiveProgress current = quest.FirstIncomplete();
            foreach (ObjectiveProgress objective in quest.Objectives)
                lines.Add(new ChecklistLine(objective, MarkOf(objective, current)));
            return lines;
        }

        private static ChecklistMark MarkOf(ObjectiveProgress objective, ObjectiveProgress current)
        {
            if (objective.Failed) return ChecklistMark.Failed;
            if (objective.IsComplete) return ChecklistMark.Done;
            if (objective == current) return ChecklistMark.Current;
            return objective.Definition.Optional ? ChecklistMark.Bonus : ChecklistMark.Pending;
        }
    }
}
