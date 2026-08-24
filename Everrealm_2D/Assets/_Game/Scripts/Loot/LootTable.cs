using System.Collections.Generic;
using LetterHunter.Effects;
using LetterHunter.Items;
using UnityEngine;

namespace LetterHunter.Loot
{
    [CreateAssetMenu(menuName = "Letter Hunter/Loot/Loot Table")]
    public sealed class LootTable : ScriptableObject
    {
        [SerializeField] private string lootTableId;
        [Range(0f, 1f), SerializeField] private float coinDropChance = 1f;
        [Min(0), SerializeField] private int coinMin;
        [Min(0), SerializeField] private int coinMax;
        [SerializeField] private LootEntry[] drops = System.Array.Empty<LootEntry>();

        public string LootTableId => string.IsNullOrWhiteSpace(lootTableId) ? name : lootTableId;
        public float CoinDropChance => Mathf.Clamp01(coinDropChance);
        public int CoinMin => Mathf.Max(0, coinMin);
        public int CoinMax => Mathf.Max(CoinMin, coinMax);
        public IReadOnlyList<LootEntry> Drops => drops;

        public LootRoll Roll(IRandomSource random = null)
        {
            random ??= new SystemRandomSource();
            var coins = RollCoins(random);
            var items = new List<ItemStack>();

            if (drops != null)
            {
                foreach (var drop in drops)
                    TryAddDrop(drop, random, items);
            }

            return new LootRoll(coins, items);
        }

        private int RollCoins(IRandomSource random)
        {
            if (CoinMax <= 0 || random.Next01() > CoinDropChance)
                return 0;

            return RangeInclusive(random, CoinMin, CoinMax);
        }

        private static void TryAddDrop(LootEntry drop, IRandomSource random, List<ItemStack> items)
        {
            if (drop.Item == null || random.Next01() > drop.DropChance)
                return;

            var amount = RangeInclusive(random, drop.MinAmount, drop.MaxAmount);
            items.Add(new ItemStack(drop.Item, amount));
        }

        private static int RangeInclusive(IRandomSource random, int min, int max)
        {
            min = Mathf.Max(0, min);
            max = Mathf.Max(min, max);
            if (min == max)
                return min;

            var normalized = Mathf.Clamp01(random.Next01());
            var offset = Mathf.Min(max - min, Mathf.FloorToInt(normalized * (max - min + 1)));
            return min + offset;
        }
    }
}
