using System;
using System.Collections.Generic;
using LetterHunter.Core;
using LetterHunter.Effects;
using LetterHunter.Feedback;

namespace LetterHunter.Combat
{
    public sealed class AutoAttackSpec
    {
        public float DamageMultiplier { get; set; } = 1f;
        public int DamageLines { get; set; } = 1;
        public int MaxTargets { get; set; } = 1;
        public AttackShape Shape { get; set; } = AttackShape.MeleeDefault;
        public DamageTag Tags { get; set; } = DamageTag.AutoAttack | DamageTag.Melee;
        public string SourceSkillId { get; set; } = "auto_attack";
        public AttackImpactProfile ImpactProfile { get; set; }
        public float CriticalChance { get; set; }
        public float CriticalDamageMultiplier { get; set; } = 1.5f;
    }

    public interface IEmpowerModifier { void Modify(AutoAttackSpec attack); }

    public sealed class EmpowerState
    {
        public IEmpowerModifier ActiveModifier { get; private set; }
        public string SourceSkillId { get; private set; }
        public AttackImpactProfile ImpactProfile { get; private set; }
        public bool VisualConsumed { get; private set; }
        public bool IsActive => ActiveModifier != null;
        public void Set(string sourceSkillId, IEmpowerModifier modifier, AttackImpactProfile impactProfile = null)
        {
            SourceSkillId = sourceSkillId;
            ActiveModifier = modifier;
            ImpactProfile = impactProfile;
            VisualConsumed = false;
        }

        public bool TryConsumeVisual()
        {
            if (!IsActive || VisualConsumed) return false;
            VisualConsumed = true;
            return true;
        }

        public void Clear()
        {
            SourceSkillId = null;
            ActiveModifier = null;
            ImpactProfile = null;
            VisualConsumed = false;
        }
    }

    public sealed class AutoAttackService
    {
        private readonly ICombatActor _owner;
        private readonly ICombatService _combat;
        private readonly ITargetProvider _targets;
        private readonly EmpowerState _empower;
        private readonly BuffService _buffs;
        private readonly PassiveService _passives;
        private AutoAttackSpec _baseProfile = new();
        private AutoAttackComboDefinition _comboDefinition;
        private int _nextComboStepIndex;
        private int _lastComboStepIndex = -1;
        private float _lastSuccessfulAttackTime = -999f;
        private AutoAttackComboStep _lastComboStep = AutoAttackComboStep.Default;

        public AutoAttackService(ICombatActor owner, ICombatService combat, ITargetProvider targets,
            EmpowerState empower, BuffService buffs, PassiveService passives)
        { _owner = owner; _combat = combat; _targets = targets; _empower = empower; _buffs = buffs; _passives = passives; }

        public int LastComboStepIndex => _lastComboStepIndex;
        public int LastComboStepNumber => _lastComboStepIndex >= 0 ? _lastComboStepIndex + 1 : 0;
        public int ComboStepCount => _comboDefinition != null ? _comboDefinition.StepCount : 0;
        public AutoAttackComboStep LastComboStep => _lastComboStep;

        public void SetBaseProfile(AutoAttackSpec profile)
        {
            profile ??= new AutoAttackSpec();
            if (profile.ImpactProfile == null)
                profile.ImpactProfile = _baseProfile.ImpactProfile;

            _baseProfile = profile;
        }

        public void SetComboDefinition(AutoAttackComboDefinition comboDefinition)
        {
            _comboDefinition = comboDefinition;
            _nextComboStepIndex = 0;
            _lastComboStepIndex = -1;
            _lastSuccessfulAttackTime = -999f;
            _lastComboStep = AutoAttackComboStep.Default;
        }

        public IReadOnlyList<DamageResult> Execute(UnityEngine.Vector2 direction)
        {
            if (!_owner.IsAlive) return Array.Empty<DamageResult>();
            var spec = Copy(_baseProfile);
            _passives.ModifyAutoAttack(spec);
            var buffImpactSourceSkillId = string.Empty;
            AttackImpactProfile buffImpactProfile = null;
            var hasBuffImpact = _buffs != null &&
                _buffs.TryGetAttackImpactOverride(_owner, out buffImpactSourceSkillId, out buffImpactProfile);
            if (hasBuffImpact)
            {
                spec.SourceSkillId = buffImpactSourceSkillId;
                spec.ImpactProfile = buffImpactProfile;
            }

            if (_empower.IsActive)
            {
                _empower.ActiveModifier.Modify(spec);
                spec.SourceSkillId = _empower.SourceSkillId;
                if (_empower.ImpactProfile != null) spec.ImpactProfile = _empower.ImpactProfile;
            }

            var comboStepIndex = ResolveComboStepIndex();
            var comboStep = ApplyComboStep(spec, comboStepIndex);
            if (_empower.IsActive && _empower.ImpactProfile != null)
                spec.ImpactProfile = _empower.ImpactProfile;
            else if (hasBuffImpact)
                spec.ImpactProfile = buffImpactProfile;

            var found = _targets.FindTargets(new TargetingQuery(_owner.Transform.position, direction, spec.Shape, spec.MaxTargets, _owner));
            if (found.Count == 0)
            {
                // An empower belongs to one attempted auto attack. Do not leave
                // its icon and damage modifier armed forever when the attack misses.
                if (_empower.IsActive) _empower.Clear();
                return Array.Empty<DamageResult>();
            }

            CommitComboStep(comboStepIndex, comboStep);

            var results = new List<DamageResult>(found.Count);
            foreach (var target in found)
            {
                var lines = new DamageLine[Math.Max(1, spec.DamageLines)];
                for (var i = 0; i < lines.Length; i++) lines[i] = new DamageLine(spec.DamageMultiplier, spec.Tags);
                results.Add(_combat.ApplyDamage(new DamageRequest(_owner, target, _owner.Stats.AttackPower,
                    lines, spec.SourceSkillId, spec.Tags, spec.ImpactProfile, direction,
                    spec.CriticalChance, spec.CriticalDamageMultiplier)));
            }

            if (_empower.IsActive) _empower.Clear();
            return results;
        }

        public DamageResult ExecuteOnTarget(UnityEngine.Vector2 direction, IDamageable target,
            UnityEngine.Vector2? impactPosition = null)
        {
            if (!_owner.IsAlive || target == null || !target.IsAlive)
                return DamageResult.Failed(DamageFailureReason.InvalidTarget);

            var spec = Copy(_baseProfile);
            _passives.ModifyAutoAttack(spec);
            var buffImpactSourceSkillId = string.Empty;
            AttackImpactProfile buffImpactProfile = null;
            var hasBuffImpact = _buffs != null &&
                _buffs.TryGetAttackImpactOverride(_owner, out buffImpactSourceSkillId, out buffImpactProfile);
            if (hasBuffImpact)
            {
                spec.SourceSkillId = buffImpactSourceSkillId;
                spec.ImpactProfile = buffImpactProfile;
            }

            if (_empower.IsActive)
            {
                _empower.ActiveModifier.Modify(spec);
                spec.SourceSkillId = _empower.SourceSkillId;
                if (_empower.ImpactProfile != null) spec.ImpactProfile = _empower.ImpactProfile;
            }

            var comboStepIndex = ResolveComboStepIndex();
            var comboStep = ApplyComboStep(spec, comboStepIndex);
            CommitComboStep(comboStepIndex, comboStep);
            var lines = new DamageLine[Math.Max(1, spec.DamageLines)];
            for (var i = 0; i < lines.Length; i++) lines[i] = new DamageLine(spec.DamageMultiplier, spec.Tags);
            var result = _combat.ApplyDamage(new DamageRequest(_owner, target, _owner.Stats.AttackPower,
                lines, spec.SourceSkillId, spec.Tags, spec.ImpactProfile, direction,
                spec.CriticalChance, spec.CriticalDamageMultiplier, impactPosition));
            if (_empower.IsActive) _empower.Clear();
            return result;
        }

        private int ResolveComboStepIndex()
        {
            if (_comboDefinition == null || _comboDefinition.StepCount == 0)
                return -1;

            var timedOut = UnityEngine.Time.time - _lastSuccessfulAttackTime > _comboDefinition.ResetTime;
            return timedOut ? 0 : _nextComboStepIndex;
        }

        private AutoAttackComboStep ApplyComboStep(AutoAttackSpec spec, int comboStepIndex)
        {
            if (_comboDefinition == null || comboStepIndex < 0)
                return AutoAttackComboStep.Default;

            var step = _comboDefinition.GetStep(comboStepIndex);
            spec.DamageMultiplier *= step.DamageMultiplier;
            spec.CriticalChance = step.CriticalChance;
            spec.CriticalDamageMultiplier = step.CriticalDamageMultiplier;

            if (step.ImpactProfile != null)
                spec.ImpactProfile = step.ImpactProfile;

            return step;
        }

        private void CommitComboStep(int comboStepIndex, AutoAttackComboStep step)
        {
            if (_comboDefinition == null || comboStepIndex < 0) return;

            _lastSuccessfulAttackTime = UnityEngine.Time.time;
            _lastComboStepIndex = comboStepIndex;
            _lastComboStep = step;
            _nextComboStepIndex = (comboStepIndex + 1) % Math.Max(1, _comboDefinition.StepCount);
        }

        private static AutoAttackSpec Copy(AutoAttackSpec source) => new()
        {
            DamageMultiplier = source.DamageMultiplier, DamageLines = source.DamageLines,
            MaxTargets = source.MaxTargets, Shape = source.Shape, Tags = source.Tags,
            SourceSkillId = source.SourceSkillId, ImpactProfile = source.ImpactProfile,
            CriticalChance = source.CriticalChance, CriticalDamageMultiplier = source.CriticalDamageMultiplier
        };
    }
}
