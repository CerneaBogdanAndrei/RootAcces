using System;
using System.Collections.Generic;

namespace RootAccess.Systems
{
    /// <summary>
    /// Serializable snapshot of a game session (JsonUtility). New fields with defaults stay compatible with older saves
    /// (JsonUtility keeps the initializer for missing keys, e.g. <see cref="levelTimes"/>); renaming or removing a field
    /// needs a <see cref="RootAccess.Core.SaveSystem.CurrentVersion"/> bump and a registered migration.
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
        public List<LevelTimeData> levelTimes = new();
        /// <summary>Ids of the data logs found (v1.1; older saves load with none).</summary>
        public List<string> loreLogs = new();
    }

    /// <summary>Best completion time of one level (play time, pauses excluded).</summary>
    [Serializable]
    public sealed class LevelTimeData
    {
        public string levelId;
        public float seconds;
    }
}
