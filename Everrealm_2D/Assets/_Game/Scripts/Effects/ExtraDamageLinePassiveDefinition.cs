using LetterHunter.Combat;
using UnityEngine;

namespace LetterHunter.Effects
{
    [CreateAssetMenu(menuName = "Letter Hunter/Effects/Passives/Extra Damage Line")]
    public sealed class ExtraDamageLinePassiveDefinition : PassiveSkillEffectDefinition
    {
        [Range(0f, 1f), SerializeField] private float chance = 0.2f;
        [Min(1), SerializeField] private int extraLines = 1;
        public override IPassiveEffect CreateRuntime() => new Runtime(chance, extraLines);

        private sealed class Runtime : IPassiveEffect
        {
            private readonly float _chance;
            private readonly int _extraLines;
            public Runtime(float chance, int extraLines) { _chance = chance; _extraLines = extraLines; }
            public void OnRegistered(PassiveContext context) { }
            public void Tick(PassiveContext context, float deltaTime) { }
            public void ModifyAutoAttack(PassiveContext context, AutoAttackSpec attack)
            { if (context.Random.Next01() <= _chance) attack.DamageLines += _extraLines; }
        }
    }
}
