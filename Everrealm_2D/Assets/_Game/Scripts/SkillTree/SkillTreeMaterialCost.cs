using System;
using LetterHunter.Items;
using UnityEngine;

namespace LetterHunter.SkillTree
{
    [Serializable]
    public struct SkillTreeMaterialCost
    {
        [SerializeField] private ItemDefinition item;
        [Min(1), SerializeField] private int amount;

        public SkillTreeMaterialCost(ItemDefinition item, int amount)
        {
            this.item = item;
            this.amount = Mathf.Max(1, amount);
        }

        public ItemDefinition Item => item;
        public int Amount => Mathf.Max(1, amount);
        public bool IsValid => item != null && Amount > 0;
    }
}
