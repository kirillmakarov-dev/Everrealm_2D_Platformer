using UnityEngine;

namespace LetterHunter.Items
{
    public enum ItemType { Material, Consumable, Weapon, Equipment, QuestItem }
    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

    [CreateAssetMenu(menuName = "Everrealm/Items/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [TextArea, SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemType itemType;
        [SerializeField] private ItemRarity rarity;
        [Min(1), SerializeField] private int maxStack = 1;
        [Min(0), SerializeField] private int sellPrice;
        [SerializeField] private bool canSell = true;
        [Header("Consumable Effect")]
        [Min(0f), SerializeField] private float healthRestore;
        [Min(0f), SerializeField] private float manaRestore;

        public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemType ItemType => itemType;
        public ItemRarity Rarity => rarity;
        public int MaxStack => Mathf.Max(1, maxStack);
        public int SellPrice => Mathf.Max(0, sellPrice);
        public bool CanSell => canSell && SellPrice > 0;
        public float HealthRestore => Mathf.Max(0f, healthRestore);
        public float ManaRestore => Mathf.Max(0f, manaRestore);
    }
}
