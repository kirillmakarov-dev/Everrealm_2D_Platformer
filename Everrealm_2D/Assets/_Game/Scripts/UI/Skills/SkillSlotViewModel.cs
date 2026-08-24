using LetterHunter.Skills;
using UnityEngine;

namespace LetterHunter.UI.Skills
{
    public readonly struct SkillSlotViewModel
    {
        public SkillSlotViewModel(int index, SkillDefinition skill, SkillRuntimeState state,
            bool canAffordMana, string inputLabel, float effectiveCooldown = -1f)
        {
            Index = index;
            Skill = skill;
            State = state;
            CanAffordMana = canAffordMana;
            InputLabel = inputLabel ?? string.Empty;
            EffectiveCooldown = effectiveCooldown;
        }

        public int Index { get; }
        public SkillDefinition Skill { get; }
        public SkillRuntimeState State { get; }
        public bool CanAffordMana { get; }
        public string InputLabel { get; }
        public float EffectiveCooldown { get; }
        public bool HasSkill => Skill != null;
        public bool IsReady => State == null || State.IsReady;
        public float CooldownRemaining => State?.CooldownRemaining ?? 0f;
        public float CooldownTotal => Skill != null
            ? Mathf.Max(0.01f, EffectiveCooldown >= 0f ? EffectiveCooldown : Skill.Cooldown)
            : 0.01f;
        public float CooldownFill => HasSkill ? Mathf.Clamp01(CooldownRemaining / CooldownTotal) : 0f;
    }
}
