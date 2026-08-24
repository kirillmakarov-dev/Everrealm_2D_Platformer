using LetterHunter.Combat;
using LetterHunter.Core;
using LetterHunter.Feedback;
using LetterHunter.Skills;
using UnityEngine;

namespace LetterHunter.Effects
{
    [CreateAssetMenu(menuName = "Letter Hunter/Effects/Empower")]
    public sealed class EmpowerEffectDefinition : SkillEffectDefinition, IEmpowerModifier
    {
        [Min(0f), SerializeField] private float damageMultiplier = 1f;
        [Min(1), SerializeField] private int damageLines = 1;
        [Min(1), SerializeField] private int maxTargets = 1;
        [SerializeField] private AttackShape attackShape = default;
        [SerializeField] private DamageTag additionalTags;
        [SerializeField] private AttackImpactProfile impactProfile;
        public override void Apply(SkillContext context)
        {
            context.EmpowerState.Set(context.SkillDefinition.SkillId, this,
                context.SkillDefinition.ImpactProfile != null ? context.SkillDefinition.ImpactProfile : impactProfile);
        }

        public void Modify(AutoAttackSpec attack)
        {
            attack.DamageMultiplier *= damageMultiplier;
            attack.DamageLines = damageLines;
            attack.MaxTargets = maxTargets;
            if (attackShape.radius > 0f || attackShape.size.sqrMagnitude > 0f) attack.Shape = attackShape;
            attack.Tags |= additionalTags;
            if (impactProfile != null) attack.ImpactProfile = impactProfile;
        }
    }
}
