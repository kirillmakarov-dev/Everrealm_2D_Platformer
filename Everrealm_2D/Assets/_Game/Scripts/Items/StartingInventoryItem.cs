using System;
using UnityEngine;

namespace LetterHunter.Items
{
    [Serializable]
    public sealed class StartingInventoryItem
    {
        [SerializeField] private ItemDefinition item;
        [Min(1), SerializeField] private int amount = 1;

        public ItemDefinition Item => item;
        public int Amount => Mathf.Max(1, amount);
        public bool IsValid => item != null && amount > 0;
        public ItemStack ToStack() => new(item, Amount);
    }
}
