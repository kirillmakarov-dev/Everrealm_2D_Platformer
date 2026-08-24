using System;
using System.Collections.Generic;
using LetterHunter.Combat;
using LetterHunter.Skills;

namespace LetterHunter.Effects
{
    public readonly struct PassiveContext
    {
        public PassiveContext(ICombatActor owner, IRandomSource random) { Owner = owner; Random = random; }
        public ICombatActor Owner { get; }
        public IRandomSource Random { get; }
    }

    public interface IRandomSource { float Next01(); }
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;
        public SystemRandomSource(int? seed = null) => _random = seed.HasValue ? new Random(seed.Value) : new Random();
        public float Next01() => (float)_random.NextDouble();
    }

    public interface IPassiveEffect
    {
        void OnRegistered(PassiveContext context);
        void Tick(PassiveContext context, float deltaTime);
        void ModifyAutoAttack(PassiveContext context, AutoAttackSpec attack);
    }

    public abstract class PassiveSkillEffectDefinition : SkillEffectDefinition
    {
        public override void Apply(SkillContext context) { }
        public abstract IPassiveEffect CreateRuntime();
    }

    public sealed class PassiveService
    {
        private readonly PassiveContext _context;
        private readonly List<IPassiveEffect> _effects = new();
        public PassiveService(ICombatActor owner, IRandomSource random = null) =>
            _context = new PassiveContext(owner, random ?? new SystemRandomSource());

        public void Register(PassiveSkillEffectDefinition definition)
        {
            var effect = definition != null ? definition.CreateRuntime() : null;
            if (effect == null || _effects.Contains(effect)) return;
            _effects.Add(effect);
            effect.OnRegistered(_context);
        }

        public void Tick(float deltaTime)
        {
            foreach (var effect in _effects) effect.Tick(_context, deltaTime);
        }

        public void ModifyAutoAttack(AutoAttackSpec attack)
        {
            foreach (var effect in _effects) effect.ModifyAutoAttack(_context, attack);
        }
    }
}
