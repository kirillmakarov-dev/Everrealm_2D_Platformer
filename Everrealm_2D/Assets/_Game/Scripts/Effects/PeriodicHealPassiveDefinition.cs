using LetterHunter.Combat;
using UnityEngine;

namespace LetterHunter.Effects
{
    [CreateAssetMenu(menuName = "Letter Hunter/Effects/Passives/Periodic Heal")]
    public sealed class PeriodicHealPassiveDefinition : PassiveSkillEffectDefinition
    {
        [Min(0.01f), SerializeField] private float interval = 5f;
        [Min(0f), SerializeField] private float healAmount = 5f;
        public override string Describe(LetterHunter.Skills.SkillDefinition skill) => $"Restores {healAmount:0.##} health every {interval:0.##} s.";
        public override IPassiveEffect CreateRuntime() => new Runtime(interval, healAmount);

        private sealed class Runtime : IPassiveEffect
        {
            private readonly float _interval;
            private readonly float _healAmount;
            private float _remaining;
            public Runtime(float interval, float healAmount) { _interval = interval; _healAmount = healAmount; }
            public void OnRegistered(PassiveContext context) => _remaining = _interval;
            public void Tick(PassiveContext context, float deltaTime)
            {
                _remaining -= deltaTime;
                if (_remaining > 0f) return;
                context.Owner.Stats.Heal(_healAmount);
                _remaining += _interval;
            }
            public void ModifyAutoAttack(PassiveContext context, AutoAttackSpec attack) { }
        }
    }
}
