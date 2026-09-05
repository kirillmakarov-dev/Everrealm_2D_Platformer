using System.Collections.Generic;
using LetterHunter.Combat;
using LetterHunter.Core;
using LetterHunter.Effects;
using LetterHunter.Skills;
using LetterHunter.Stats;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Tests.EditMode
{
    public sealed class SkillProjectileDamageTests
    {
        [Test]
        public void EmpowerActivatesWithoutProjectileOrImmediateDamage()
        {
            var ownerObject = new GameObject("Owner");
            var targetObject = new GameObject("Target");
            var effect = ScriptableObject.CreateInstance<EmpowerEffectDefinition>();
            var skill = CreateSkill("empower", SkillType.Empower, effect, 10f, 0f);
            try
            {
                var owner = ownerObject.AddComponent<TestActor>(); owner.Initialize();
                var target = targetObject.AddComponent<TestActor>(); target.Initialize();
                var service = CreateService(owner);
                service.Register(skill);
                var mana = owner.Stats.CurrentMana;
                var health = target.Stats.CurrentHealth;
                Assert.That(service.TryPrepareProjectile(skill.SkillId, Vector2.right, Vector2.zero,
                    out var cast).Failure, Is.EqualTo(SkillUseFailure.NotProjectileSkill));
                Assert.That(cast, Is.Null);
                Assert.That(owner.Stats.CurrentMana, Is.EqualTo(mana));
                Assert.That(service.TryUse(skill.SkillId, target, Vector2.right).Success, Is.True);
                Assert.That(target.Stats.CurrentHealth, Is.EqualTo(health));
            }
            finally
            {
                Object.DestroyImmediate(skill); Object.DestroyImmediate(effect);
                Object.DestroyImmediate(ownerObject); Object.DestroyImmediate(targetObject);
            }
        }

        [Test]
        public void BuffProjectile_DamagesEnemyAndAppliesBuffToCaster()
        {
            var ownerObject = new GameObject("Owner");
            var targetObject = new GameObject("Target");
            var owner = ownerObject.AddComponent<TestActor>();
            var target = targetObject.AddComponent<TestActor>();
            owner.Initialize();
            target.Initialize();

            var effect = ScriptableObject.CreateInstance<BuffEffectDefinition>();
            var effectSo = new SerializedObject(effect);
            effectSo.FindProperty("stat").enumValueIndex = (int)BuffStat.AttackPower;
            effectSo.FindProperty("amount").floatValue = 5f;
            effectSo.ApplyModifiedPropertiesWithoutUndo();
            var skill = CreateSkill("buff_projectile", SkillType.Buff, effect, .5f, 5f);
            var service = CreateService(owner);
            service.Register(skill);

            Assert.That(service.TryPrepareProjectile(skill.SkillId, Vector2.right, Vector2.zero, out var cast).Success,
                Is.True);
            Assert.That(service.ResolveProjectileHit(cast, target).Success, Is.True);

            Assert.That(target.Stats.CurrentHealth, Is.EqualTo(97f).Within(.001f));
            Assert.That(owner.Stats.AttackPower, Is.EqualTo(15f).Within(.001f));

            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(ownerObject);
            Object.DestroyImmediate(targetObject);
        }

        [Test]
        public void DamageProjectile_AppliesItsDamageEffectOnlyOnce()
        {
            var ownerObject = new GameObject("Owner");
            var targetObject = new GameObject("Target");
            var owner = ownerObject.AddComponent<TestActor>();
            var target = targetObject.AddComponent<TestActor>();
            owner.Initialize();
            target.Initialize();

            var effect = ScriptableObject.CreateInstance<DamageSkillEffectDefinition>();
            var skill = CreateSkill("damage_projectile", SkillType.Active, effect, 1f, 0f);
            var service = CreateService(owner);
            service.Register(skill);

            Assert.That(service.TryPrepareProjectile(skill.SkillId, Vector2.right, Vector2.zero, out var cast).Success,
                Is.True);
            Assert.That(service.ResolveProjectileHit(cast, target).Success, Is.True);

            Assert.That(target.Stats.CurrentHealth, Is.EqualTo(92f).Within(.001f));

            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(effect);
            Object.DestroyImmediate(ownerObject);
            Object.DestroyImmediate(targetObject);
        }

        private static SkillService CreateService(TestActor owner)
        {
            var combat = new CombatService();
            var buffs = new BuffService();
            var empower = new EmpowerState();
            var targets = new EmptyTargetProvider();
            var passives = new PassiveService(owner);
            var autoAttack = new AutoAttackService(owner, combat, targets, empower, buffs, passives);
            return new SkillService(owner, combat, buffs, empower, autoAttack, targets, passives);
        }

        private static SkillDefinition CreateSkill(string id, SkillType type, SkillEffectDefinition effect,
            float damageMultiplier, float duration)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var so = new SerializedObject(skill);
            so.FindProperty("skillId").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("skillType").enumValueIndex = (int)type;
            so.FindProperty("effect").objectReferenceValue = effect;
            so.FindProperty("baseDamageMultiplier").floatValue = damageMultiplier;
            so.FindProperty("damageLines").intValue = 1;
            so.FindProperty("duration").floatValue = duration;
            so.FindProperty("projectileDamageTags").intValue = (int)(DamageTag.Skill | DamageTag.Ranged);
            so.ApplyModifiedPropertiesWithoutUndo();
            return skill;
        }

        private sealed class EmptyTargetProvider : ITargetProvider
        {
            public IReadOnlyList<IDamageable> FindTargets(TargetingQuery query) => new List<IDamageable>();
        }

        private sealed class TestActor : MonoBehaviour, ICombatActor
        {
            public Transform Transform => transform;
            public CombatStats Stats { get; private set; }
            public CharacterClassType ClassType => CharacterClassType.Warrior;
            public bool IsAlive => Stats != null && Stats.CurrentHealth > 0f;
            public void Initialize() => Stats = new CombatStats(CombatStatsData.Default);
            public void ReceiveDamage(DamageResult result) => Stats.TakeDamage(result.FinalDamage);
        }
    }
}
