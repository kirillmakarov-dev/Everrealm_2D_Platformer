using System.Collections.Generic;
using LetterHunter.Classes;
using LetterHunter.Combat;
using LetterHunter.Core;
using LetterHunter.Effects;
using LetterHunter.Feedback;
using LetterHunter.Items;
using LetterHunter.Skills;
using LetterHunter.Stats;
using LetterHunter.Audio;
using UnityEngine;

namespace LetterHunter.Characters
{
    [DisallowMultipleComponent]
    public sealed class PlayerClassController : MonoBehaviour, ICombatActor, IExperienceRecipient
    {
        [SerializeField] private ClassDefinition classDefinition;
        [SerializeField] private MonoBehaviour targetProviderComponent;
        [SerializeField] private AttackImpactProfile defaultAutoAttackImpact;
        [SerializeField] private AutoAttackComboDefinition autoAttackCombo;
        [SerializeField] private FloatingDamageTextController damageTextController;
        [SerializeField] private PlayerFeedbackVfx feedbackVfx;
        [SerializeField] private Vector2 facingDirection = Vector2.right;
        [Header("Audio")]
        [SerializeField] private SoundManager soundManager;
        [Header("Hit Reaction")]
        [SerializeField] private CharacterHitReaction2D hitReaction;
        [Min(0f), SerializeField] private float damageKnockbackHorizontal = 5f;
        [Min(0f), SerializeField] private float damageKnockbackVertical = 2.5f;
        [Min(0f), SerializeField] private float damageMovementLockDuration = 0.22f;
        [Min(0f), SerializeField] private float invulnerabilityDuration = 1.25f;

        private readonly List<SkillDefinition> _usableSkills = new();
        private BuffService _buffService;
        private PassiveService _passiveService;
        private SkillService _skillService;
        private AutoAttackService _autoAttackService;
        private EmpowerState _empowerState;

        public Transform Transform => transform;
        public CombatStats Stats { get; private set; }
        public void AddExperience(int amount) => GetComponent<PlayerLevelProgression>()?.AddExperience(amount);
        public CharacterClassType ClassType => classDefinition != null ? classDefinition.ClassType : CharacterClassType.Warrior;
        public ClassDefinition ClassDefinition => classDefinition;
        public bool IsAlive => Stats != null && Stats.CurrentHealth > 0f;
        public Vector2 FacingDirection { get => facingDirection; set { if (value.sqrMagnitude > 0f) facingDirection = value.normalized; } }
        public IReadOnlyList<SkillDefinition> UsableSkills => _usableSkills;
        public SkillService SkillService => _skillService;
        public BuffService BuffService => _buffService;
        public PassiveService PassiveService => _passiveService;
        public AutoAttackService AutoAttackService => _autoAttackService;
        public EmpowerState EmpowerState => _empowerState;

        public bool TryUseConsumable(ItemDefinition item)
        {
            if (item == null || item.ItemType != ItemType.Consumable || Stats == null)
                return false;
            if (item.HealthRestore <= 0f && item.ManaRestore <= 0f)
                return false;

            var canRestoreHealth = item.HealthRestore > 0f && Stats.CurrentHealth < Stats.MaxHealth;
            var canRestoreMana = item.ManaRestore > 0f && Stats.CurrentMana < Stats.MaxMana;
            if (!canRestoreHealth && !canRestoreMana)
                return false;

            if (canRestoreHealth)
                Stats.Heal(item.HealthRestore);
            if (canRestoreMana)
                Stats.RestoreMana(item.ManaRestore);
            if (canRestoreHealth)
                feedbackVfx?.PlayHealthPotion();
            if (canRestoreMana)
                feedbackVfx?.PlayManaPotion();
            return true;
        }

        private void Awake()
        {
            if (classDefinition == null) { Debug.LogError("Player requires a ClassDefinition.", this); enabled = false; return; }
            if (targetProviderComponent is not ITargetProvider targetProvider)
            { Debug.LogError("Target Provider must implement ITargetProvider.", this); enabled = false; return; }

            Stats = new CombatStats(classDefinition.BaseStats);
            if (damageTextController == null)
                damageTextController = GetComponent<FloatingDamageTextController>();
            if (damageTextController == null)
                damageTextController = gameObject.AddComponent<FloatingDamageTextController>();
            if (feedbackVfx == null)
                feedbackVfx = GetComponent<PlayerFeedbackVfx>();
            if (hitReaction == null)
                hitReaction = GetComponent<CharacterHitReaction2D>();
            if (hitReaction == null)
                hitReaction = gameObject.AddComponent<CharacterHitReaction2D>();
            if (soundManager == null)
                soundManager = FindFirstObjectByType<SoundManager>();

            RebuildSkillRuntime(targetProvider);
        }

        private void Update()
        {
            var delta = Time.deltaTime;
            _skillService?.Tick(delta);
            _buffService?.Tick(delta);
            _passiveService?.Tick(delta);
        }

        public IReadOnlyList<DamageResult> AutoAttack() => _autoAttackService?.Execute(facingDirection) ?? System.Array.Empty<DamageResult>();

        public void LearnSkill(SkillDefinition skill)
        {
            if (skill == null || string.IsNullOrWhiteSpace(skill.SkillId) || _skillService == null)
                return;
            var tree = GetComponent<LetterHunter.SkillTree.PlayerSkillTreeController>();
            if (tree != null && !tree.IsSkillUnlocked(skill)) return;

            _skillService.Register(skill);
            if (skill.SkillType is SkillType.Passive or SkillType.AutoAttackUpgrade)
                return;

            foreach (var known in _usableSkills)
                if (known != null && known.SkillId == skill.SkillId)
                    return;

            _usableSkills.Add(skill);
        }

        public void ResetLearnedSkills()
        {
            if (targetProviderComponent is ITargetProvider targetProvider)
                RebuildSkillRuntime(targetProvider);
        }

        public bool IsSkillAvailable(SkillDefinition skill)
        {
            if (skill == null || !TryGetSkillRuntimeState(skill.SkillId, out _)) return false;
            var tree = GetComponent<LetterHunter.SkillTree.PlayerSkillTreeController>();
            return tree == null || tree.IsSkillUnlocked(skill);
        }

        public SkillUseResult UseSkill(int index)
        {
            if (_skillService == null || index < 0 || index >= _usableSkills.Count) return SkillUseResult.Failed(SkillUseFailure.NotRegistered);
            return UseSkill(_usableSkills[index].SkillId);
        }

        public SkillUseResult UseSkill(string skillId)
        {
            if (_skillService == null || string.IsNullOrWhiteSpace(skillId))
                return SkillUseResult.Failed(SkillUseFailure.NotRegistered);

            if (!IsSkillAvailable(_usableSkills.Find(skill => skill != null && skill.SkillId == skillId)))
                return SkillUseResult.Failed(SkillUseFailure.NotRegistered);

            return _skillService.TryUse(skillId, null, facingDirection);
        }

        public SkillUseResult TryLaunchSkillProjectile(string skillId, Vector2 position, float speed,
            float lifetime, GameObject fallbackPrefab, out SkillProjectile2D projectile)
        {
            projectile = null;
            if (_skillService == null || string.IsNullOrWhiteSpace(skillId))
                return SkillUseResult.Failed(SkillUseFailure.NotRegistered);

            var skill = _usableSkills.Find(candidate => candidate != null && candidate.SkillId == skillId);
            if (!IsSkillAvailable(skill))
                return SkillUseResult.Failed(SkillUseFailure.NotRegistered);

            var result = _skillService.TryPrepareProjectile(skillId, FacingDirection,
                position, out var cast);
            if (!result.Success) return result;

            var prefab = skill.ProjectilePrefab != null ? skill.ProjectilePrefab : fallbackPrefab;
            var instance = prefab != null
                ? Instantiate(prefab, position, Quaternion.identity)
                : new GameObject($"{skill.DisplayName} Projectile");
            instance.transform.SetPositionAndRotation(new Vector3(position.x, position.y, 0f), Quaternion.identity);
            projectile = instance.GetComponent<SkillProjectile2D>() ?? instance.AddComponent<SkillProjectile2D>();
            var resolvedSpeed = skill.ProjectileSpeed > 0f ? skill.ProjectileSpeed : speed;
            var resolvedLifetime = skill.ProjectileLifetime > 0f ? skill.ProjectileLifetime : lifetime;
            projectile.Launch(this, cast, resolvedSpeed, resolvedLifetime,
                target =>
                {
                    var hitResult = _skillService.ResolveProjectileHit(cast, target);
                    if (hitResult.Success)
                        soundManager?.PlayImpact();
                });
            soundManager?.PlayShot();
            return result;
        }

        public bool TryLaunchBasicProjectile(Vector2 position, float speed, float lifetime,
            GameObject prefab, out SkillProjectile2D projectile)
        {
            projectile = null;
            if (_autoAttackService == null || !IsAlive) return false;
            var instance = prefab != null
                ? Instantiate(prefab, position, Quaternion.identity)
                : new GameObject("Basic Skill Projectile");
            instance.transform.SetPositionAndRotation(new Vector3(position.x, position.y, 0f), Quaternion.identity);
            projectile = instance.GetComponent<SkillProjectile2D>() ?? instance.AddComponent<SkillProjectile2D>();
            projectile.Launch(this, FacingDirection, speed, lifetime,
                target =>
                {
                    var result = _autoAttackService.ExecuteOnTarget(FacingDirection, target);
                    if (result.AppliedSuccessfully)
                        soundManager?.PlayImpact();
                },
                GetEmpowerVisualIcon());
            soundManager?.PlayShot();
            return true;
        }

        private Sprite GetEmpowerVisualIcon()
        {
            if (_empowerState == null || !_empowerState.IsActive || string.IsNullOrWhiteSpace(_empowerState.SourceSkillId))
                return null;
            if (!_empowerState.TryConsumeVisual())
                return null;

            var skill = _usableSkills.Find(candidate => candidate != null &&
                candidate.SkillId == _empowerState.SourceSkillId);
            return skill != null ? skill.Icon : null;
        }

        public bool TryGetSkillRuntimeState(string skillId, out SkillRuntimeState state)
        {
            state = null;
            return _skillService != null && !string.IsNullOrWhiteSpace(skillId) &&
                   _skillService.TryGetRuntimeState(skillId, out state);
        }

        public void ReceiveDamage(DamageResult result)
        {
            if (hitReaction != null && hitReaction.IsInvulnerable)
                return;

            Stats?.TakeDamage(result.FinalDamage);
            damageTextController?.ShowDamage(result);
            hitReaction?.ApplyKnockback(result.AttackDirection, damageKnockbackHorizontal,
                damageKnockbackVertical, damageMovementLockDuration);
            hitReaction?.BeginInvulnerability(invulnerabilityDuration);
        }

        private void RebuildSkillRuntime(ITargetProvider targetProvider)
        {
            var combat = new CombatService();
            _empowerState = new EmpowerState();
            _buffService = new BuffService();
            _passiveService = new PassiveService(this);
            _autoAttackService = new AutoAttackService(this, combat, targetProvider, _empowerState, _buffService, _passiveService);
            _autoAttackService.SetBaseProfile(new AutoAttackSpec { ImpactProfile = defaultAutoAttackImpact });
            _autoAttackService.SetComboDefinition(autoAttackCombo);
            _skillService = new SkillService(this, combat, _buffService, _empowerState, _autoAttackService, targetProvider, _passiveService);

            _usableSkills.Clear();
            foreach (var skill in classDefinition.StartingSkills)
            {
                LearnSkill(skill);
            }
        }
    }
}
