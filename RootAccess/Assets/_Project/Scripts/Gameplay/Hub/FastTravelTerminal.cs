using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Hub fast travel: teleports the player out (dissolve + particles) and loads an unlocked level.</summary>
    public sealed class FastTravelTerminal : HubConsole
    {
        [SerializeField] private LevelDefinition destination;
        [SerializeField] private string arrivalSpawnId;
        [SerializeField] private float departDelay = 0.6f;

        private float _loadAt = -1f;

        public override string Prompt => destination != null
            ? string.Format(Strings.Hub.TargetSuffix, Strings.Prompts.FastTravel, destination.DisplayName)
            : Strings.Prompts.FastTravel;

        public override bool CanInteract(IInteractor interactor) => destination != null && _loadAt < 0f;

        protected override void Use(IInteractor interactor)
        {
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
