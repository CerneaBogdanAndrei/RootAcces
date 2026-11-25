namespace RootAccess.Core
{
    /// <summary>Names accepted by <c>-ra-command</c>; each screen listens for its own and opens itself.</summary>
    public static class AutomationCommands
    {
        public const string Pause = "pause";
        public const string Inventory = "inventory";
        public const string Skills = "skills";
        public const string Quests = "quests";
        public const string RubricPanel = "rubric";
        public const string Settings = "settings";
        public const string GameOver = "gameover";
    }
}
