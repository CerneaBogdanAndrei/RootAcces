using RootAccess.Core;
using UnityEngine;

namespace RootAccess.Systems
{
    /// <summary>Root content registry: every definition the session may need to resolve by id.</summary>
    [CreateAssetMenu(menuName = "Root Access/Game Database", fileName = "GameDatabase")]
    public sealed class GameDatabase : ScriptableObject
    {
        [SerializeField] private ItemDatabase items;
        [SerializeField] private QuestDefinition[] quests = new QuestDefinition[0];
        [SerializeField] private SkillDefinition[] skills = new SkillDefinition[0];
        [SerializeField] private LevelDefinition[] levels = new LevelDefinition[0];
        [SerializeField] private QuestDefinition startingQuest;
        [SerializeField] private TraceSettings traceSettings = TraceSettings.Default;
        [SerializeField, Min(0)] private int startingCredits = 150;
        [SerializeField] private ItemGrant[] startingItems = new ItemGrant[0];
        [SerializeField, Tooltip("Data logs hidden in the levels, in reading order.")]
        private LoreDefinition[] lore = new LoreDefinition[0];

        public ItemDatabase Items => items;
        public QuestDefinition[] Quests => quests;
        public SkillDefinition[] Skills => skills;
        public LevelDefinition[] Levels => levels;
        public QuestDefinition StartingQuest => startingQuest;
        public TraceSettings TraceSettings => traceSettings;
        public int StartingCredits => startingCredits;
        public ItemGrant[] StartingItems => startingItems;
        public LoreDefinition[] Lore => lore;

        public ItemDefinition FindItem(string id) => items != null ? items.Find(id) : null;

        public QuestDefinition FindQuest(string id)
        {
            foreach (QuestDefinition quest in quests)
                if (quest != null && quest.QuestId == id) return quest;
            return null;
        }

        public SkillDefinition FindSkill(string id)
        {
            foreach (SkillDefinition skill in skills)
                if (skill != null && skill.SkillId == id) return skill;
            return null;
        }

        public LoreDefinition FindLore(string id)
        {
            foreach (LoreDefinition log in lore)
                if (log != null && log.LoreId == id) return log;
            return null;
        }

        public LevelDefinition FindLevel(string levelId)
        {
            foreach (LevelDefinition level in levels)
                if (level != null && level.LevelId == levelId) return level;
            return null;
        }

        public LevelDefinition FindLevelByScene(string sceneName)
        {
            foreach (LevelDefinition level in levels)
                if (level != null && level.SceneName == sceneName) return level;
            return null;
        }
    }
}
