using LetterHunter.Characters;
using LetterHunter.Stats;
using UnityEngine;

namespace LetterHunter.Debugging
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerClassController))]
    public sealed class CharacterStatsDebug : MonoBehaviour
    {
        [SerializeField] private PlayerClassController combatModule;
        private readonly object _debugModifierSource = new();

        public CombatStats Stats => combatModule != null ? combatModule.Stats : null;
        public bool IsInitialized => Stats != null;

        private void Reset() => combatModule = GetComponent<PlayerClassController>();
        private void Awake()
        {
            if (combatModule == null) combatModule = GetComponent<PlayerClassController>();
        }

        private void Start()
        {
            if (Stats == null)
            {
                Debug.LogError("[Stats Debug] Player stats were not initialized.", this);
                return;
            }

            Debug.Log($"[Stats Debug] Ready. HP={Stats.CurrentHealth:0.##}/{Stats.MaxHealth:0.##}, " +
                      $"Mana={Stats.CurrentMana:0.##}/{Stats.MaxMana:0.##}, ATK={Stats.AttackPower:0.##}, " +
                      $"DEF={Stats.Defense:0.##}, ASPD={Stats.AttackSpeed:0.##}.", this);
        }

        public void Apply(float health, float mana, float attackPower, float defense, float attackSpeed)
        {
            if (Stats == null) return;
            Stats.SetCurrentHealth(health);
            Stats.SetCurrentMana(mana);
            Stats.SetAttackPowerModifier(_debugModifierSource, Mathf.Max(0f, attackPower) - Stats.BaseAttackPower);
            Stats.SetDefenseModifier(_debugModifierSource, Mathf.Max(0f, defense) - Stats.BaseDefense);
            Stats.SetAttackSpeedModifier(_debugModifierSource, Mathf.Max(.01f, attackSpeed) - Stats.BaseAttackSpeed);
            Debug.Log($"[Stats Debug] Applied HP={Stats.CurrentHealth:0.##}, Mana={Stats.CurrentMana:0.##}, " +
                      $"ATK={Stats.AttackPower:0.##}, DEF={Stats.Defense:0.##}, ASPD={Stats.AttackSpeed:0.##}.", this);
        }

        public void RestoreResources()
        {
            if (Stats == null) return;
            Stats.SetCurrentHealth(Stats.MaxHealth);
            Stats.SetCurrentMana(Stats.MaxMana);
        }

        public void ClearDebugModifiers() => Stats?.RemoveModifiers(_debugModifierSource);
    }
}
