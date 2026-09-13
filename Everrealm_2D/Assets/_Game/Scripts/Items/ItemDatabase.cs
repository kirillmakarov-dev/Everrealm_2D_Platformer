using System.Collections.Generic;
using UnityEngine;

namespace LetterHunter.Items
{
    [CreateAssetMenu(menuName = "Everrealm/Databases/Item Database")]
    public sealed class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> items = new();

        public IReadOnlyList<ItemDefinition> Items => items;

        public bool TryGetItem(string itemId, out ItemDefinition item)
        {
            item = null;
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            foreach (var candidate in items)
            {
                if (candidate == null || candidate.ItemId != itemId)
                    continue;

                item = candidate;
                return true;
            }

            return false;
        }
    }
}
