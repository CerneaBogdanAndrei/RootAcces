using UnityEngine;

namespace RootAccess.Core
{
    /// <summary>Static data for a playable level (hub fast-travel, job board, level select).</summary>
    [CreateAssetMenu(menuName = "Root Access/Level Definition", fileName = "Level_")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string levelId;
        [SerializeField] private string sceneName;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private string displayNameEn;
        [SerializeField, TextArea] private string descriptionEn;
        [SerializeField] private bool unlockedByDefault;
        [SerializeField] private int order;

        public string LevelId => levelId;
        public string SceneName => sceneName;
        public string DisplayName => Loc.T(displayName, displayNameEn);
        public string Description => Loc.T(description, descriptionEn);
        public bool UnlockedByDefault => unlockedByDefault;
        public int Order => order;

        public void Configure(string id, string scene, string display, bool unlocked, int sortOrder)
        {
            levelId = id;
            sceneName = scene;
            displayName = display;
            unlockedByDefault = unlocked;
            order = sortOrder;
        }

        public void SetDescription(string text) => description = text;

        /// <summary>English name and description, shown when the game language is English.</summary>
        public void SetEnglish(string display, string text)
        {
            displayNameEn = display;
            descriptionEn = text;
        }
    }
}
