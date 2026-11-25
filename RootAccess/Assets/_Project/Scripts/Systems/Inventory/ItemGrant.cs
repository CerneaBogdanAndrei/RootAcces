using System;
using UnityEngine;

namespace RootAccess.Systems
{
    /// <summary>A fixed amount of an item handed to the player (new game loadout, rewards).</summary>
    [Serializable]
    public struct ItemGrant
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int quantity;

        public ItemGrant(ItemDefinition item, int quantity)
        {
            this.item = item;
            this.quantity = Mathf.Max(1, quantity);
        }

        public ItemDefinition Item => item;
        public int Quantity => Mathf.Max(1, quantity);
    }
}
