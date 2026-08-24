using System;

namespace LetterHunter.Items
{
    [Serializable]
    public readonly struct ItemStack
    {
        public ItemStack(ItemDefinition item, int amount)
        {
            Item = item;
            Amount = Math.Max(0, amount);
        }

        public ItemDefinition Item { get; }
        public int Amount { get; }
        public bool IsValid => Item != null && Amount > 0;

        public ItemStack WithAmount(int amount) => new(Item, amount);
    }
}
