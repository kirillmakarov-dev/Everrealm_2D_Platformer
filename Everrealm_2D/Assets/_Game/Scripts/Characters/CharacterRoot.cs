using LetterHunter.Effects;
using LetterHunter.Skills;
using LetterHunter.Stats;
using LetterHunter.Core;
using LetterHunter.Debugging;
using LetterHunter.UI.Skills;
using UnityEngine;
using System;

namespace LetterHunter.Characters
{
    [DisallowMultipleComponent]
    public sealed class CharacterRoot : MonoBehaviour
    {
        [Header("Adapters")]
        [SerializeField] private CharacterMotor2D motor;
        [SerializeField] private CharacterGroundDetector groundDetector;
        [SerializeField] private CharacterFacingView2D facingView;
        [SerializeField] private CharacterAnimationController animationController;
        [SerializeField] private CharacterInputRouter inputRouter;
        [SerializeField] private PlayerClassController combatModule;
        [SerializeField] private PlayerSkillLoadout skillLoadout;
        [SerializeField] private SkillBarPresenter skillBar;
        [SerializeField] private CharacterHitReaction2D hitReaction;
        [Header("Data")]
        [SerializeField] private CharacterMovementConfig movementConfig;
        [Min(1f), SerializeField] private float sprintSpeedMultiplier = 1.5f;
        [Range(.01f, 1f), SerializeField] private float walkBlendTreeValue = .5f;
        [Min(.01f), SerializeField] private float attackStateDuration = .2f;
        [Header("Skill Projectiles")]
        [SerializeField] private Transform projectileSpawnPoint;
        [SerializeField] private GameObject defaultSkillProjectilePrefab;
        [Min(0f), SerializeField] private float skillProjectileSpeed = 12f;
        [Min(.01f), SerializeField] private float skillProjectileLifetime = 2f;

        private CharacterMovementController _movement;
        private CharacterJumpController _jump;
        private CharacterFacingController _facing;
        private CharacterStateMachine _stateMachine;
        private bool _sprintHeld;
        private bool _pendingBasicShot;
        private string _pendingSkillId;
        private bool _pendingSkillProjectile;

        public CharacterRuntime Runtime { get; private set; }
        public CombatStats Stats => combatModule != null ? combatModule.Stats : null;
        public SkillService Skills => combatModule != null ? combatModule.SkillService : null;
        public BuffService Buffs => combatModule != null ? combatModule.BuffService : null;
        public PassiveService Passives => combatModule != null ? combatModule.PassiveService : null;
        public float MoveSpeed => Stats != null ? Stats.MoveSpeed : movementConfig != null ? movementConfig.MoveSpeed : 0f;
        private float EffectiveMoveSpeed => _sprintHeld ? MoveSpeed * Mathf.Max(1f, sprintSpeedMultiplier) : MoveSpeed;
        public float JumpHeight => Stats?.JumpHeight ?? 0f;




        private void Awake()
        {
#if UNITY_EDITOR
            if (GetComponent<CharacterStatsDebug>() == null)
                gameObject.AddComponent<CharacterStatsDebug>();
#endif
            if (motor == null || groundDetector == null || inputRouter == null || movementConfig == null)
            { Debug.LogError("CharacterRoot is missing required adapters or MovementConfig.", this); enabled = false; return; }
            if (hitReaction == null)
                hitReaction = GetComponent<CharacterHitReaction2D>();
            if (skillLoadout == null)
                skillLoadout = GetComponent<PlayerSkillLoadout>();
            if (skillBar == null)
                skillBar = FindFirstObjectByType<SkillBarPresenter>();

            Runtime = new CharacterRuntime();
            motor.ConfigureGravity(movementConfig.GravityScale);
            Stats?.ConfigureJump(movementConfig.JumpForce,
                Mathf.Abs(Physics2D.gravity.y) * movementConfig.GravityScale);
            _movement = new CharacterMovementController(motor, groundDetector, movementConfig, Runtime,
                () => EffectiveMoveSpeed);
            _jump = new CharacterJumpController(motor, groundDetector, movementConfig,
                () => Stats != null ? (float)Stats.JumpVelocity : movementConfig.JumpForce);
            _facing = new CharacterFacingController(Runtime, facingView);
            _stateMachine = new CharacterStateMachine(Runtime);
            _stateMachine.StateChanged += OnStateChanged;
            inputRouter.MoveRequested += OnMove;
            inputRouter.JumpRequested += OnJump;
            inputRouter.AttackRequested += OnAttack;
            inputRouter.SprintChanged += OnSprintChanged;
            inputRouter.SkillRequested += OnSkill;
        }

        private void FixedUpdate()
        {
            if (hitReaction == null)
                hitReaction = GetComponent<CharacterHitReaction2D>();

            if (hitReaction == null || !hitReaction.IsMovementLocked)
                _movement?.Tick(Time.fixedDeltaTime);
            if (combatModule != null) combatModule.FacingDirection = Runtime.FacingDirection;
        }

        private void Start()
        {
            Stats?.ConfigureJump(movementConfig.JumpForce,
                Mathf.Abs(Physics2D.gravity.y) * movementConfig.GravityScale);
        }

        private void Update()
        {
            if (Runtime == null) return;
            Runtime.CurrentVelocity = motor.Velocity;
            Runtime.Grounded = groundDetector.IsGrounded;
            Runtime.IsDead = combatModule != null && !combatModule.IsAlive;
            _stateMachine.Tick(Time.deltaTime);
            if (Runtime.CurrentState == CharacterStateId.Run)
                animationController?.SetMoveSpeed(GetAnimationMoveSpeed());
        }

        private void OnDestroy()
        {
            if (inputRouter == null) return;
            inputRouter.MoveRequested -= OnMove;
            inputRouter.JumpRequested -= OnJump;
            inputRouter.AttackRequested -= OnAttack;
            inputRouter.SprintChanged -= OnSprintChanged;
            inputRouter.SkillRequested -= OnSkill;
        }

        private void OnMove(Vector2 input) { _movement.SetMoveInput(input); _facing.UpdateFacing(input); }
        private void OnSprintChanged(bool sprintHeld) => _sprintHeld = sprintHeld;
        private void OnJump()
        {
            var gravity = Mathf.Max(.01f, Mathf.Abs(Physics2D.gravity.y) * movementConfig.GravityScale);
            // Re-read the asset on every jump so DefaultMovement remains the source of truth.
            Stats?.ConfigureJump(movementConfig.JumpForce, gravity);
            if (!_jump.TryJump()) return;

            // GroundDetector refreshes in FixedUpdate. Mark the runtime airborne now so
            // the state machine cannot overwrite the immediate Jump animation with Idle
            // during the frame between the input event and the next physics step.
            Runtime.Grounded = false;
            // Set the Animator parameter immediately; transition timing stays authored in the Animator.
            animationController?.PlayJump();
        }
        private void OnAttack()
        {
            if (combatModule == null) return;
            _pendingSkillId = null;
            _pendingSkillProjectile = false;
            _pendingBasicShot = true;
            _stateMachine.BeginAttack(attackStateDuration);
        }
        private void OnSkill(int slot)
        {
            if (skillBar == null)
                skillBar = FindFirstObjectByType<SkillBarPresenter>();
            if (skillBar != null && skillBar.IsAssigningSkill)
                return;

            TryUseSkillSlot(slot);
        }

        public SkillUseResult TryUseSkillSlot(int slot)
        {
            if (combatModule == null)
                return LetterHunter.Skills.SkillUseResult.Failed(LetterHunter.Skills.SkillUseFailure.NotRegistered);

            if (skillBar == null)
                skillBar = FindFirstObjectByType<SkillBarPresenter>();
            if (skillBar != null && skillBar.IsAssigningSkill)
                return LetterHunter.Skills.SkillUseResult.Failed(LetterHunter.Skills.SkillUseFailure.NotRegistered);
            if (skillLoadout == null)
                skillLoadout = GetComponent<PlayerSkillLoadout>();
            var skill = skillLoadout != null
                ? skillLoadout.ResolveSkill(combatModule, slot, out _)
                : slot >= 0 && slot < combatModule.UsableSkills.Count ? combatModule.UsableSkills[slot] : null;
            var manaBefore = combatModule.Stats?.CurrentMana ?? 0f;
            if (skill == null)
                return LetterHunter.Skills.SkillUseResult.Failed(LetterHunter.Skills.SkillUseFailure.NotRegistered);
            _pendingBasicShot = false;
            if (skill.SkillType == SkillType.Empower)
            {
                var empowerResult = combatModule.UseSkill(skill.SkillId);
                return empowerResult;
            }
            _pendingSkillId = skill.SkillId;
            _pendingSkillProjectile = true;
            var result = LetterHunter.Skills.SkillUseResult.Succeeded();
            if (!result.Success)
            {
                Debug.LogWarning($"[Skill] Slot {slot + 1} failed: {result.Failure}.", this);
                return result;
            }

            var manaAfter = combatModule.Stats.CurrentMana;
            var message = $"[Skill] {skill.DisplayName} activated. Mana: {manaBefore:0.##} → {manaAfter:0.##}.";
            if (skill.SkillType == SkillType.Empower)
                message += " Empower is armed; press J to apply it to the next Auto Attack.";
            else if (skill.SkillType == SkillType.Buff)
                message += $" Buff duration: {skill.Duration:0.##}s. AttackPower: {combatModule.Stats.AttackPower:0.##}, AttackSpeed: {combatModule.Stats.AttackSpeed:0.##}.";
            else if (skill.SkillType == SkillType.Active)
                message += " Active effect is attached to the projectile.";
            Debug.Log(message, this);
            if (skill.SkillType != SkillType.Empower)
                _stateMachine.BeginAttack(attackStateDuration);
            return result;
        }

        /// <summary>Animation Event receiver. Add an event named Shoot to Shooting.anim.</summary>
        public void Shoot()
        {
            if (_pendingBasicShot)
            {
                _pendingBasicShot = false;
                var launched = combatModule.TryLaunchBasicProjectile(GetProjectileSpawnPosition(),
                    skillProjectileSpeed, skillProjectileLifetime, defaultSkillProjectilePrefab, out _);
                if (launched) Debug.Log("[Combat] Basic projectile launched on animation event.", this);
            }
            if (_pendingSkillProjectile && !string.IsNullOrWhiteSpace(_pendingSkillId))
            {
                var skillId = _pendingSkillId;
                _pendingSkillId = null;
                _pendingSkillProjectile = false;
                combatModule.TryLaunchSkillProjectile(skillId, GetProjectileSpawnPosition(),
                    skillProjectileSpeed, skillProjectileLifetime, defaultSkillProjectilePrefab, out _);
            }
        }

        private Vector2 GetProjectileSpawnPosition()
        {
            if (projectileSpawnPoint != null)
                return projectileSpawnPoint.position;

            var collider = GetComponent<Collider2D>();
            return collider != null ? collider.bounds.center : transform.position;
        }

        private void OnStateChanged(CharacterStateId previous, CharacterStateId next)
        {
            if (animationController == null) return;
            switch (next)
            {
                case CharacterStateId.Idle: animationController.PlayIdle(); break;
                case CharacterStateId.Run: animationController.PlayRun(GetAnimationMoveSpeed()); break;
                case CharacterStateId.Jump: animationController.PlayJump(); break;
                case CharacterStateId.Fall: animationController.PlayFall(); break;
                case CharacterStateId.Attack: animationController.PlayAttack(); break;
                case CharacterStateId.Dead: animationController.PlayDead(); break;
            }
        }

        private float GetAnimationMoveSpeed()
        {
            var normalizedVelocity = Mathf.Abs(Runtime.CurrentVelocity.x) / Mathf.Max(.01f, MoveSpeed);
            return Mathf.Clamp01(normalizedVelocity * (_sprintHeld ? 1f : walkBlendTreeValue));
        }
    }
}
