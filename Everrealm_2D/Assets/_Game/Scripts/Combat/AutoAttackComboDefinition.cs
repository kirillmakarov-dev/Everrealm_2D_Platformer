using System;
using System.Collections.Generic;
using LetterHunter.Feedback;
using UnityEngine;

namespace LetterHunter.Combat
{
    [CreateAssetMenu(menuName = "Everrealm/Combat/Auto Attack Combo")]
    public sealed class AutoAttackComboDefinition : ScriptableObject
    {
        [SerializeField] private string comboId = "basic_combo";
        [Min(0.05f), SerializeField] private float resetTime = 0.85f;
        [SerializeField] private AutoAttackComboStep[] steps =
        {
            new("Hit 1", 1f, 0f, 1.5f, null),
            new("Hit 2", 1f, 0f, 1.5f, null),
            new("Finisher", 1f, 0.25f, 1.5f, null)
        };

        public string ComboId => string.IsNullOrWhiteSpace(comboId) ? name : comboId;
        public float ResetTime => Mathf.Max(0.05f, resetTime);
        public IReadOnlyList<AutoAttackComboStep> Steps => steps;
        public int StepCount => steps != null ? steps.Length : 0;

        public AutoAttackComboStep GetStep(int index)
        {
            if (steps == null || steps.Length == 0) return AutoAttackComboStep.Default;
            return steps[Mathf.Clamp(index, 0, steps.Length - 1)];
        }
    }

    [Serializable]
    public struct AutoAttackComboStep
    {
        public static readonly AutoAttackComboStep Default = new("Hit", 1f, 0f, 1.5f, null);

        public AutoAttackComboStep(string displayName, float damageMultiplier, float criticalChance,
            float criticalDamageMultiplier, AttackImpactProfile impactProfile)
        {
            this.displayName = displayName;
            this.damageMultiplier = Mathf.Max(0f, damageMultiplier);
            this.criticalChance = Mathf.Clamp01(criticalChance);
            this.criticalDamageMultiplier = Mathf.Max(1f, criticalDamageMultiplier);
            this.impactProfile = impactProfile;
        }

        [SerializeField] private string displayName;
        [Min(0f), SerializeField] private float damageMultiplier;
        [Range(0f, 1f), SerializeField] private float criticalChance;
        [Min(1f), SerializeField] private float criticalDamageMultiplier;
        [SerializeField] private AttackImpactProfile impactProfile;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Hit" : displayName;
        public float DamageMultiplier => damageMultiplier <= 0f ? 1f : damageMultiplier;
        public float CriticalChance => Mathf.Clamp01(criticalChance);
        public float CriticalDamageMultiplier => Mathf.Max(1f, criticalDamageMultiplier);
        public AttackImpactProfile ImpactProfile => impactProfile;
    }
}
