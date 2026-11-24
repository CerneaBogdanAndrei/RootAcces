using UnityEngine;

namespace RootAccess.Systems
{
    public enum SkillBranch
    {
        Stealth,
        Hacking,
        Hardware
    }

    /// <summary>Gameplay hook a skill unlocks; systems query the skill tree for these.</summary>
    public enum SkillEffect
    {
        None,
        DoubleJump,
        SilentSteps,
        LongerShadows,
        FasterHacking,
        ExtraHackAttempt,
        RemoteHack,
        EmpCapacity,
        DashCooldown,
        ArmorPlating
    }

    [CreateAssetMenu(menuName = "Root Access/Skill Definition", fileName = "Skill_")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [SerializeField] private string skillId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private SkillBranch branch;
        [SerializeField, Min(0)] private int xpCost = 100;
        [SerializeField] private SkillEffect effect;
        [SerializeField] private SkillDefinition[] prerequisites = new SkillDefinition[0];

        public string SkillId => skillId;
        public string DisplayName => displayName;
        public string Description => description;
        public SkillBranch Branch => branch;
        public int XpCost => xpCost;
        public SkillEffect Effect => effect;
        public SkillDefinition[] Prerequisites => prerequisites;

        public void Configure(string id, string display, string desc, SkillBranch skillBranch, int cost, SkillEffect skillEffect,
            params SkillDefinition[] requires)
        {
            skillId = id;
            displayName = display;
            description = desc;
            branch = skillBranch;
            xpCost = Mathf.Max(0, cost);
            effect = skillEffect;
            prerequisites = requires ?? new SkillDefinition[0];
        }
    }
}
