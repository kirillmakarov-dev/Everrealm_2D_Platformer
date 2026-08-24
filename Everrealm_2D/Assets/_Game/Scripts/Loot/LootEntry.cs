using System;
using LetterHunter.Items;
using UnityEngine;

namespace LetterHunter.Loot
{
    [Serializable]
    public struct LootEntry
    {
        [SerializeField] private ItemDefinition item;
        [Range(0f, 1f), SerializeField] private float dropChance;
        [Min(1), SerializeField] private int minAmount;
        [Min(1), SerializeField] private int maxAmount;

        public ItemDefinition Item => item;
        public float DropChance => Mathf.Clamp01(dropChance);
        public int MinAmount => Mathf.Max(1, minAmount);
        public int MaxAmount => Mathf.Max(MinAmount, maxAmount);
    }
}
