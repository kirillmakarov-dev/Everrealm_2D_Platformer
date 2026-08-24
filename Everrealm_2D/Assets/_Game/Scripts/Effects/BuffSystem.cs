using System;
using System.Collections.Generic;
using LetterHunter.Combat;
using LetterHunter.Feedback;

namespace LetterHunter.Effects
{
    public enum BuffStat { AttackPower, Defense, AttackSpeed }

    public sealed class BuffInstance
    {
        public BuffInstance(string id, ICombatActor target, BuffStat stat, float amount, float duration,
            AttackImpactProfile attackImpactProfile)
        {
            Id = id;
            Target = target;
            Stat = stat;
            Amount = amount;
            Remaining = duration;
            AttackImpactProfile = attackImpactProfile;
        }

        public string Id { get; }
        public ICombatActor Target { get; }
        public BuffStat Stat { get; }
        public float Amount { get; }
        public float Remaining { get; set; }
        public AttackImpactProfile AttackImpactProfile { get; }
    }

    public sealed class BuffService
    {
        private readonly List<BuffInstance> _active = new();

        public void Apply(string id, ICombatActor target, BuffStat stat, float amount, float duration,
            AttackImpactProfile attackImpactProfile = null)
        {
            Remove(id, target);
            var buff = new BuffInstance(id, target, stat, amount, Math.Max(0f, duration), attackImpactProfile);
            _active.Add(buff);
            ApplyModifier(buff);
        }

        public bool TryGetAttackImpactOverride(ICombatActor target, out string sourceSkillId, out AttackImpactProfile profile)
        {
            sourceSkillId = string.Empty;
            profile = null;

            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var buff = _active[i];
                if (!ReferenceEquals(buff.Target, target) || buff.AttackImpactProfile == null)
                    continue;

                sourceSkillId = buff.Id;
                profile = buff.AttackImpactProfile;
                return true;
            }

            return false;
        }

        public void Tick(float deltaTime)
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                _active[i].Remaining -= deltaTime;
                if (_active[i].Remaining <= 0f) RemoveAt(i);
            }
        }

        private void Remove(string id, ICombatActor target)
        {
            for (var i = _active.Count - 1; i >= 0; i--)
                if (_active[i].Id == id && ReferenceEquals(_active[i].Target, target)) RemoveAt(i);
        }

        private static void ApplyModifier(BuffInstance buff)
        {
            if (buff.Stat == BuffStat.AttackPower) buff.Target.Stats.SetAttackPowerModifier(buff, buff.Amount);
            else if (buff.Stat == BuffStat.Defense) buff.Target.Stats.SetDefenseModifier(buff, buff.Amount);
            else buff.Target.Stats.SetAttackSpeedModifier(buff, buff.Amount);
        }

        private void RemoveAt(int index)
        {
            _active[index].Target.Stats.RemoveModifiers(_active[index]);
            _active.RemoveAt(index);
        }
    }
}
