using System.Collections.Generic;
using LetterHunter.Items;

namespace LetterHunter.Loot
{
    public readonly struct LootRoll
    {
        public LootRoll(int coins, IReadOnlyList<ItemStack> items)
        {
            Coins = System.Math.Max(0, coins);
            Items = items ?? System.Array.Empty<ItemStack>();
        }

        public int Coins { get; }
        public IReadOnlyList<ItemStack> Items { get; }
        public bool HasAnyRewards => Coins > 0 || Items.Count > 0;
    }
}
