using LetterHunter.Skills;
using UnityEngine;

namespace LetterHunter.Effects
{
    public abstract class SkillEffectDefinition : ScriptableObject, ISkillEffect
    {
        public virtual string Describe(SkillDefinition skill) => "Effect details are not configured.";
        public abstract void Apply(SkillContext context);
    }
}
