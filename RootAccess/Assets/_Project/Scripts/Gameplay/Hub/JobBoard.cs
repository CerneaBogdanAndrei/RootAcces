using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Darknet job board: lists side contracts that can be accepted from the terminal screen.</summary>
    public sealed class JobBoard : HubConsole
    {
        [SerializeField] private QuestDefinition[] contracts = new QuestDefinition[0];

        public override string Prompt => Strings.Prompts.JobBoard;

        public override bool CanInteract(IInteractor interactor) => contracts.Length > 0;

        protected override void Use(IInteractor interactor) =>
            EventBus<TerminalScreenRequested>.Publish(new TerminalScreenRequested(TerminalTab.Contracts, contracts: contracts));
    }
}
