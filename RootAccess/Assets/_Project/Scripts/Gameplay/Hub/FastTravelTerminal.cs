using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>
    /// Hub fast travel. With a list of <see cref="destinations"/> it opens the terminal's Travel tab (every level, locked
    /// ones greyed out); with a single <see cref="destination"/> it teleports the player out (dissolve + particles) and
    /// loads that level directly.
    /// </summary>
    public sealed class FastTravelTerminal : HubConsole
    {
        [SerializeField] private LevelDefinition destination;
        [SerializeField, Tooltip("Non-empty: open the Travel tab with these levels instead of travelling directly.")]
        private LevelDefinition[] destinations = new LevelDefinition[0];
        [SerializeField] private string arrivalSpawnId;
        [SerializeField] private float departDelay = 0.6f;

        private float _loadAt = -1f;

        private bool OpensTravelTab => destinations is { Length: > 0 };

        public override string Prompt => !OpensTravelTab && destination != null
            ? string.Format(Strings.Hub.TargetSuffix, Strings.Prompts.FastTravel, destination.DisplayName)
            : Strings.Prompts.FastTravel;

        public override bool CanInteract(IInteractor interactor) => (OpensTravelTab || destination != null) && _loadAt < 0f;

        protected override void Use(IInteractor interactor)
        {
            if (OpensTravelTab)
            {
                EventBus<TerminalScreenRequested>.Publish(new TerminalScreenRequested(TerminalTab.Travel, destinations: destinations));
                return;
            }

            bool unlocked = destination.UnlockedByDefault ||
                            (TryGetSession(out SessionService session) && session.Session.IsLevelUnlocked(destination.LevelId));
            if (!unlocked)
            {
                Toast(string.Format(Strings.Hub.LevelLocked, destination.DisplayName));
                return;
            }

            if (interactor.GameObject.TryGetComponent(out ITeleportable traveller) && traveller.CanTeleport) traveller.BeginTeleport();
            EventBus<VfxRequested>.Publish(new VfxRequested(VfxId.TeleportOut, interactor.Transform.position));
            _loadAt = Time.time + departDelay;
        }

        private void Update()
        {
            if (_loadAt < 0f || Time.time < _loadAt) return;
            _loadAt = -1f;
            if (ServiceLocator.TryGet(out GameManager gameManager)) gameManager.LoadScene(destination.SceneName, arrivalSpawnId);
        }
    }
}
