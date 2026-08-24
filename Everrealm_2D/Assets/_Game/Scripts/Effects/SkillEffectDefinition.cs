using LetterHunter.Skills;
using UnityEngine;

namespace LetterHunter.Effects
{
    public abstract class SkillEffectDefinition : ScriptableObject, ISkillEffect
    {
        public abstract void Apply(SkillContext context);
    }
}
