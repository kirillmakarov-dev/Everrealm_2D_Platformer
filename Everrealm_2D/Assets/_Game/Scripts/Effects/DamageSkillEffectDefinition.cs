using System;
using LetterHunter.Combat;
using LetterHunter.Core;
using LetterHunter.Feedback;
using LetterHunter.Skills;
using UnityEngine;

namespace LetterHunter.Effects
{
    [CreateAssetMenu(menuName = "Letter Hunter/Effects/Area Damage")]
    public sealed class DamageSkillEffectDefinition : SkillEffectDefinition
    {
        [SerializeField] private AttackShape attackShape;
        [SerializeField] private DamageTag damageTags = DamageTag.Skill;
        [SerializeField] private AttackImpactProfile impactProfile;

        public override void Apply(SkillContext context)
        {
            var definition = context.SkillDefinition;
            var values = context.RuntimeValues;
            var targets = context.TargetProvider.FindTargets(new TargetingQuery(context.Position, context.Direction,
                attackShape, values.MaxTargets, context.Caster));
            foreach (var target in targets)
            {
                var lines = new DamageLine[Math.Max(1, values.DamageLines)];
                for (var i = 0; i < lines.Length; i++) lines[i] = new DamageLine(values.DamageMultiplier, damageTags);
                context.CombatService.ApplyDamage(new DamageRequest(context.Caster, target,
                    context.Caster.Stats.AttackPower, lines, definition.SkillId, damageTags,
                    definition.ImpactProfile != null ? definition.ImpactProfile : impactProfile,
                    context.Direction));
            }
        }
    }
}
