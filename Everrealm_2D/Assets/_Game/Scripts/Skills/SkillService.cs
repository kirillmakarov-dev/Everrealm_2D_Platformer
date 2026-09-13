using System;
using System.Collections.Generic;
using LetterHunter.Combat;
using LetterHunter.Core;
using LetterHunter.Effects;
using UnityEngine;

namespace LetterHunter.Skills
{
    public enum SkillUseFailure { None, NotRegistered, NoEffect, OnCooldown, NotEnoughMana, CasterDead, NotProjectileSkill }

    public readonly struct SkillUseResult
    {
        private SkillUseResult(bool success, SkillUseFailure failure) { Success = success; Failure = failure; }
        public bool Success { get; }
        public SkillUseFailure Failure { get; }
        public static SkillUseResult Succeeded() => new(true, SkillUseFailure.None);
        public static SkillUseResult Failed(SkillUseFailure failure) => new(false, failure);
    }

    public sealed class SkillService
    {
        private readonly ICombatActor _owner;
        private readonly ICombatService _combat;
        private readonly BuffService _buffs;
        private readonly EmpowerState _empower;
        private readonly AutoAttackService _autoAttack;
        private readonly ITargetProvider _targets;
        private readonly PassiveService _passives;
        private readonly Dictionary<string, SkillDefinition> _definitions = new();
        private readonly Dictionary<string, SkillRuntimeState> _states = new();
        private readonly Dictionary<string, SkillRuntimeModifiers> _progressionModifiers = new();

        public SkillService(ICombatActor owner, ICombatService combat, BuffService buffs,
            EmpowerState empower, AutoAttackService autoAttack, ITargetProvider targets, PassiveService passives)
        { _owner = owner; _combat = combat; _buffs = buffs; _empower = empower; _autoAttack = autoAttack; _targets = targets; _passives = passives; }

        public void Register(SkillDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.SkillId)) return;
            _definitions[definition.SkillId] = definition;
            _states[definition.SkillId] = new SkillRuntimeState();
            if (definition.Effect is PassiveSkillEffectDefinition passive) _passives.Register(passive);
            if (definition.SkillType == LetterHunter.Core.SkillType.AutoAttackUpgrade && definition.Effect != null)
                definition.Effect.Apply(CreateContext(definition, null, Vector2.right));
        }

        public SkillUseResult TryUse(string skillId, IDamageable target, Vector2 direction)
        {
            if (!_definitions.TryGetValue(skillId, out var definition)) return SkillUseResult.Failed(SkillUseFailure.NotRegistered);
            if (definition.Effect == null) return SkillUseResult.Failed(SkillUseFailure.NoEffect);
            if (!_owner.IsAlive) return SkillUseResult.Failed(SkillUseFailure.CasterDead);
            var state = _states[skillId];
            var values = GetRuntimeValues(definition);
            if (!state.IsReady) return SkillUseResult.Failed(SkillUseFailure.OnCooldown);
            if (!_owner.Stats.TrySpendMana(values.ManaCost)) return SkillUseResult.Failed(SkillUseFailure.NotEnoughMana);

            var context = CreateContext(definition, target, direction);
            definition.Effect.Apply(context);
            state.StartCooldown(values.Cooldown);
            state.StartDuration(definition.Duration);
            return SkillUseResult.Succeeded();
        }

        public SkillUseResult TryPrepareProjectile(string skillId, Vector2 direction,
            Vector2 position, out PreparedSkillCast cast)
        {
            cast = null;
            var validation = CanPrepareProjectile(skillId);
            if (!validation.Success)
                return validation;

            var definition = _definitions[skillId];
            var state = _states[skillId];
            var values = GetRuntimeValues(definition);
            if (!_owner.Stats.TrySpendMana(values.ManaCost))
                return SkillUseResult.Failed(SkillUseFailure.NotEnoughMana);

            state.StartCooldown(values.Cooldown);
            cast = new PreparedSkillCast(definition, values, position,
                direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right);
            return SkillUseResult.Succeeded();
        }

        public SkillUseResult CanPrepareProjectile(string skillId)
        {
            if (!_definitions.TryGetValue(skillId, out var definition))
                return SkillUseResult.Failed(SkillUseFailure.NotRegistered);
            if (definition.SkillType == SkillType.Empower)
                return SkillUseResult.Failed(SkillUseFailure.NotProjectileSkill);
            if (definition.Effect == null)
                return SkillUseResult.Failed(SkillUseFailure.NoEffect);
            if (!_owner.IsAlive)
                return SkillUseResult.Failed(SkillUseFailure.CasterDead);

            if (!_states[skillId].IsReady)
                return SkillUseResult.Failed(SkillUseFailure.OnCooldown);
            if (_owner.Stats.CurrentMana < GetRuntimeValues(definition).ManaCost)
                return SkillUseResult.Failed(SkillUseFailure.NotEnoughMana);

            return SkillUseResult.Succeeded();
        }

        public SkillUseResult ResolveProjectileHit(PreparedSkillCast cast, IDamageable target)
        {
            if (cast == null || cast.Consumed || target == null || !target.IsAlive)
                return SkillUseResult.Failed(SkillUseFailure.NoEffect);

            cast.Consumed = true;
            var context = CreateContext(cast.Definition, target, cast.Direction, cast.Position, cast.RuntimeValues);
            if (cast.Definition.Effect is not DamageSkillEffectDefinition)
                ApplyProjectileDamage(cast, target);
            cast.Definition.Effect.Apply(context);
            _states[cast.Definition.SkillId].StartDuration(cast.Definition.Duration);
            return SkillUseResult.Succeeded();
        }

        private void ApplyProjectileDamage(PreparedSkillCast cast, IDamageable target)
        {
            var values = cast.RuntimeValues;
            var tags = cast.Definition.ProjectileDamageTags;
            var lines = new DamageLine[Math.Max(1, values.DamageLines)];
            for (var i = 0; i < lines.Length; i++)
                lines[i] = new DamageLine(values.DamageMultiplier, tags);

            _combat.ApplyDamage(new DamageRequest(_owner, target, _owner.Stats.AttackPower,
                lines, cast.Definition.SkillId, tags, cast.Definition.ImpactProfile,
                cast.Direction));
        }

        public void Tick(float deltaTime)
        {
            foreach (var state in _states.Values) state.Tick(deltaTime);
        }

        public IReadOnlyCollection<SkillDefinition> Definitions => _definitions.Values;

        public bool TryGetRuntimeState(string skillId, out SkillRuntimeState state) =>
            _states.TryGetValue(skillId, out state);

        public void SetProgressionModifiers(string skillId, SkillRuntimeModifiers modifiers)
        {
            if (!string.IsNullOrWhiteSpace(skillId))
                _progressionModifiers[skillId] = modifiers;
        }

        public void ClearProgressionModifiers() => _progressionModifiers.Clear();

        public SkillRuntimeValues GetRuntimeValues(SkillDefinition definition)
        {
            if (definition == null)
                return new SkillRuntimeValues(null, default);
            _progressionModifiers.TryGetValue(definition.SkillId, out var modifiers);
            return new SkillRuntimeValues(definition, modifiers);
        }

        private SkillContext CreateContext(SkillDefinition definition, IDamageable target, Vector2 direction) =>
            CreateContext(definition, target, direction, _owner.Transform.position, GetRuntimeValues(definition));

        private SkillContext CreateContext(SkillDefinition definition, IDamageable target, Vector2 direction,
            Vector2 position, SkillRuntimeValues runtimeValues) =>
            new(_owner, target, definition, _combat, _buffs, _empower, _autoAttack, _targets,
                runtimeValues, position, direction);
    }

    public sealed class PreparedSkillCast
    {
        internal PreparedSkillCast(SkillDefinition definition, SkillRuntimeValues runtimeValues,
            Vector2 position, Vector2 direction)
        {
            Definition = definition;
            RuntimeValues = runtimeValues;
            Position = position;
            Direction = direction;
        }

        public SkillDefinition Definition { get; }
        public SkillRuntimeValues RuntimeValues { get; }
        public Vector2 Position { get; }
        public Vector2 Direction { get; }
        internal bool Consumed { get; set; }
    }
}
