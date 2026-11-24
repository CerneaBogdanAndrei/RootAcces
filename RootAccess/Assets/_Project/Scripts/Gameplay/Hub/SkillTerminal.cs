using RootAccess.Core;
using RootAccess.Systems;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Skill terminal skeleton: unlocks the first affordable skill whose prerequisites are met.
    /// The full three-branch skill tree UI arrives in ROADMAP P4.
    /// </summary>
    public sealed class SkillTerminal : HubConsole
    {
        public override string Prompt => Strings.Prompts.SkillTerminal;

        protected override void Use(IInteractor interactor)
        {
            if (!TryGetSession(out SessionService session)) return;
            SkillTree tree = session.Session.Skills;
            foreach (SkillDefinition skill in session.Database.Skills)
            {
                if (tree.TryUnlock(skill) != SkillUnlockResult.Success) continue;
                Toast(string.Format(Strings.Hub.SkillUnlocked, skill.DisplayName));
                return;
            }

            Toast(string.Format(Strings.Hub.NoSkillAvailable, tree.Xp));
        }
    }
}
