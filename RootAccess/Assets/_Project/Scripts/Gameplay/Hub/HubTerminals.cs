using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Base for simple "press E" consoles in the hideout.</summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class HubConsole : MonoBehaviour, IInteractable
    {
        public abstract string Prompt { get; }
        public Vector3 InteractionPosition => transform.position;

        protected virtual void Awake() => GetComponent<Collider2D>().isTrigger = true;

        public virtual bool CanInteract(IInteractor interactor) => true;

        public void Interact(IInteractor interactor)
        {
            EventBus<SfxRequested>.Publish(SfxRequested.Ui(SfxId.UiClick));
            Use(interactor);
        }

        protected abstract void Use(IInteractor interactor);

        protected static void Toast(string message) => EventBus<ToastRequested>.Publish(new ToastRequested(message));

        protected static bool TryGetSession(out SessionService session) => ServiceLocator.TryGet(out session);
    }
}
