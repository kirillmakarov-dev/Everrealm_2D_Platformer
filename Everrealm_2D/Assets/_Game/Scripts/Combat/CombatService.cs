using System;

namespace LetterHunter.Combat
{
    public interface ICombatService
    {
        DamageResult ApplyDamage(DamageRequest request);
    }

    public sealed class CombatService : ICombatService
    {
        public DamageResult ApplyDamage(DamageRequest request)
        {
            if (request?.Attacker == null) return DamageResult.Failed(DamageFailureReason.InvalidAttacker);
            if (request.Target == null) return DamageResult.Failed(DamageFailureReason.InvalidTarget);
            if (!request.Target.IsAlive) return DamageResult.Failed(DamageFailureReason.TargetDead);
            if (request.Lines.Count == 0) return DamageResult.Failed(DamageFailureReason.NoDamageLines);

            var total = 0f;
            foreach (var line in request.Lines)
                total += Math.Max(0f, request.BaseDamage * line.Multiplier - request.Target.Stats.Defense);

            var critical = request.CriticalChance > 0f && UnityEngine.Random.value <= request.CriticalChance;
            if (critical)
                total *= request.CriticalDamageMultiplier;

            var result = DamageResult.Success(total, request.Lines.Count, critical, request.SourceSkillId,
                request.Tags, request.ImpactProfile, request.AttackDirection, request.ImpactPosition, request.Attacker);
            request.Target.ReceiveDamage(result);
            if (!request.Target.IsAlive && request.Target is IExperienceReward reward &&
                request.Attacker is IExperienceRecipient recipient)
                recipient.AddExperience(reward.ExperienceReward);
            return result;
        }
    }
}
