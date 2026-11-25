using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Black-market vendor: opens the shop tab of the personal terminal with this console's stock.</summary>
    public sealed class ShopTerminal : HubConsole
    {
        [SerializeField] private ShopOffer[] offers = new ShopOffer[0];

        public override string Prompt => Strings.Prompts.Shop;

        public override bool CanInteract(IInteractor interactor) => offers.Length > 0;

        protected override void Use(IInteractor interactor) =>
            EventBus<TerminalScreenRequested>.Publish(new TerminalScreenRequested(TerminalTab.Shop, offers: offers));
    }
}
