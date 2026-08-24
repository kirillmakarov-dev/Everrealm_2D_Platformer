using System;
using System.Collections.Generic;

namespace LetterHunter.Stats
{
    [Serializable]
    public struct CombatStatsData
    {
        public float maxHealth;
        public float maxMana;
        public float attackPower;
        public float defense;
        public float attackSpeed;
        public float moveSpeed;

        public static CombatStatsData Default => new()
        {
            maxHealth = 100f, maxMana = 50f, attackPower = 10f,
            defense = 2f, attackSpeed = 1f, moveSpeed = 5f
        };
    }

    public sealed class CombatStats
    {
        private readonly Dictionary<object, float> _attackPowerModifiers = new();
        private readonly Dictionary<object, float> _defenseModifiers = new();
        private readonly Dictionary<object, float> _attackSpeedModifiers = new();
        private readonly Dictionary<object, float> _moveSpeedModifiers = new();
        private readonly Dictionary<object, float> _jumpHeightModifiers = new();
        private float _baseJumpHeight;
        private float _gravityMagnitude = 1f;

        public CombatStats(CombatStatsData data)
        {
            MaxHealth = Math.Max(1f, data.maxHealth);
            MaxMana = Math.Max(0f, data.maxMana);
            BaseAttackPower = Math.Max(0f, data.attackPower);
            BaseDefense = Math.Max(0f, data.defense);
            BaseAttackSpeed = Math.Max(0.01f, data.attackSpeed);
            BaseMoveSpeed = Math.Max(0f, data.moveSpeed);
            Level = 1;
            CurrentHealth = MaxHealth;
            CurrentMana = MaxMana;
        }

        public float MaxHealth { get; }
        public float CurrentHealth { get; private set; }
        public float MaxMana { get; }
        public float CurrentMana { get; private set; }
        public float BaseAttackPower { get; }
        public float BaseDefense { get; }
        public float BaseAttackSpeed { get; }
        public float BaseMoveSpeed { get; }
        public float AttackSpeed => Math.Max(0.01f, BaseAttackSpeed + Sum(_attackSpeedModifiers));
        public float MoveSpeed => Math.Max(0f, BaseMoveSpeed + Sum(_moveSpeedModifiers));
        public float JumpHeight => Math.Max(0f, _baseJumpHeight + Sum(_jumpHeightModifiers));
        public float JumpVelocity => (float)Math.Sqrt(2f * _gravityMagnitude * JumpHeight);
        public int Level { get; private set; }
        public float AttackPower => BaseAttackPower + Sum(_attackPowerModifiers);
        public float Defense => BaseDefense + Sum(_defenseModifiers);

        public bool TrySpendMana(float amount)
        {
            amount = Math.Max(0f, amount);
            if (CurrentMana < amount) return false;
            CurrentMana -= amount;
            return true;
        }

        public void RestoreMana(float amount) => CurrentMana = Math.Min(MaxMana, CurrentMana + Math.Max(0f, amount));
        public void Heal(float amount) => CurrentHealth = Math.Min(MaxHealth, CurrentHealth + Math.Max(0f, amount));
        public void TakeDamage(float amount) => CurrentHealth = Math.Max(0f, CurrentHealth - Math.Max(0f, amount));
        public void SetCurrentHealth(float value) => CurrentHealth = Math.Clamp(value, 0f, MaxHealth);
        public void SetCurrentMana(float value) => CurrentMana = Math.Clamp(value, 0f, MaxMana);
        public void SetAttackPowerModifier(object source, float amount) => _attackPowerModifiers[source] = amount;
        public void SetDefenseModifier(object source, float amount) => _defenseModifiers[source] = amount;
        public void SetAttackSpeedModifier(object source, float amount) => _attackSpeedModifiers[source] = amount;
        public void SetMoveSpeedModifier(object source, float amount) => _moveSpeedModifiers[source] = amount;
        public void SetJumpHeightModifier(object source, float amount) => _jumpHeightModifiers[source] = amount;
        public void SetLevel(int level) => Level = Math.Max(1, level);

        // Converts the authored launch velocity into a readable world-space jump height.
        // The runtime jump controller later converts the effective height back to velocity,
        // allowing Skill Tree height bonuses without mutating the movement config asset.
        public void ConfigureJump(float jumpVelocity, float gravityMagnitude)
        {
            _gravityMagnitude = Math.Max(0.01f, Math.Abs(gravityMagnitude));
            var safeVelocity = Math.Max(0f, jumpVelocity);
            _baseJumpHeight = safeVelocity * safeVelocity / (2f * _gravityMagnitude);
        }

        public void RemoveModifiers(object source)
        {
            _attackPowerModifiers.Remove(source);
            _defenseModifiers.Remove(source);
            _attackSpeedModifiers.Remove(source);
            _moveSpeedModifiers.Remove(source);
            _jumpHeightModifiers.Remove(source);
        }

        private static float Sum(Dictionary<object, float> values)
        {
            var total = 0f;
            foreach (var value in values.Values) total += value;
            return total;
        }
    }
}
