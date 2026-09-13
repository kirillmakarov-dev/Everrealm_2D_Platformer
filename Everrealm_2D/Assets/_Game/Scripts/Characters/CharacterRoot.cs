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
        private bool _ignoreGroundUntilDescending;
        private float _airbornePeakY;
        private CapsuleCollider2D _playerCollider;
        private Vector2 _groundedColliderSize;
        private Vector2 _groundedColliderOffset;

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

            _playerCollider = GetComponent<CapsuleCollider2D>();
            if (_playerCollider != null)
            {
                _groundedColliderSize = _playerCollider.size;
                _groundedColliderOffset = _playerCollider.offset;
            }

            Runtime = new CharacterRuntime();
            // Establish the initial airborne baseline as well. Some scenes place the
            // player above the first platform, so there may be no grounded frame before
            // gravity starts moving the character.
            _airbornePeakY = transform.position.y;
            Runtime.Grounded = groundDetector.IsGrounded;
            motor.ConfigureGravity(movementConfig.GravityScale);
            Stats?.ConfigureJump(movementConfig.JumpForce,
                Mathf.Abs(Physics2D.gravity.y) * movementConfig.GravityScale);
            _movement = new CharacterMovementController(motor, groundDetector, movementConfig, Runtime,
                () => EffectiveMoveSpeed);
            _jump = new CharacterJumpController(motor, groundDetector, movementConfig,
                () => Stats != null ? (float)Stats.JumpVelocity : movementConfig.JumpForce);
            _facing = new CharacterFacingController(Runtime, facingView);
            _stateMachine = new CharacterStateMachine(Runtime, movementConfig.MinimumFallDistance);
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

            if (Runtime == null) return;
            if (!Runtime.IsDead && (hitReaction == null || !hitReaction.IsMovementLocked))
            {
                _movement?.Tick(Time.fixedDeltaTime);
                Stats?.ConfigureJump(movementConfig.JumpForce,
                    Mathf.Abs(Physics2D.gravity.y) * movementConfig.GravityScale);
                if (_jump.Tick(Time.fixedDeltaTime))
                {
                    _ignoreGroundUntilDescending = true;
                    _airbornePeakY = transform.position.y;
                    Runtime.Grounded = false;
                    _stateMachine.NotifyJump();
                }
            }
            SampleMovement();
            if (combatModule != null) combatModule.FacingDirection = Runtime.FacingDirection;
        }

        private void Start()
        {
            Stats?.ConfigureJump(movementConfig.JumpForce,
                Mathf.Abs(Physics2D.gravity.y) * movementConfig.GravityScale);
        }

        private void SampleMovement()
        {
            Runtime.CurrentVelocity = motor.Velocity;
            if (motor.Velocity.y <= .01f) _ignoreGroundUntilDescending = false;
            var grounded = groundDetector.IsGrounded && !_ignoreGroundUntilDescending;
            if (grounded)
            {
                Runtime.AirborneDropDistance = 0f;
                _airbornePeakY = transform.position.y;
            }
            else
            {
                // Track the highest point reached during this airborne segment. The
                // fall threshold therefore measures actual downward travel, whether
                // the player jumped or simply walked off a ledge.
                _airbornePeakY = Mathf.Max(_airbornePeakY, transform.position.y);
                Runtime.AirborneDropDistance = Mathf.Max(0f, _airbornePeakY - transform.position.y);
            }
            Runtime.Grounded = grounded;
            SetAirborneCollider(!grounded);
        }

        private void SetAirborneCollider(bool airborne)
        {
            if (_playerCollider == null)
                return;

            var airborneHeight = Mathf.Min(_groundedColliderSize.y,
                Mathf.Max(_groundedColliderSize.x, _groundedColliderSize.y * .5f));
            var lowerEdgeTravel = _groundedColliderSize.y - airborneHeight;
            // Recover over a slightly shorter distance so the standing shape returns sooner.
            var recoveryDistance = Mathf.Max(.01f,
                lowerEdgeTravel * .85f + groundDetector.CheckRadius);
            var recovery = 0f;

            if (!airborne)
            {
                recovery = 1f;
            }
            else if (motor.Velocity.y <= 0f &&
                     groundDetector.TryGetGroundDistance(recoveryDistance, out var distanceToGround))
            {
                recovery = 1f - Mathf.Clamp01(distanceToGround / recoveryDistance);
            }

            var currentHeight = Mathf.Lerp(airborneHeight, _groundedColliderSize.y, recovery);
            _playerCollider.size = new Vector2(_groundedColliderSize.x, currentHeight);
            _playerCollider.offset = _groundedColliderOffset +
                Vector2.up * ((_groundedColliderSize.y - currentHeight) * .5f);
        }

        private void Update()
        {
            if (Runtime == null) return;
            // Keep the threshold live so changing the referenced ScriptableObject in the
            // Inspector while testing immediately affects the state machine.
            _stateMachine.SetMinimumFallDistance(movementConfig.MinimumFallDistance);
            Runtime.IsDead = combatModule != null && !combatModule.IsAlive;
            _stateMachine.Tick(Time.deltaTime);
            animationController?.UpdateLocomotion(_stateMachine.LocomotionState, GetAnimationMoveSpeed());
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
            if (Runtime == null || Runtime.IsDead ||
                (hitReaction != null && hitReaction.IsMovementLocked)) return;
            _jump.RequestJump();
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

            if (skillBar != null)
            {
                skillBar.TryActivateSlot(slot);
                return;
            }

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

            // A non-empower skill fires from the animation event, so validate its
            // cooldown before entering Attack. This keeps a cooldown click from
            // starting the shooting animation while preserving the existing
            // mana/cast transaction at the Shoot event.
            if (!combatModule.TryGetSkillRuntimeState(skill.SkillId, out var skillState))
                return LetterHunter.Skills.SkillUseResult.Failed(LetterHunter.Skills.SkillUseFailure.NotRegistered);
            if (!skillState.IsReady)
                return LetterHunter.Skills.SkillUseResult.Failed(LetterHunter.Skills.SkillUseFailure.OnCooldown);

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
