using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using LetterHunter.Characters;
using LetterHunter.Classes;
using LetterHunter.Combat;
using LetterHunter.Core;
using LetterHunter.Core.StateMachine;
using LetterHunter.Effects;
using LetterHunter.Feedback;
using LetterHunter.Skills;
using LetterHunter.Stats;
using LetterHunter.UI.Skills;
using NUnit.Framework;
using UnityEngine;

namespace LetterHunter.Tests
{
    public sealed class GameplayRegressionTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<UnityEngine.Object> _assets = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _assets)
                if (asset != null) UnityEngine.Object.DestroyImmediate(asset);

            foreach (var obj in _objects)
                if (obj != null) UnityEngine.Object.DestroyImmediate(obj);

            _assets.Clear();
            _objects.Clear();
        }

        [Test]
        public void CombatStats_ClampResourcesAndApplyIndependentModifiers()
        {
            var stats = new CombatStats(new CombatStatsData
            {
                maxHealth = -10f,
                maxMana = 12f,
                attackPower = -5f,
                defense = 2f,
                attackSpeed = -1f,
                moveSpeed = -3f
            });

            Assert.That(stats.MaxHealth, Is.EqualTo(1f));
            Assert.That(stats.BaseAttackPower, Is.EqualTo(0f));
            Assert.That(stats.AttackSpeed, Is.EqualTo(0.01f));
            Assert.That(stats.MoveSpeed, Is.EqualTo(0f));

            Assert.That(stats.TrySpendMana(4f), Is.True);
            Assert.That(stats.CurrentMana, Is.EqualTo(8f));
            Assert.That(stats.TrySpendMana(99f), Is.False);
            stats.RestoreMana(99f);
            Assert.That(stats.CurrentMana, Is.EqualTo(12f));

            var attackSource = new object();
            var defenseSource = new object();
            stats.SetAttackPowerModifier(attackSource, 7f);
            stats.SetDefenseModifier(defenseSource, 3f);

            Assert.That(stats.AttackPower, Is.EqualTo(7f));
            Assert.That(stats.Defense, Is.EqualTo(5f));

            stats.RemoveModifiers(attackSource);

            Assert.That(stats.AttackPower, Is.EqualTo(0f));
            Assert.That(stats.Defense, Is.EqualTo(5f));

            stats.ConfigureJump(10f, 20f);
            var movementSource = new object();
            stats.SetMoveSpeedModifier(movementSource, 1.5f);
            stats.SetJumpHeightModifier(movementSource, 0.5f);
            stats.SetLevel(4);

            Assert.That(stats.MoveSpeed, Is.EqualTo(1.5f));
            Assert.That(stats.JumpHeight, Is.EqualTo(3f).Within(0.001f));
            Assert.That(stats.JumpVelocity, Is.EqualTo(Mathf.Sqrt(120f)).Within(0.001f));
            Assert.That(stats.Level, Is.EqualTo(4));

            stats.RemoveModifiers(movementSource);
            Assert.That(stats.MoveSpeed, Is.EqualTo(0f));
            Assert.That(stats.JumpHeight, Is.EqualTo(2.5f).Within(0.001f));
        }

        [Test]
        public void CombatService_AppliesDefensePerLineAndReportsCriticalMetadata()
        {
            var attacker = CreateActor("Attacker", attackPower: 20f, defense: 0f);
            var target = CreateActor("Target", attackPower: 1f, defense: 3f);
            var request = new DamageRequest(attacker, target, 10f, new[]
                {
                    new DamageLine(1f, DamageTag.Skill),
                    new DamageLine(0.5f, DamageTag.Skill)
                },
                "fire_spin", DamageTag.Skill | DamageTag.Fire, null, new Vector2(5f, 0f),
                criticalChance: 1f, criticalDamageMultiplier: 2f);

            var result = new CombatService().ApplyDamage(request);

            Assert.That(result.AppliedSuccessfully, Is.True);
            Assert.That(result.WasCritical, Is.True);
            Assert.That(result.FinalDamage, Is.EqualTo(18f).Within(0.001f));
            Assert.That(result.LinesCount, Is.EqualTo(2));
            Assert.That(result.SourceSkillId, Is.EqualTo("fire_spin"));
            Assert.That(result.Tags.HasFlag(DamageTag.Fire), Is.True);
            Assert.That(target.Stats.CurrentHealth, Is.EqualTo(82f).Within(0.001f));
            Assert.That(target.LastDamage.FinalDamage, Is.EqualTo(18f).Within(0.001f));
        }

        [Test]
        public void CombatService_ReturnsFailureWithoutMutatingInvalidTargets()
        {
            var attacker = CreateActor("Attacker", attackPower: 10f, defense: 0f);
            var target = CreateActor("Target", attackPower: 1f, defense: 0f);
            target.Stats.TakeDamage(999f);

            var deadResult = new CombatService().ApplyDamage(new DamageRequest(attacker, target, 10f,
                new[] { new DamageLine(1f) }, "hit", DamageTag.Melee));
            var noLinesResult = new CombatService().ApplyDamage(new DamageRequest(attacker, attacker, 10f,
                Array.Empty<DamageLine>(), "hit", DamageTag.Melee));

            Assert.That(deadResult.FailureReason, Is.EqualTo(DamageFailureReason.TargetDead));
            Assert.That(noLinesResult.FailureReason, Is.EqualTo(DamageFailureReason.NoDamageLines));
            Assert.That(target.ReceivedDamageCount, Is.EqualTo(0));
        }

        [Test]
        public void AutoAttackService_UsesComboStepsPassivesAndConsumesEmpowerAfterHit()
        {
            var attacker = CreateActor("Attacker", attackPower: 10f, defense: 0f);
            var firstTarget = CreateActor("Target A", attackPower: 1f, defense: 0f);
            var secondTarget = CreateActor("Target B", attackPower: 1f, defense: 0f);
            var targets = new StubTargetProvider(firstTarget, secondTarget);
            var empower = new EmpowerState();
            var buffs = new BuffService();
            var passives = new PassiveService(attacker, new FixedRandomSource(0f));
            var service = new AutoAttackService(attacker, new CombatService(), targets, empower, buffs, passives);
            service.SetBaseProfile(new AutoAttackSpec { DamageMultiplier = 1f, DamageLines = 1, MaxTargets = 2 });
            service.SetComboDefinition(CreateCombo(new[]
            {
                new AutoAttackComboStep("Opening", 1f, 0f, 1.5f, null),
                new AutoAttackComboStep("Middle", 2f, 0f, 1.5f, null),
                new AutoAttackComboStep("Finisher", 3f, 1f, 2f, null)
            }));

            var passive = CreateAsset<ExtraDamageLinePassiveDefinition>();
            SetField(passive, "chance", 1f);
            SetField(passive, "extraLines", 1);
            passives.Register(passive);
            empower.Set("charged_hit", new TestEmpowerModifier(), null);

            var firstResults = service.Execute(Vector2.right);
            var secondResults = service.Execute(Vector2.right);

            Assert.That(firstResults, Has.Count.EqualTo(2));
            Assert.That(firstResults[0].FinalDamage, Is.EqualTo(40f).Within(0.001f));
            Assert.That(firstResults[0].LinesCount, Is.EqualTo(2));
            Assert.That(firstResults[0].SourceSkillId, Is.EqualTo("charged_hit"));
            Assert.That(firstResults[0].Tags.HasFlag(DamageTag.Fire), Is.True);
            Assert.That(empower.IsActive, Is.False);

            Assert.That(secondResults[0].FinalDamage, Is.EqualTo(40f).Within(0.001f));
            Assert.That(service.LastComboStepNumber, Is.EqualTo(2));

            var thirdResults = service.Execute(Vector2.right);

            Assert.That(thirdResults[0].WasCritical, Is.True);
            Assert.That(thirdResults[0].FinalDamage, Is.EqualTo(120f).Within(0.001f));
            Assert.That(service.LastComboStep.DisplayName, Is.EqualTo("Finisher"));
        }

        [Test]
        public void BuffService_ReplacesExistingBuffExpiresAndProvidesLatestImpactOverride()
        {
            var actor = CreateActor("Buffed", attackPower: 10f, defense: 1f);
            var weakImpact = CreateAsset<AttackImpactProfile>();
            var strongImpact = CreateAsset<AttackImpactProfile>();
            var buffs = new BuffService();

            buffs.Apply("focus", actor, BuffStat.AttackPower, 5f, 3f, weakImpact);
            buffs.Apply("focus", actor, BuffStat.AttackPower, 8f, 3f, strongImpact);

            Assert.That(actor.Stats.AttackPower, Is.EqualTo(18f));
            Assert.That(buffs.TryGetAttackImpactOverride(actor, out var sourceSkillId, out var profile), Is.True);
            Assert.That(sourceSkillId, Is.EqualTo("focus"));
            Assert.That(profile, Is.SameAs(strongImpact));

            buffs.Tick(3.1f);

            Assert.That(actor.Stats.AttackPower, Is.EqualTo(10f));
            Assert.That(buffs.TryGetAttackImpactOverride(actor, out _, out _), Is.False);
        }

        [Test]
        public void PassiveService_RegistersDefenseAndPeriodicHealEffects()
        {
            var actor = CreateActor("Passive Owner", attackPower: 10f, defense: 2f);
            actor.Stats.TakeDamage(20f);
            var defense = CreateAsset<DefensePassiveDefinition>();
            var heal = CreateAsset<PeriodicHealPassiveDefinition>();
            SetField(defense, "defenseBonus", 4f);
            SetField(heal, "interval", 2f);
            SetField(heal, "healAmount", 7f);
            var passives = new PassiveService(actor);

            passives.Register(defense);
            passives.Register(heal);
            passives.Tick(1.9f);

            Assert.That(actor.Stats.Defense, Is.EqualTo(6f));
            Assert.That(actor.Stats.CurrentHealth, Is.EqualTo(80f));

            passives.Tick(0.2f);

            Assert.That(actor.Stats.CurrentHealth, Is.EqualTo(87f));
        }

        [Test]
        public void SkillService_ConsumesManaStartsCooldownAndAppliesAreaDamage()
        {
            var caster = CreateActor("Caster", attackPower: 12f, defense: 0f);
            var target = CreateActor("Skill Target", attackPower: 1f, defense: 1f);
            var targetProvider = new StubTargetProvider(target);
            var buffs = new BuffService();
            var empower = new EmpowerState();
            var passives = new PassiveService(caster);
            var autoAttack = new AutoAttackService(caster, new CombatService(), targetProvider, empower, buffs, passives);
            var service = new SkillService(caster, new CombatService(), buffs, empower, autoAttack, targetProvider, passives);
            var effect = CreateAsset<DamageSkillEffectDefinition>();
            SetField(effect, "damageTags", DamageTag.Skill | DamageTag.Fire);
            var skill = CreateSkill("fire_spin", SkillType.Active, effect, manaCost: 10f, cooldown: 2f,
                duration: 0.5f, damageMultiplier: 2f, damageLines: 2, maxTargets: 1);

            service.Register(skill);
            var result = service.TryUse("fire_spin", null, Vector2.right);
            var cooldownResult = service.TryUse("fire_spin", null, Vector2.right);

            Assert.That(result.Success, Is.True);
            Assert.That(cooldownResult.Failure, Is.EqualTo(SkillUseFailure.OnCooldown));
            Assert.That(caster.Stats.CurrentMana, Is.EqualTo(40f));
            Assert.That(target.LastDamage.FinalDamage, Is.EqualTo(46f).Within(0.001f));
            Assert.That(target.LastDamage.Tags.HasFlag(DamageTag.Fire), Is.True);
            Assert.That(service.TryGetRuntimeState("fire_spin", out var state), Is.True);
            Assert.That(state.CooldownRemaining, Is.EqualTo(2f).Within(0.001f));

            service.Tick(2.1f);

            Assert.That(state.IsReady, Is.True);
        }

        [Test]
        public void SkillService_UsesProgressionDamageManaAndCooldownModifiersWithoutMutatingAsset()
        {
            var caster = CreateActor("Progression Caster", attackPower: 12f, defense: 0f);
            var target = CreateActor("Progression Target", attackPower: 1f, defense: 1f);
            var targetProvider = new StubTargetProvider(target);
            var buffs = new BuffService();
            var empower = new EmpowerState();
            var passives = new PassiveService(caster);
            var autoAttack = new AutoAttackService(caster, new CombatService(), targetProvider, empower, buffs, passives);
            var service = new SkillService(caster, new CombatService(), buffs, empower, autoAttack, targetProvider, passives);
            var skill = CreateSkill("ranked_slash", SkillType.Active, CreateAsset<DamageSkillEffectDefinition>(),
                manaCost: 10f, cooldown: 2f, duration: 0f, damageMultiplier: 2f, damageLines: 2, maxTargets: 1);

            service.Register(skill);
            service.SetProgressionModifiers(skill.SkillId, new SkillRuntimeModifiers(0.25f, 0.25f, 0.2f));
            var result = service.TryUse(skill.SkillId, null, Vector2.right);

            Assert.That(result.Success, Is.True);
            Assert.That(caster.Stats.CurrentMana, Is.EqualTo(42f).Within(0.001f));
            Assert.That(target.LastDamage.FinalDamage, Is.EqualTo(58f).Within(0.001f));
            Assert.That(service.TryGetRuntimeState(skill.SkillId, out var state), Is.True);
            Assert.That(state.CooldownRemaining, Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(skill.ManaCost, Is.EqualTo(10f));
            Assert.That(skill.Cooldown, Is.EqualTo(2f));
            Assert.That(skill.BaseDamageMultiplier, Is.EqualTo(2f));
        }

        [Test]
        public void SkillService_RejectsUnregisteredDeadAndUnaffordableSkills()
        {
            var caster = CreateActor("Caster", attackPower: 8f, defense: 0f);
            var targetProvider = new StubTargetProvider();
            var buffs = new BuffService();
            var empower = new EmpowerState();
            var passives = new PassiveService(caster);
            var autoAttack = new AutoAttackService(caster, new CombatService(), targetProvider, empower, buffs, passives);
            var service = new SkillService(caster, new CombatService(), buffs, empower, autoAttack, targetProvider, passives);

            Assert.That(service.TryUse("missing", null, Vector2.right).Failure, Is.EqualTo(SkillUseFailure.NotRegistered));

            var expensive = CreateSkill("expensive", SkillType.Active, CreateAsset<DamageSkillEffectDefinition>(),
                manaCost: 999f, cooldown: 0f, duration: 0f, damageMultiplier: 1f, damageLines: 1, maxTargets: 1);
            service.Register(expensive);

            Assert.That(service.TryUse("expensive", null, Vector2.right).Failure, Is.EqualTo(SkillUseFailure.NotEnoughMana));

            caster.Stats.TakeDamage(999f);

            Assert.That(service.TryUse("expensive", null, Vector2.right).Failure, Is.EqualTo(SkillUseFailure.CasterDead));
        }

        [Test]
        public void ScriptableDefinitions_ExposeSafeFallbackValuesForInspectorData()
        {
            var classDefinition = CreateAsset<ClassDefinition>();
            var combo = CreateCombo(Array.Empty<AutoAttackComboStep>());
            SetField(combo, "comboId", " ");
            SetField(combo, "resetTime", -1f);
            var namelessStep = new AutoAttackComboStep(" ", -2f, 3f, 0.2f, null);

            Assert.That(classDefinition.BaseStats.maxHealth, Is.EqualTo(CombatStatsData.Default.maxHealth));
            Assert.That(combo.ComboId, Is.EqualTo(combo.name));
            Assert.That(combo.ResetTime, Is.EqualTo(0.05f));
            Assert.That(combo.GetStep(5).DisplayName, Is.EqualTo("Hit"));
            Assert.That(namelessStep.DamageMultiplier, Is.EqualTo(1f));
            Assert.That(namelessStep.CriticalChance, Is.EqualTo(1f));
            Assert.That(namelessStep.CriticalDamageMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void CharacterStateMachine_FollowsMovementGroundingAndAttackPriority()
        {
            var runtime = new CharacterRuntime();
            SetProperty(runtime, "Grounded", true);
            SetProperty(runtime, "CurrentVelocity", new Vector2(3f, 0f));
            var machine = new CharacterStateMachine(runtime);

            machine.Tick(0.016f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Run));

            SetProperty(runtime, "Grounded", false);
            SetProperty(runtime, "CurrentVelocity", new Vector2(0f, 5f));
            machine.NotifyJump();
            machine.Tick(0.016f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Jump));

            machine.BeginAttack(0.05f);
            SetProperty(runtime, "Grounded", true);
            SetProperty(runtime, "CurrentVelocity", Vector2.zero);
            machine.Tick(0.016f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Attack));

            machine.Tick(0.05f);
            machine.Tick(0.016f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Idle));
        }

        [Test]
        public void CharacterStateMachine_SuppressesShortFallUntilDistanceThreshold()
        {
            var runtime = new CharacterRuntime();
            SetProperty(runtime, "Grounded", false);
            SetProperty(runtime, "CurrentVelocity", new Vector2(0f, -1f));
            SetProperty(runtime, "AirborneDropDistance", .49f);
            var machine = new CharacterStateMachine(runtime, .5f);

            machine.Tick(.016f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Idle));

            SetProperty(runtime, "AirborneDropDistance", .5f);
            machine.Tick(.016f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Fall));

            machine.SetMinimumFallDistance(2f);
            SetProperty(runtime, "AirborneDropDistance", 1f);
            machine.Tick(.016f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Idle));
        }

        [Test]
        public void Jump_KeepsPoseAtApex_AndNeverCreatesJumpOnLedge()
        {
            var runtime = new CharacterRuntime();
            var machine = new CharacterStateMachine(runtime, .5f);
            SetProperty(runtime, "CurrentVelocity", new Vector2(0f, -.2f));
            machine.Tick(.02f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Idle));
            machine.NotifyJump();
            machine.Tick(.02f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Jump));
            SetProperty(runtime, "AirborneDropDistance", .6f);
            machine.Tick(.02f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Fall));
            SetProperty(runtime, "Grounded", true);
            machine.Tick(.02f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Idle));
            SetProperty(runtime, "Grounded", false);
            SetProperty(runtime, "AirborneDropDistance", .1f);
            machine.Tick(.02f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Idle));
        }

        [Test]
        public void Jump_BufferCoyoteAndSingleImpulse()
        {
            var config = CreateAsset<CharacterMovementConfig>();
            var motor = new JumpTestMotor();
            var ground = new JumpTestGround { IsGrounded = true };
            var jump = new CharacterJumpController(motor, ground, config);
            jump.Tick(.02f);
            ground.IsGrounded = false;
            jump.Tick(.02f);
            jump.RequestJump();
            Assert.That(jump.Tick(.02f), Is.True, "Coyote jump");
            Assert.That(jump.TryJump(), Is.False, "No double jump");
            motor.SetVerticalVelocity(-1f);
            jump.Tick(.2f);
            jump.RequestJump();
            Assert.That(jump.Tick(.02f), Is.False);
            ground.IsGrounded = true;
            Assert.That(jump.Tick(.02f), Is.True, "Buffered landing jump");
        }

        [Test]
        public void Jump_ExpiredBufferDoesNotFireOnLanding()
        {
            var config = CreateAsset<CharacterMovementConfig>();
            var motor = new JumpTestMotor();
            var ground = new JumpTestGround();
            var jump = new CharacterJumpController(motor, ground, config);
            jump.RequestJump();
            Assert.That(jump.Tick(.2f), Is.False);
            ground.IsGrounded = true;
            Assert.That(jump.Tick(.02f), Is.False);
        }

        private sealed class JumpTestMotor : ICharacterMotor2D
        {
            public Vector2 Velocity { get; private set; }
            public void SetHorizontalVelocity(float x) => Velocity = new Vector2(x, Velocity.y);
            public void SetVerticalVelocity(float y) => Velocity = new Vector2(Velocity.x, y);
            public void ConfigureGravity(float value) { }
        }

        [Test]
        public void Shooting_DoesNotHideJumpFallOrLandingFromLocomotion()
        {
            var runtime = new CharacterRuntime();
            var machine = new CharacterStateMachine(runtime);
            machine.BeginAttack(1f);
            machine.NotifyJump();
            SetProperty(runtime, "CurrentVelocity", new Vector2(2f, 5f));
            machine.Tick(.02f);
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Attack));
            Assert.That(machine.LocomotionState, Is.EqualTo(CharacterStateId.Jump));
            SetProperty(runtime, "CurrentVelocity", new Vector2(2f, -5f));
            SetProperty(runtime, "AirborneDropDistance", 1f);
            machine.Tick(.02f);
            Assert.That(machine.LocomotionState, Is.EqualTo(CharacterStateId.Fall));
            SetProperty(runtime, "Grounded", true);
            machine.Tick(.02f);
            Assert.That(machine.LocomotionState, Is.EqualTo(CharacterStateId.Run));
            SetProperty(runtime, "CurrentVelocity", Vector2.zero);
            machine.Tick(.02f);
            Assert.That(machine.LocomotionState, Is.EqualTo(CharacterStateId.Idle));
            Assert.That(runtime.CurrentState, Is.EqualTo(CharacterStateId.Attack));
        }

        [Test]
        public void PlayerAnimator_LandingAndFallUsePhysics_AndShootingCannotRestartItself()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Player model/Walking.controller");
            var states = controller.layers[0].stateMachine.states.Select(s => s.state).ToArray();
            foreach (var name in new[] { "Jump", "Fall" })
            {
                var state = states.Single(s => s.name == name);
                var landing = state.transitions.Single(t => t.destinationState.name == "Blend Tree");
                Assert.That(landing.hasExitTime, Is.False, name + " must land immediately");
                Assert.That(landing.conditions.Any(c => c.parameter == "LocomotionState" && c.threshold == 0), Is.True);
                Assert.That(landing.duration, Is.LessThanOrEqualTo(.08f));
            }
            var falling = states.Single(s => s.name == "Jump").transitions.Single(t => t.destinationState.name == "Fall");
            Assert.That(falling.hasExitTime, Is.False);
            Assert.That(falling.conditions.Any(c => c.parameter == "LocomotionState" && c.threshold == 3), Is.True);
            Assert.That(states.Single(s => s.name == "Fall").transitions.Any(t => t.destinationState.name == "Jump"), Is.True);
            var shot = controller.layers[1].stateMachine.anyStateTransitions.Single();
            Assert.That(shot.canTransitionToSelf, Is.False);
            Assert.That(shot.duration, Is.LessThanOrEqualTo(.05f));
        }

        [Test]
        public void SkeletonDeath_KeepsFloorCollision_AndOutlivesDeathClip()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/SkeletonEnemy.prefab");
            var enemy = new SerializedObject(prefab.GetComponent<LetterHunter.Debugging.DummyEnemy2D>());
            Assert.That(enemy.FindProperty("disableCollidersOnDeath").boolValue, Is.False);
            Assert.That(enemy.FindProperty("deathUpwardVelocity").floatValue, Is.Zero);
            Assert.That(enemy.FindProperty("deathHorizontalVelocity").floatValue, Is.Zero);
            Assert.That(prefab.GetComponentsInChildren<Collider2D>().Any(c => c.enabled && !c.isTrigger), Is.True);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Game/Enemy model/skeleton Death.anim");
            Assert.That(enemy.FindProperty("destroyAfterDeath").floatValue, Is.GreaterThan(clip.length));
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/_Game/Enemy model/SkeletonEnemy.controller");
            var death = controller.layers[0].stateMachine.anyStateTransitions.Single(t =>
                t.conditions.Any(c => c.parameter == "Dead"));
            Assert.That(death.canTransitionToSelf, Is.False, "Dead stays true; self-transition would restart the clip every frame");
            Assert.That(death.hasExitTime, Is.False);
        }

        [Test]
        public void SkeletonDeath_LocksHorizontalPositionAndClearsResidualMotion()
        {
            var root = new GameObject("Death movement test");
            _objects.Add(root);
            var body = root.AddComponent<Rigidbody2D>();
            root.AddComponent<BoxCollider2D>();
            var enemy = root.AddComponent<LetterHunter.Debugging.DummyEnemy2D>();
            SetField(enemy, "_body", body);
            SetField(enemy, "deathUpwardVelocity", 0f);
            SetField(enemy, "deathGravityScale", 3.5f);

            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.linearVelocity = new Vector2(4f, -2f);
            body.angularVelocity = 5f;
            InvokePrivate(enemy, "ConfigureDeathPhysics");

            Assert.That(body.constraints.HasFlag(RigidbodyConstraints2D.FreezePositionX), Is.True);
            Assert.That(body.gravityScale, Is.EqualTo(3.5f));

            body.linearVelocity = new Vector2(3f, -2f);
            body.angularVelocity = 5f;
            InvokePrivate(enemy, "StopDeathHorizontalMovement");

            Assert.That(body.linearVelocity.x, Is.Zero);
            Assert.That(body.linearVelocity.y, Is.EqualTo(-2f));
            Assert.That(body.angularVelocity, Is.Zero);
        }

        [Test]
        public void EnemyPatrol_IgnoresTargetsSeparatedByAnotherPlatform()
        {
            var enemyObject = new GameObject("Patrol vertical range test");
            _objects.Add(enemyObject);
            enemyObject.transform.position = Vector3.zero;
            enemyObject.AddComponent<Rigidbody2D>();
            var enemyCollider = enemyObject.AddComponent<BoxCollider2D>();
            enemyCollider.size = new Vector2(1f, 2f);
            var patrol = enemyObject.AddComponent<EnemyPatrolAI2D>();

            var targetObject = new GameObject("Target");
            _objects.Add(targetObject);
            targetObject.transform.position = new Vector3(0f, 4f, 0f);
            var targetCollider = targetObject.AddComponent<BoxCollider2D>();
            targetCollider.size = new Vector2(1f, 2f);
            Physics2D.SyncTransforms();

            SetField(patrol, "_collider", enemyCollider);
            SetField(patrol, "maxTargetVerticalGap", 1.25f);
            Assert.That((bool)InvokePrivate(patrol, "IsWithinTargetVerticalRange", targetObject.transform, targetCollider), Is.False);

            targetObject.transform.position = new Vector3(2f, 0f, 0f);
            Physics2D.SyncTransforms();
            Assert.That((bool)InvokePrivate(patrol, "IsWithinTargetVerticalRange", targetObject.transform, targetCollider), Is.True);
        }

        [Test]
        public void ProjectileAudio_UsesPrefabOverrides_AndBasicPrefabFallsBackToManager()
        {
            var fire = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/Projectiles/Skills/Projectile_warrior_fire_spin.prefab");
            var basic = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/SkillProjectile.prefab");
            var fireProjectile = fire.GetComponent<SkillProjectile2D>();
            var basicProjectile = basic.GetComponent<SkillProjectile2D>();
            var bouncingStar = AssetDatabase.LoadAssetAtPath<SkillDefinition>(
                "Assets/_Game/Data/Skills/Ninja/BouncingStar.asset");

            Assert.That(fireProjectile.LaunchSound, Is.Not.Null);
            Assert.That(fireProjectile.ImpactSound, Is.Not.Null);
            Assert.That(bouncingStar.ProjectileLaunchSound, Is.Not.Null,
                "A skill definition can override the projectile prefab launch sound.");
            Assert.That(basicProjectile.LaunchSound, Is.Null,
                "The default projectile must use SoundManager's default shot when no override is authored.");
            Assert.That(basicProjectile.ImpactSound, Is.Null,
                "The default projectile must use SoundManager's default impact when no override is authored.");
        }

        private sealed class JumpTestGround : IGroundDetector
        {
            public bool IsGrounded { get; set; }
            public event Action Landed { add { } remove { } }
            public event Action LeftGround { add { } remove { } }
        }

        [Test]
        public void GenericStateMachine_InvokesEnterExitTickAndIgnoresUnknownStates()
        {
            var machine = new StateMachine<TestStateId, object>();
            var first = new CountingState();
            var second = new CountingState();
            var context = new object();
            machine.Register(TestStateId.First, first);
            machine.Register(TestStateId.Second, second);

            Assert.That(machine.ChangeState(TestStateId.First, context), Is.True);
            Assert.That(machine.ChangeState(TestStateId.First, context), Is.False);
            machine.Tick(context, 0.25f);
            Assert.That(machine.ChangeState(TestStateId.Second, context), Is.True);
            Assert.That(machine.ChangeState(TestStateId.Missing, context), Is.False);

            Assert.That(first.EnterCount, Is.EqualTo(1));
            Assert.That(first.TickCount, Is.EqualTo(1));
            Assert.That(first.ExitCount, Is.EqualTo(1));
            Assert.That(second.EnterCount, Is.EqualTo(1));
            Assert.That(machine.CurrentId, Is.EqualTo(TestStateId.Second));
        }

        [Test]
        public void SkillRuntimeStateAndSlotViewModel_ReportCooldownReadinessForHud()
        {
            var state = new SkillRuntimeState();
            var skill = CreateSkill("slash", SkillType.Active, CreateAsset<DamageSkillEffectDefinition>(),
                manaCost: 0f, cooldown: 4f, duration: 1f, damageMultiplier: 1f, damageLines: 1, maxTargets: 1);

            state.StartCooldown(4f);
            state.StartDuration(1f);
            state.Tick(1.5f);
            var viewModel = new SkillSlotViewModel(0, skill, state, canAffordMana: true, inputLabel: "Q");

            Assert.That(state.IsActive, Is.False);
            Assert.That(state.IsReady, Is.False);
            Assert.That(state.CooldownRemaining, Is.EqualTo(2.5f));
            Assert.That(viewModel.HasSkill, Is.True);
            Assert.That(viewModel.IsReady, Is.False);
            Assert.That(viewModel.CooldownFill, Is.EqualTo(0.625f).Within(0.001f));
            Assert.That(viewModel.InputLabel, Is.EqualTo("Q"));
        }

        private TestCombatActor CreateActor(string name, float attackPower, float defense)
        {
            var obj = new GameObject(name);
            _objects.Add(obj);
            return new TestCombatActor(obj.transform, new CombatStats(new CombatStatsData
            {
                maxHealth = 100f,
                maxMana = 50f,
                attackPower = attackPower,
                defense = defense,
                attackSpeed = 1f,
                moveSpeed = 5f
            }));
        }

        private T CreateAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _assets.Add(asset);
            return asset;
        }

        private SkillDefinition CreateSkill(string id, SkillType type, SkillEffectDefinition effect, float manaCost,
            float cooldown, float duration, float damageMultiplier, int damageLines, int maxTargets)
        {
            var skill = CreateAsset<SkillDefinition>();
            SetField(skill, "skillId", id);
            SetField(skill, "displayName", id);
            SetField(skill, "skillType", type);
            SetField(skill, "manaCost", manaCost);
            SetField(skill, "cooldown", cooldown);
            SetField(skill, "duration", duration);
            SetField(skill, "baseDamageMultiplier", damageMultiplier);
            SetField(skill, "damageLines", damageLines);
            SetField(skill, "maxTargets", maxTargets);
            SetField(skill, "effect", effect);
            return skill;
        }

        private AutoAttackComboDefinition CreateCombo(AutoAttackComboStep[] steps)
        {
            var combo = CreateAsset<AutoAttackComboDefinition>();
            combo.name = "Test Combo";
            SetField(combo, "comboId", "test_combo");
            SetField(combo, "resetTime", 0.85f);
            SetField(combo, "steps", steps);
            return combo;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(target.GetType().Name, fieldName);
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            var property = target.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (property == null) throw new MissingMemberException(target.GetType().Name, propertyName);
            property.SetValue(target, value);
        }

        private static object InvokePrivate(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (method == null) throw new MissingMethodException(target.GetType().Name, methodName);
            return method.Invoke(target, arguments);
        }

        private sealed class TestCombatActor : ICombatActor
        {
            public TestCombatActor(Transform transform, CombatStats stats)
            {
                Transform = transform;
                Stats = stats;
            }

            public bool IsAlive => Stats.CurrentHealth > 0f;
            public CombatStats Stats { get; }
            public Transform Transform { get; }
            public CharacterClassType ClassType => CharacterClassType.Warrior;
            public DamageResult LastDamage { get; private set; }
            public int ReceivedDamageCount { get; private set; }

            public void ReceiveDamage(DamageResult result)
            {
                LastDamage = result;
                ReceivedDamageCount++;
                if (result.AppliedSuccessfully)
                    Stats.TakeDamage(result.FinalDamage);
            }
        }

        private sealed class StubTargetProvider : ITargetProvider
        {
            private readonly IReadOnlyList<IDamageable> _targets;

            public StubTargetProvider(params IDamageable[] targets) => _targets = targets;

            public IReadOnlyList<IDamageable> FindTargets(TargetingQuery query)
            {
                var count = Mathf.Min(query.MaxTargets, _targets.Count);
                var found = new List<IDamageable>(count);
                for (var i = 0; i < count; i++)
                    found.Add(_targets[i]);

                return found;
            }
        }

        private sealed class TestEmpowerModifier : IEmpowerModifier
        {
            public void Modify(AutoAttackSpec attack)
            {
                attack.DamageMultiplier *= 2f;
                attack.Tags |= DamageTag.Fire;
            }
        }

        private sealed class FixedRandomSource : IRandomSource
        {
            private readonly float _value;
            public FixedRandomSource(float value) => _value = value;
            public float Next01() => _value;
        }

        private enum TestStateId { Missing, First, Second }

        private sealed class CountingState : IState<object>
        {
            public int EnterCount { get; private set; }
            public int ExitCount { get; private set; }
            public int TickCount { get; private set; }
            public void Enter(object context) => EnterCount++;
            public void Exit(object context) => ExitCount++;
            public void Tick(object context, float deltaTime) => TickCount++;
        }
    }
}
