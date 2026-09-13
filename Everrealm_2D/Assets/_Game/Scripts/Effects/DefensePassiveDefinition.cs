using LetterHunter.Combat;
using UnityEngine;

namespace LetterHunter.Effects
{
    [CreateAssetMenu(menuName = "Everrealm/Effects/Passives/Defense")]
    public sealed class DefensePassiveDefinition : PassiveSkillEffectDefinition
    {
        [Min(0f), SerializeField] private float defenseBonus = 5f;
        public override string Describe(LetterHunter.Skills.SkillDefinition skill) => $"Permanent defense bonus: +{defenseBonus:0.##}.";
        public override IPassiveEffect CreateRuntime() => new Runtime(defenseBonus);

        private sealed class Runtime : IPassiveEffect
        {
            private readonly float _bonus;
            public Runtime(float bonus) => _bonus = bonus;
            public void OnRegistered(PassiveContext context) => context.Owner.Stats.SetDefenseModifier(this, _bonus);
            public void Tick(PassiveContext context, float deltaTime) { }
            public void ModifyAutoAttack(PassiveContext context, AutoAttackSpec attack) { }
        }
    }
}
