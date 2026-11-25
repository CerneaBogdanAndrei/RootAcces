using RootAccess.Core;
using RootAccess.Systems;

namespace RootAccess.Gameplay
{
    /// <summary>The hideout's skill terminal: the only place where skills can be unlocked with XP.</summary>
    public sealed class SkillTerminal : HubConsole
    {
        public override string Prompt => Strings.Prompts.SkillTerminal;

        protected override void Use(IInteractor interactor) =>
            EventBus<TerminalScreenRequested>.Publish(new TerminalScreenRequested(TerminalTab.Skills, allowSkillUnlock: true));
    }
}
