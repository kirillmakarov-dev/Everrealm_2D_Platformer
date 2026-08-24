using LetterHunter.Skills;
using UnityEngine;

namespace LetterHunter.Effects
{
    [CreateAssetMenu(menuName = "Letter Hunter/Effects/Stat Buff")]
    public sealed class BuffEffectDefinition : SkillEffectDefinition
    {
        [SerializeField] private BuffStat stat;
        [SerializeField] private float amount = 5f;

        public override void Apply(SkillContext context) => context.BuffService.Apply(
            context.SkillDefinition.SkillId, context.Caster, stat, amount, context.SkillDefinition.Duration,
            context.SkillDefinition.ImpactProfile);
    }
}
