using System;

namespace LetterHunter.Items
{
    public sealed class InventorySlot
    {
        public ItemDefinition Item { get; private set; }
        public int Amount { get; private set; }
        public bool IsEmpty => Item == null || Amount <= 0;
        public int Capacity => Item != null ? Item.MaxStack : 0;
        public int RemainingSpace => IsEmpty ? 0 : Math.Max(0, Capacity - Amount);
        public ItemStack Stack => IsEmpty ? default : new ItemStack(Item, Amount);

        public void Set(ItemDefinition item, int amount)
        {
            Item = item;
            Amount = item != null ? Math.Clamp(amount, 0, item.MaxStack) : 0;
            if (Amount <= 0)
                Clear();
        }

        public int Add(int amount)
        {
            if (IsEmpty || amount <= 0)
                return 0;

            var added = Math.Min(amount, RemainingSpace);
            Amount += added;
            return added;
        }

        public int Remove(int amount)
        {
            if (IsEmpty || amount <= 0)
                return 0;

            var removed = Math.Min(amount, Amount);
            Amount -= removed;
            if (Amount <= 0)
                Clear();

            return removed;
        }

        public void Clear()
        {
            Item = null;
            Amount = 0;
        }
    }
}
