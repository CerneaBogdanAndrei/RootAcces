using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Darknet job board: accepts the next contract (quest) and optionally plays its briefing.</summary>
    public sealed class JobBoard : HubConsole
    {
        [SerializeField] private QuestDefinition contract;
        [SerializeField] private DialogueDefinition briefing;

        public override string Prompt => Strings.Prompts.JobBoard;

        protected override void Use(IInteractor interactor)
        {
            if (contract == null || !TryGetSession(out SessionService session)) return;
            if (session.Session.Quests.Get(contract.QuestId) != null)
            {
                Toast(string.Format(Strings.Hub.ContractActive, contract.Title));
                return;
            }

            session.StartQuest(contract);
            Toast(string.Format(Strings.Hub.ContractAccepted, contract.Title));
            if (briefing != null) EventBus<DialogueRequested>.Publish(new DialogueRequested(briefing));
        }
    }
}
