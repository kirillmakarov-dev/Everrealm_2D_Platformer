using LetterHunter.Combat;
using LetterHunter.Core;
using LetterHunter.Feedback;
using LetterHunter.Skills;
using UnityEngine;

namespace LetterHunter.Effects
{
    [CreateAssetMenu(menuName = "Letter Hunter/Effects/Auto Attack Upgrade")]
    public sealed class AutoAttackUpgradeEffectDefinition : SkillEffectDefinition
    {
        [Min(0f), SerializeField] private float damageMultiplier = 1f;
        [Min(1), SerializeField] private int damageLines = 3;
        [Min(1), SerializeField] private int maxTargets = 1;
        [SerializeField] private AttackShape shape;
        [SerializeField] private DamageTag tags = DamageTag.AutoAttack;
        [SerializeField] private AttackImpactProfile impactProfile;

        public override void Apply(SkillContext context) => context.AutoAttackService.SetBaseProfile(new AutoAttackSpec
        {
            DamageMultiplier = damageMultiplier, DamageLines = damageLines, MaxTargets = maxTargets,
            Shape = shape, Tags = tags, SourceSkillId = context.SkillDefinition.SkillId,
            ImpactProfile = context.SkillDefinition.ImpactProfile != null ? context.SkillDefinition.ImpactProfile : impactProfile
        });
    }
}
