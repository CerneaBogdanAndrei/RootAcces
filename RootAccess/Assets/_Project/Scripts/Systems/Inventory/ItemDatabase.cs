using System.Collections.Generic;
using UnityEngine;

namespace RootAccess.Systems
{
    /// <summary>Id → definition lookup used when restoring saves.</summary>
    [CreateAssetMenu(menuName = "Root Access/Item Database", fileName = "ItemDatabase")]
    public sealed class ItemDatabase : ScriptableObject
    {
        [SerializeField] private ItemDefinition[] items = new ItemDefinition[0];

        private Dictionary<string, ItemDefinition> _lookup;

        public IReadOnlyList<ItemDefinition> Items => items;

        public ItemDefinition Find(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            if (_lookup == null || _lookup.Count != items.Length) BuildLookup();
            return _lookup.TryGetValue(itemId, out ItemDefinition item) ? item : null;
        }

        public void SetItems(ItemDefinition[] definitions)
        {
            items = definitions ?? new ItemDefinition[0];
            _lookup = null;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, ItemDefinition>();
            foreach (ItemDefinition item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.ItemId)) continue;
                if (!_lookup.TryAdd(item.ItemId, item)) Debug.LogWarning($"[ItemDatabase] Duplicate item id '{item.ItemId}'.");
            }
        }
    }
}
