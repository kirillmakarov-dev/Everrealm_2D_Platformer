using System.Collections.Generic;
using LetterHunter.Core;
using LetterHunter.Skills;
using LetterHunter.Stats;
using UnityEngine;

namespace LetterHunter.Classes
{
    [CreateAssetMenu(menuName = "Letter Hunter/Classes/Class Definition")]
    public sealed class ClassDefinition : ScriptableObject
    {
        [SerializeField] private CharacterClassType classType;
        [SerializeField] private string displayName;
        [SerializeField] private CombatStatsData baseStats = default;
        [SerializeField] private List<SkillDefinition> startingSkills = new();
        public CharacterClassType ClassType => classType;
        public string DisplayName => displayName;
        public CombatStatsData BaseStats => baseStats.maxHealth > 0f ? baseStats : CombatStatsData.Default;
        public IReadOnlyList<SkillDefinition> StartingSkills => startingSkills;
    }
}
