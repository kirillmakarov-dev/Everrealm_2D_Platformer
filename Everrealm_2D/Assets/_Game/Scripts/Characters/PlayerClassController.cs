using System.Collections.Generic;
using LetterHunter.Classes;
using LetterHunter.Combat;
using LetterHunter.Core;
using LetterHunter.Effects;
using LetterHunter.Feedback;
using LetterHunter.Skills;
using LetterHunter.Stats;
using UnityEngine;

namespace LetterHunter.Characters
{
    [DisallowMultipleComponent]
    public sealed class PlayerClassController : MonoBehaviour, ICombatActor
    {
        [SerializeField] private ClassDefinition classDefinition;
        [SerializeField] private MonoBehaviour targetProviderComponent;
        [SerializeField] private AttackImpactProfile defaultAutoAttackImpact;
        [SerializeField] private AutoAttackComboDefinition autoAttackCombo;
        [SerializeField] private FloatingDamageTextController damageTextController;
        [SerializeField] private Vector2 facingDirection = Vector2.right;
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
            if (hitReaction == null)
                hitReaction = GetComponent<CharacterHitReaction2D>();
            if (hitReaction == null)
                hitReaction = gameObject.AddComponent<CharacterHitReaction2D>();

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

        public SkillUseResult UseSkill(int index)
        {
            if (_skillService == null || index < 0 || index >= _usableSkills.Count) return SkillUseResult.Failed(SkillUseFailure.NotRegistered);
            return _skillService.TryUse(_usableSkills[index].SkillId, null, facingDirection);
        }

        public SkillUseResult UseSkill(string skillId)
        {
            if (_skillService == null || string.IsNullOrWhiteSpace(skillId))
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
            if (skill == null)
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
                target => _skillService.ResolveProjectileHit(cast, target));
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
                target => _autoAttackService.ExecuteOnTarget(FacingDirection, target));
            return true;
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
                _skillService.Register(skill);
                if (skill != null && skill.SkillType is not (SkillType.Passive or SkillType.AutoAttackUpgrade))
                    _usableSkills.Add(skill);
            }
        }
    }
}
