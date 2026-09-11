using System.Collections.Generic;
using LetterHunter.Items;
using UnityEngine;

namespace LetterHunter.Economy
{
    [System.Serializable]
    public sealed class ShopCatalogEntry
    {
        [SerializeField] private ItemDefinition item;
        [Min(1), SerializeField] private int buyPrice = 1;

        public ItemDefinition Item => item;
        public int BuyPrice => Mathf.Max(1, buyPrice);
    }

    [CreateAssetMenu(menuName = "Letter Hunter/Economy/Shop Catalog")]
    public sealed class ShopCatalogDefinition : ScriptableObject
    {
        [SerializeField] private List<ShopCatalogEntry> entries = new();
        [SerializeField] private List<ItemDefinition> sellableItems = new();

        public IReadOnlyList<ShopCatalogEntry> Entries => entries;
        public IReadOnlyList<ItemDefinition> SellableItems => sellableItems;

        public bool TryGetPrice(ItemDefinition item, out int price)
        {
            price = 0;
            if (item == null)
                return false;

            foreach (var entry in entries)
            {
                if (entry == null || entry.Item != item)
                    continue;
                price = entry.BuyPrice;
                return true;
            }

            return false;
        }

        public bool CanSell(ItemDefinition item)
        {
            return item != null && sellableItems.Contains(item);
        }
    }
}
