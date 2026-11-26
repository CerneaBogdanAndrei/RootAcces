using UnityEngine;

namespace RootAccess.Systems
{
    /// <summary>
    /// One data log hidden in a level: a page of the Halden Systems story, read in the terminal's "Jurnale" tab once found.
    /// </summary>
    [CreateAssetMenu(menuName = "Root Access/Lore Log", fileName = "Lore_")]
    public sealed class LoreDefinition : ScriptableObject
    {
        [SerializeField] private string loreId;
        [SerializeField] private string title;
        [SerializeField, TextArea(3, 8)] private string body;
        [SerializeField, Tooltip("Level id where the log is hidden (shown as a hint while it is still missing).")]
        private string levelId;
        [SerializeField, Min(0)] private int order;

        public string LoreId => loreId;
        public string Title => title;
        public string Body => body;
        public string LevelId => levelId;
        public int Order => order;

        public void Configure(string id, string logTitle, string text, string level, int index)
        {
            loreId = id;
            title = logTitle;
            body = text;
            levelId = level;
            order = index;
        }
    }
}
