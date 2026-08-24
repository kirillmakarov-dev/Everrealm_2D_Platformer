using System;
using LetterHunter.Skills;
using UnityEngine;

namespace LetterHunter.UI.Skills
{
    [Serializable]
    public sealed class SkillSlotBinding
    {
        [Min(0), SerializeField] private int slotIndex;
        [SerializeField] private SkillDefinition skill;
        [SerializeField] private string inputLabel;

        public int SlotIndex => slotIndex;
        public SkillDefinition Skill => skill;
        public string InputLabel => inputLabel;

        public SkillSlotBinding() { }

        public SkillSlotBinding(int slotIndex, SkillDefinition skill, string inputLabel)
        {
            this.slotIndex = Mathf.Max(0, slotIndex);
            this.skill = skill;
            this.inputLabel = inputLabel;
        }
    }
}
