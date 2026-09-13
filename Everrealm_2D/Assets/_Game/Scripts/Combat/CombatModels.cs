using System;
using System.Collections.Generic;
using LetterHunter.Core;
using LetterHunter.Feedback;
using LetterHunter.Stats;
using UnityEngine;

namespace LetterHunter.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        CombatStats Stats { get; }
        void ReceiveDamage(DamageResult result);
    }

    public interface ICombatActor : IDamageable
    {
        Transform Transform { get; }
        CharacterClassType ClassType { get; }
    }

    [Serializable]
    public readonly struct DamageLine
    {
        public DamageLine(float multiplier, DamageTag tags = DamageTag.None)
        {
            Multiplier = Math.Max(0f, multiplier);
            Tags = tags;
        }

        public float Multiplier { get; }
        public DamageTag Tags { get; }
    }

    public sealed class DamageRequest
    {
        public DamageRequest(ICombatActor attacker, IDamageable target, float baseDamage,
            IReadOnlyList<DamageLine> lines, string sourceSkillId, DamageTag tags,
            AttackImpactProfile impactProfile = null, Vector2 attackDirection = default,
            float criticalChance = 0f, float criticalDamageMultiplier = 1.5f,
            Vector2? impactPosition = null)
        {
            Attacker = attacker;
            Target = target;
            BaseDamage = Math.Max(0f, baseDamage);
            Lines = lines ?? Array.Empty<DamageLine>();
            SourceSkillId = sourceSkillId ?? string.Empty;
            Tags = tags;
            ImpactProfile = impactProfile;
            AttackDirection = attackDirection.sqrMagnitude > 0f ? attackDirection.normalized : Vector2.right;
            CriticalChance = Mathf.Clamp01(criticalChance);
            CriticalDamageMultiplier = Math.Max(1f, criticalDamageMultiplier);
            ImpactPosition = impactPosition;
        }

        public ICombatActor Attacker { get; }
        public IDamageable Target { get; }
        public float BaseDamage { get; }
        public IReadOnlyList<DamageLine> Lines { get; }
        public int DamageLinesCount => Lines.Count;
        public string SourceSkillId { get; }
        public DamageTag Tags { get; }
        public AttackImpactProfile ImpactProfile { get; }
        public Vector2 AttackDirection { get; }
        public float CriticalChance { get; }
        public float CriticalDamageMultiplier { get; }
        public Vector2? ImpactPosition { get; }
    }

    public enum DamageFailureReason { None, InvalidAttacker, InvalidTarget, TargetDead, NoDamageLines }

    public readonly struct DamageResult
    {
        private DamageResult(bool applied, float finalDamage, int linesCount, bool wasCritical, DamageFailureReason reason,
            string sourceSkillId, DamageTag tags, AttackImpactProfile impactProfile, Vector2 attackDirection,
            Vector2? impactPosition, ICombatActor attacker)
        {
            AppliedSuccessfully = applied;
            FinalDamage = finalDamage;
            LinesCount = linesCount;
            WasCritical = wasCritical;
            FailureReason = reason;
            SourceSkillId = sourceSkillId ?? string.Empty;
            Tags = tags;
            ImpactProfile = impactProfile;
            AttackDirection = attackDirection.sqrMagnitude > 0f ? attackDirection.normalized : Vector2.right;
            ImpactPosition = impactPosition;
            Attacker = attacker;
        }

        public bool AppliedSuccessfully { get; }
        public float FinalDamage { get; }
        public int LinesCount { get; }
        public bool WasCritical { get; }
        public DamageFailureReason FailureReason { get; }
        public string SourceSkillId { get; }
        public DamageTag Tags { get; }
        public AttackImpactProfile ImpactProfile { get; }
        public Vector2 AttackDirection { get; }
        public Vector2? ImpactPosition { get; }
        public bool HasImpactPosition => ImpactPosition.HasValue;
        public ICombatActor Attacker { get; }

        public static DamageResult Success(float damage, int lines, bool critical = false,
            string sourceSkillId = "", DamageTag tags = DamageTag.None, AttackImpactProfile impactProfile = null,
            Vector2 attackDirection = default, Vector2? impactPosition = null, ICombatActor attacker = null) =>
            new(true, damage, lines, critical, DamageFailureReason.None, sourceSkillId, tags, impactProfile,
                attackDirection, impactPosition, attacker);

        public static DamageResult Failed(DamageFailureReason reason) =>
            new(false, 0f, 0, false, reason, string.Empty, DamageTag.None, null, Vector2.right, null, null);
    }
}
