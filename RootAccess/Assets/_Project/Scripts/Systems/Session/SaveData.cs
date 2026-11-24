using System;
using System.Collections.Generic;

namespace RootAccess.Systems
{
    /// <summary>
    /// Serializable snapshot of a game session (JsonUtility). Bump <see cref="RootAccess.Core.SaveSystem.CurrentVersion"/>
    /// and register a migration whenever this shape changes.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public string sceneName;
        public string checkpointId;
        public float checkpointX;
        public float checkpointY;
        public bool hasCheckpoint;
        public int credits;
        public int xp;
        public float playTimeSeconds;
        public List<string> unlockedSkills = new();
        public List<ItemStackData> inventory = new();
        public List<QuestSaveData> quests = new();
        public List<string> unlockedLevels = new();
        public List<string> hackedTerminals = new();
    }
}
