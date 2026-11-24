using System;
using System.Collections.Generic;

namespace RootAccess.Systems
{
    public enum SkillUnlockResult
    {
        Success,
        AlreadyUnlocked,
        MissingPrerequisite,
        NotEnoughXp,
        InvalidSkill
    }

    /// <summary>XP wallet + unlocked set with prerequisite validation for the three skill branches.</summary>
    public sealed class SkillTree
    {
        private readonly HashSet<string> _unlocked = new();
        private readonly HashSet<SkillEffect> _effects = new();

        public event Action<SkillDefinition> SkillUnlocked;
        public event Action<int> XpChanged;

        public int Xp { get; private set; }
        public IReadOnlyCollection<string> UnlockedIds => _unlocked;

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            Xp += amount;
            XpChanged?.Invoke(Xp);
        }

        public bool IsUnlocked(SkillDefinition skill) => skill != null && _unlocked.Contains(skill.SkillId);

        public bool HasEffect(SkillEffect effect) => _effects.Contains(effect);

        public SkillUnlockResult CanUnlock(SkillDefinition skill)
        {
            if (skill == null || string.IsNullOrEmpty(skill.SkillId)) return SkillUnlockResult.InvalidSkill;
            if (IsUnlocked(skill)) return SkillUnlockResult.AlreadyUnlocked;
            foreach (SkillDefinition prerequisite in skill.Prerequisites)
                if (prerequisite != null && !IsUnlocked(prerequisite)) return SkillUnlockResult.MissingPrerequisite;
            return Xp < skill.XpCost ? SkillUnlockResult.NotEnoughXp : SkillUnlockResult.Success;
        }

        public SkillUnlockResult TryUnlock(SkillDefinition skill)
        {
            SkillUnlockResult result = CanUnlock(skill);
            if (result != SkillUnlockResult.Success) return result;

            Xp -= skill.XpCost;
            _unlocked.Add(skill.SkillId);
            if (skill.Effect != SkillEffect.None) _effects.Add(skill.Effect);
            XpChanged?.Invoke(Xp);
            SkillUnlocked?.Invoke(skill);
            return SkillUnlockResult.Success;
        }

        public void LoadFrom(int xp, IEnumerable<string> unlockedIds, Func<string, SkillDefinition> resolve)
        {
            Xp = Math.Max(0, xp);
            _unlocked.Clear();
            _effects.Clear();
            if (unlockedIds != null)
            {
                foreach (string id in unlockedIds)
                {
                    if (string.IsNullOrEmpty(id)) continue;
                    _unlocked.Add(id);
                    SkillDefinition definition = resolve?.Invoke(id);
                    if (definition != null && definition.Effect != SkillEffect.None) _effects.Add(definition.Effect);
                }
            }

            XpChanged?.Invoke(Xp);
        }
    }
}
