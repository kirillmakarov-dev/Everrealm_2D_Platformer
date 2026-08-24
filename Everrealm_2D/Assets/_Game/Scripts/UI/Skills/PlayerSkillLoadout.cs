using System;
using System.Collections.Generic;
using LetterHunter.Characters;
using LetterHunter.Skills;
using UnityEngine;

namespace LetterHunter.UI.Skills
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerClassController))]
    public sealed class PlayerSkillLoadout : MonoBehaviour
    {
        [SerializeField] private List<SkillSlotBinding> slots = new();
        [SerializeField] private string[] defaultInputLabels = { "1", "2", "3", "4" };

        public IReadOnlyList<SkillSlotBinding> Slots => slots;
        public event Action LoadoutChanged;

        public bool TryAssignSkill(int slotIndex, SkillDefinition skill, out string failure)
        {
            failure = null;
            if (slotIndex < 0)
            {
                failure = "Invalid slot.";
                return false;
            }

            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId))
            {
                failure = "Skill is missing.";
                return false;
            }

            for (var i = 0; i < slots.Count; i++)
            {
                var binding = slots[i];
                if (binding == null || binding.Skill == null)
                    continue;
                if (binding.SlotIndex != slotIndex && binding.Skill.SkillId == skill.SkillId)
                {
                    failure = $"{skill.DisplayName} is already on the skill bar.";
                    return false;
                }
            }

            var inputLabel = ResolveInputLabel(slotIndex, skill, null);
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null || slots[i].SlotIndex != slotIndex)
                    continue;

                slots[i] = new SkillSlotBinding(slotIndex, skill, inputLabel);
                LoadoutChanged?.Invoke();
                return true;
            }

            slots.Add(new SkillSlotBinding(slotIndex, skill, inputLabel));
            LoadoutChanged?.Invoke();
            return true;
        }

        public bool TrySwapSlots(int firstIndex, int secondIndex, SkillDefinition firstSkill,
            SkillDefinition secondSkill, out string failure)
        {
            failure = null;
            if (firstIndex < 0 || secondIndex < 0)
            {
                failure = "Invalid slot.";
                return false;
            }
            if (firstIndex == secondIndex)
                return true;

            slots.RemoveAll(binding => binding != null &&
                (binding.SlotIndex == firstIndex || binding.SlotIndex == secondIndex));
            slots.Add(new SkillSlotBinding(firstIndex, secondSkill,
                ResolveInputLabel(firstIndex, secondSkill, null)));
            slots.Add(new SkillSlotBinding(secondIndex, firstSkill,
                ResolveInputLabel(secondIndex, firstSkill, null)));
            LoadoutChanged?.Invoke();
            return true;
        }

        public IReadOnlyList<SkillSlotBinding> BuildRuntimeSlots(PlayerClassController player, int maxSlots)
        {
            var result = new List<SkillSlotBinding>(maxSlots);

            for (var i = 0; i < maxSlots; i++)
            {
                var skill = ResolveSkill(player, i, out var inputLabel);
                result.Add(new SkillSlotBinding(i, skill, inputLabel));
            }

            return result;
        }

        public SkillDefinition ResolveSkill(PlayerClassController player, int slotIndex, out string inputLabel)
        {
            var explicitBinding = FindBinding(slotIndex);
            if (explicitBinding != null)
            {
                inputLabel = ResolveInputLabel(slotIndex, explicitBinding.Skill, explicitBinding.InputLabel);
                return explicitBinding.Skill;
            }

            var skill = player != null && slotIndex >= 0 && slotIndex < player.UsableSkills.Count
                ? player.UsableSkills[slotIndex]
                : null;
            inputLabel = ResolveInputLabel(slotIndex, skill, null);
            return skill;
        }

        public void SetSlots(IEnumerable<SkillSlotBinding> bindings)
        {
            slots.Clear();
            if (bindings == null)
            {
                LoadoutChanged?.Invoke();
                return;
            }

            foreach (var binding in bindings)
                if (binding != null)
                    slots.Add(binding);
            LoadoutChanged?.Invoke();
        }

        public void ClearSlots()
        {
            if (slots.Count == 0)
                return;
            slots.Clear();
            LoadoutChanged?.Invoke();
        }

        public bool RemoveSkill(string skillId, bool notify = true)
        {
            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            var removed = slots.RemoveAll(binding => binding != null && binding.Skill != null &&
                binding.Skill.SkillId == skillId) > 0;
            if (removed && notify)
                LoadoutChanged?.Invoke();
            return removed;
        }

        private SkillSlotBinding FindBinding(int slotIndex)
        {
            foreach (var binding in slots)
                if (binding != null && binding.SlotIndex == slotIndex)
                    return binding;

            return null;
        }

        private string ResolveInputLabel(int index, SkillDefinition skill, string overrideLabel)
        {
            if (!string.IsNullOrWhiteSpace(overrideLabel)) return overrideLabel;
            if (skill != null && !string.IsNullOrWhiteSpace(skill.InputLabel)) return skill.InputLabel;
            if (index == 9) return "0";
            return index >= 0 && index < defaultInputLabels.Length ? defaultInputLabels[index] : (index + 1).ToString();
        }
    }
}
