using System.Collections.Generic;
using UnityEngine;

namespace LetterHunter.Skills
{
    [CreateAssetMenu(menuName = "Everrealm/Databases/Skill Database")]
    public sealed class SkillDatabase : ScriptableObject
    {
        [SerializeField] private List<SkillDefinition> skills = new();

        public IReadOnlyList<SkillDefinition> Skills => skills;

        public bool TryGetSkill(string skillId, out SkillDefinition skill)
        {
            skill = null;
            if (string.IsNullOrWhiteSpace(skillId))
                return false;

            foreach (var candidate in skills)
            {
                if (candidate == null || candidate.SkillId != skillId)
                    continue;

                skill = candidate;
                return true;
            }

            return false;
        }
    }
}
