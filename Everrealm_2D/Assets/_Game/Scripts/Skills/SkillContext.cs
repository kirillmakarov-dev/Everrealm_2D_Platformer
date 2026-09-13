using LetterHunter.Combat;
using LetterHunter.Effects;
using UnityEngine;

namespace LetterHunter.Skills
{
    public readonly struct SkillContext
    {
        public SkillContext(ICombatActor caster, IDamageable target, SkillDefinition definition,
            ICombatService combatService, BuffService buffService, EmpowerState empowerState,
            AutoAttackService autoAttackService, ITargetProvider targetProvider, SkillRuntimeValues runtimeValues,
            Vector2 position, Vector2 direction, Vector2? impactPosition = null)
        {
            Caster = caster; Target = target; SkillDefinition = definition; CombatService = combatService;
            BuffService = buffService; EmpowerState = empowerState; AutoAttackService = autoAttackService;
            TargetProvider = targetProvider; Position = position; Direction = direction;
            RuntimeValues = runtimeValues;
            ImpactPosition = impactPosition;
        }

        public ICombatActor Caster { get; }
        public IDamageable Target { get; }
        public SkillDefinition SkillDefinition { get; }
        public ICombatService CombatService { get; }
        public BuffService BuffService { get; }
        public EmpowerState EmpowerState { get; }
        public AutoAttackService AutoAttackService { get; }
        public ITargetProvider TargetProvider { get; }
        public SkillRuntimeValues RuntimeValues { get; }
        public Vector2 Position { get; }
        public Vector2 Direction { get; }
        public Vector2? ImpactPosition { get; }
    }

    public interface ISkillEffect { void Apply(SkillContext context); }
}
