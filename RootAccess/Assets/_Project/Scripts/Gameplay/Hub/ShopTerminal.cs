using RootAccess.Core;
using RootAccess.Systems;
using UnityEngine;

namespace RootAccess.Gameplay
{
    /// <summary>Black-market vendor skeleton: sells one item per interaction for credits.</summary>
    public sealed class ShopTerminal : HubConsole
    {
        [SerializeField] private ItemDefinition stock;
        [SerializeField, Min(0)] private int price = 50;

        public override string Prompt => stock != null ? string.Format(Strings.Hub.PriceSuffix, Strings.Prompts.Shop, stock.DisplayName, price) : Strings.Prompts.Shop;

        public override bool CanInteract(IInteractor interactor) => stock != null;

        protected override void Use(IInteractor interactor)
        {
            if (!TryGetSession(out SessionService session)) return;
            Wallet wallet = session.Session.Wallet;
            if (!wallet.CanAfford(price))
            {
                Toast(string.Format(Strings.Hub.TooExpensive, wallet.Credits, price));
                return;
            }

            if (!session.Session.Inventory.CanAdd(stock))
            {
                Toast(Strings.Hud.InventoryFull);
                return;
            }

            wallet.TrySpend(price);
            session.TryAddItem(stock);
            Toast(string.Format(Strings.Hub.Bought, stock.DisplayName, price));
        }
    }
}
