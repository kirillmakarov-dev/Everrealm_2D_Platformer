using System.Collections.Generic;
using LetterHunter.Core;
using LetterHunter.Effects;
using LetterHunter.Feedback;
using UnityEngine;

namespace LetterHunter.Skills
{
    [CreateAssetMenu(menuName = "Letter Hunter/Skills/Skill Definition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        [SerializeField] private string skillId;
        [SerializeField] private string displayName;
        [Header("UI")]
        [SerializeField] private Sprite icon;
        [SerializeField] private string shortName;
        [SerializeField] private string inputLabel;
        [Header("Gameplay")]
        [TextArea, SerializeField] private string description;
        [SerializeField] private CharacterClassType classType;
        [SerializeField] private SkillType skillType;
        [Min(0f), SerializeField] private float manaCost;
        [Min(0f), SerializeField] private float cooldown;
        [Min(0f), SerializeField] private float duration;
        [Min(0f), SerializeField] private float baseDamageMultiplier = 1f;
        [Min(1), SerializeField] private int damageLines = 1;
        [Min(1), SerializeField] private int maxTargets = 1;
        [Min(1), SerializeField] private int rank = 1;
        [SerializeField] private List<string> tags = new();
        [SerializeField] private SkillEffectDefinition effect;
        [SerializeField] private AttackImpactProfile impactProfile;

        public string SkillId => string.IsNullOrWhiteSpace(skillId) ? name : skillId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public Sprite Icon => icon;
        public string ShortName => string.IsNullOrWhiteSpace(shortName) ? displayName : shortName;
        public string InputLabel => inputLabel;
        public string Description => description;
        public CharacterClassType ClassType => classType;
        public SkillType SkillType => skillType;
        public float ManaCost => manaCost;
        public float Cooldown => cooldown;
        public float Duration => duration;
        public float BaseDamageMultiplier => baseDamageMultiplier;
        public int DamageLines => damageLines;
        public int MaxTargets => maxTargets;
        public int Rank => rank;
        public IReadOnlyList<string> Tags => tags;
        public SkillEffectDefinition Effect => effect;
        public AttackImpactProfile ImpactProfile => impactProfile;
    }
}
