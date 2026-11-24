using System;
using System.Collections.Generic;

namespace RootAccess.Systems
{
    public sealed class InventorySlot
    {
        public ItemDefinition Item { get; internal set; }
        public int Quantity { get; internal set; }
        public bool IsEmpty => Item == null || Quantity <= 0;
        public int SpaceLeft => IsEmpty ? 0 : Item.MaxStack - Quantity;

        internal void Clear()
        {
            Item = null;
            Quantity = 0;
        }
    }

    /// <summary>
    /// Fixed-capacity, stack-aware inventory model (no Unity dependencies besides item definitions).
    /// Add returns the quantity that did not fit, so callers can leave the rest in the world.
    /// </summary>
    public sealed class Inventory
    {
        private readonly InventorySlot[] _slots;

        public Inventory(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Inventory capacity must be positive.");
            _slots = new InventorySlot[capacity];
            for (int i = 0; i < capacity; i++) _slots[i] = new InventorySlot();
        }

        public event Action Changed;

        public int Capacity => _slots.Length;
        public IReadOnlyList<InventorySlot> Slots => _slots;

        /// <returns>The overflow: how many units could not be stored.</returns>
        public int Add(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return Math.Max(0, quantity);
            int remaining = quantity;

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                InventorySlot slot = _slots[i];
                if (slot.IsEmpty || slot.Item != item || slot.SpaceLeft <= 0) continue;
                int moved = Math.Min(slot.SpaceLeft, remaining);
                slot.Quantity += moved;
                remaining -= moved;
            }

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                InventorySlot slot = _slots[i];
                if (!slot.IsEmpty) continue;
                int moved = Math.Min(item.MaxStack, remaining);
                slot.Item = item;
                slot.Quantity = moved;
                remaining -= moved;
            }

            if (remaining != quantity) Changed?.Invoke();
            return remaining;
        }

        public bool CanAdd(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0) return false;
            int space = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                InventorySlot slot = _slots[i];
                if (slot.IsEmpty) space += item.MaxStack;
                else if (slot.Item == item) space += slot.SpaceLeft;
                if (space >= quantity) return true;
            }

            return false;
        }

        /// <summary>Removes exactly <paramref name="quantity"/> units or nothing at all.</summary>
        public bool Remove(ItemDefinition item, int quantity = 1)
        {
            if (item == null || quantity <= 0 || Count(item) < quantity) return false;
            int remaining = quantity;
            for (int i = _slots.Length - 1; i >= 0 && remaining > 0; i--)
            {
                InventorySlot slot = _slots[i];
                if (slot.IsEmpty || slot.Item != item) continue;
                int taken = Math.Min(slot.Quantity, remaining);
                slot.Quantity -= taken;
                remaining -= taken;
                if (slot.Quantity <= 0) slot.Clear();
            }

            Changed?.Invoke();
            return true;
        }

        public int Count(ItemDefinition item)
        {
            if (item == null) return 0;
            int total = 0;
            for (int i = 0; i < _slots.Length; i++)
                if (!_slots[i].IsEmpty && _slots[i].Item == item) total += _slots[i].Quantity;
            return total;
        }

        public bool Contains(ItemDefinition item, int quantity = 1) => Count(item) >= quantity;

        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++) _slots[i].Clear();
            Changed?.Invoke();
        }

        public List<ItemStackData> ToData()
        {
            var data = new List<ItemStackData>();
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].IsEmpty) continue;
                data.Add(new ItemStackData { itemId = _slots[i].Item.ItemId, quantity = _slots[i].Quantity });
            }

            return data;
        }

        public void LoadFrom(IEnumerable<ItemStackData> stacks, Func<string, ItemDefinition> resolve)
        {
            for (int i = 0; i < _slots.Length; i++) _slots[i].Clear();
            if (stacks != null)
            {
                foreach (ItemStackData stack in stacks)
                {
                    ItemDefinition item = resolve?.Invoke(stack.itemId);
                    if (item != null) Add(item, stack.quantity);
                }
            }

            Changed?.Invoke();
        }
    }

    [Serializable]
    public struct ItemStackData
    {
        public string itemId;
        public int quantity;
    }
}
