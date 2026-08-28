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
            if (context.Target != null)
            {
                ApplyDamage(context, context.Target, definition, values);
                return;
            }

            var targets = context.TargetProvider.FindTargets(new TargetingQuery(context.Position, context.Direction,
                attackShape, values.MaxTargets, context.Caster));
            foreach (var target in targets)
                ApplyDamage(context, target, definition, values);
        }

        private void ApplyDamage(SkillContext context, IDamageable target, SkillDefinition definition,
            SkillRuntimeValues values)
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
