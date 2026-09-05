using LetterHunter.Combat;
using LetterHunter.Core;
using LetterHunter.Stats;
using NUnit.Framework;
using UnityEngine;

namespace LetterHunter.Tests.EditMode
{
    public sealed class LevelProgressionTests
    {
        [Test]
        public void ThresholdsCarryOverflowAndRestoreTotalExperience()
        {
            var definition = ScriptableObject.CreateInstance<LevelProgressionDefinition>();
            try
            {
                var state = new LevelProgressionState(definition);
                state.Add(149);
                Assert.That(state.Level, Is.EqualTo(1));
                state.Add(1);
                Assert.That(state.Level, Is.EqualTo(2));
                Assert.That(state.CurrentExperience, Is.Zero);
                state.Add(175);
                Assert.That(state.Level, Is.EqualTo(3));
                Assert.That(state.CurrentExperience, Is.EqualTo(25));
                var restored = new LevelProgressionState(definition);
                restored.Restore(state.TotalExperience);
                Assert.That(restored.Level, Is.EqualTo(3));
                Assert.That(restored.Fill, Is.EqualTo(.125f));
                restored.SetLevel(2);
                Assert.That(restored.TotalExperience, Is.EqualTo(150));
                restored.Add(-50);
                Assert.That(restored.TotalExperience, Is.EqualTo(150));
                restored.Add(int.MaxValue);
                Assert.That(restored.IsMaxLevel, Is.True);
                Assert.That(restored.Fill, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(definition); }
        }

        [Test]
        public void OnlyKillingHitAwardsExperienceAndDeadTargetsCannotAwardTwice()
        {
            var attacker = new Actor();
            var target = new Actor();
            var combat = new CombatService();
            DamageRequest Hit(float damage) => new(attacker, target, damage,
                new[] { new DamageLine(1) }, "", DamageTag.None);
            combat.ApplyDamage(Hit(1));
            Assert.That(attacker.Experience, Is.Zero);
            combat.ApplyDamage(Hit(10000));
            Assert.That(attacker.Experience, Is.EqualTo(25));
            combat.ApplyDamage(Hit(10000));
            Assert.That(attacker.Experience, Is.EqualTo(25));
        }

        private sealed class Actor : ICombatActor, IExperienceRecipient, IExperienceReward
        {
            public Transform Transform => null;
            public CharacterClassType ClassType => CharacterClassType.Warrior;
            public CombatStats Stats { get; } = new(CombatStatsData.Default);
            public bool IsAlive => Stats.CurrentHealth > 0;
            public int ExperienceReward => 25;
            public int Experience { get; private set; }
            public void ReceiveDamage(DamageResult result) => Stats.TakeDamage(result.FinalDamage);
            public void AddExperience(int amount) => Experience += amount;
        }
    }
}
