using UnityEngine;

namespace RootAccess.Systems
{
    public enum ItemCategory
    {
        UsbKey,
        Exploit,
        Gadget,
        DataShard,
        Consumable
    }

    /// <summary>Static item data. Runtime inventories only store the id and a quantity.</summary>
    [CreateAssetMenu(menuName = "Root Access/Item Definition", fileName = "Item_")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private ItemCategory category;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(1)] private int maxStack = 1;
        [SerializeField, Min(0)] private int creditValue;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemCategory Category => category;
        public Sprite Icon => icon;
        public int MaxStack => Mathf.Max(1, maxStack);
        public int CreditValue => creditValue;

        public void Configure(string id, string display, string desc, ItemCategory itemCategory, int stack, int value, Sprite itemIcon = null)
        {
            itemId = id;
            displayName = display;
            description = desc;
            category = itemCategory;
            maxStack = Mathf.Max(1, stack);
            creditValue = value;
            icon = itemIcon;
        }
    }
}
