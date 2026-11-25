using System;
using UnityEngine;

namespace RootAccess.Systems
{
    public enum PurchaseResult
    {
        Success,
        InvalidOffer,
        NotEnoughCredits,
        InventoryFull
    }

    /// <summary>One line of a vendor's stock: what is sold and for how many credits.</summary>
    [Serializable]
    public struct ShopOffer
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(0)] private int price;

        public ShopOffer(ItemDefinition item, int price)
        {
            this.item = item;
            this.price = Mathf.Max(0, price);
        }

        public ItemDefinition Item => item;
        public int Price => price;
        public bool IsValid => item != null;
    }

    /// <summary>Purchase rules shared by every vendor: all-or-nothing, never charges for an item that does not fit.</summary>
    public static class Shop
    {
        public static PurchaseResult CanBuy(Wallet wallet, Inventory inventory, in ShopOffer offer)
        {
            if (wallet == null || inventory == null || !offer.IsValid) return PurchaseResult.InvalidOffer;
            if (!wallet.CanAfford(offer.Price)) return PurchaseResult.NotEnoughCredits;
            return inventory.CanAdd(offer.Item) ? PurchaseResult.Success : PurchaseResult.InventoryFull;
        }

        public static PurchaseResult TryBuy(Wallet wallet, Inventory inventory, in ShopOffer offer)
        {
            PurchaseResult result = CanBuy(wallet, inventory, offer);
            if (result != PurchaseResult.Success) return result;
            wallet.TrySpend(offer.Price);
            inventory.Add(offer.Item);
            return PurchaseResult.Success;
        }
    }
}
