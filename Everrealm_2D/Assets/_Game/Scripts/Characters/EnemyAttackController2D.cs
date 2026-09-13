using System;
using LetterHunter.Combat;
using UnityEngine;

namespace LetterHunter.Characters
{
    [DisallowMultipleComponent]
    public sealed class EnemyAttackController2D : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour attackerComponent;
        [SerializeField] private EnemyAttackDefinition attackDefinition;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private bool requirePlayerTarget = true;
        [SerializeField] private float attackRange = 1.2f;
        [SerializeField] private Vector2 attackOffset = new(0.75f, 0f);
        [SerializeField] private bool autoAttack;
        [SerializeField] private bool logAttacks;

        private readonly Collider2D[] _hits = new Collider2D[8];
        private readonly CombatService _combat = new();
        private ContactFilter2D _targetFilter;
        private ICombatActor _attacker;
        private EnemyPatrolAI2D _patrolAI;
        private float _cooldownRemaining;

        public event Action AttackPerformed;

        private void Awake()
        {
            if (attackerComponent == null)
                attackerComponent = GetComponent<MonoBehaviour>();

            _attacker = attackerComponent as ICombatActor ?? GetComponent<ICombatActor>();
            _patrolAI = GetComponent<EnemyPatrolAI2D>();
            _targetFilter = new ContactFilter2D();
            _targetFilter.SetLayerMask(targetLayers);
            _targetFilter.useTriggers = true;
        }

        private void Update()
        {
            if (_cooldownRemaining > 0f)
                _cooldownRemaining -= Time.deltaTime;

            if (autoAttack && _cooldownRemaining <= 0f)
                TryAttack();
        }

        [ContextMenu("Try Attack")]
        public bool TryAttack() => TryAttack(null);

        public bool TryAttack(IDamageable expectedTarget)
        {
            if (_attacker == null || attackDefinition == null || !_attacker.IsAlive || _cooldownRemaining > 0f)
                return false;

            var count = CollectTargetsInAttackArea();
            for (var i = 0; i < count; i++)
            {
                var target = FindDamageable(_hits[i]);
                if (target == null || ReferenceEquals(target, _attacker) || !target.IsAlive) continue;
                if (expectedTarget != null && !ReferenceEquals(target, expectedTarget)) continue;

                ApplyAttack(target);
                _cooldownRemaining = attackDefinition.Cooldown;
                AttackPerformed?.Invoke();
                return true;
            }

            return false;
        }

        public bool IsTargetInRange(IDamageable expectedTarget, Collider2D expectedCollider = null)
        {
            if (expectedTarget == null || !expectedTarget.IsAlive)
                return false;

            var count = CollectTargetsInAttackArea();
            for (var i = 0; i < count; i++)
            {
                if (expectedCollider != null && ReferenceEquals(_hits[i], expectedCollider))
                    return true;

                var target = FindDamageable(_hits[i]);
                if (ReferenceEquals(target, expectedTarget) && target.IsAlive)
                    return true;
            }

            return false;
        }

        private int CollectTargetsInAttackArea()
        {
            var facingSign = CurrentFacingSign();
            var signedOffset = new Vector2(Mathf.Abs(attackOffset.x) * facingSign, attackOffset.y);
            var origin = (Vector2)transform.position + signedOffset;
            _targetFilter.SetLayerMask(targetLayers);
            return Physics2D.OverlapCircle(origin, attackRange, _targetFilter, _hits);
        }

        private void ApplyAttack(IDamageable target)
        {
            var lineCount = Math.Max(1, attackDefinition.DamageLines);
            var lines = new DamageLine[lineCount];
            for (var i = 0; i < lineCount; i++)
                lines[i] = new DamageLine(attackDefinition.DamageMultiplier, attackDefinition.Tags);

            var targetPosition = target is Component component ? component.transform.position : transform.position;
            var direction = ((Vector2)targetPosition - (Vector2)transform.position).normalized;
            if (direction.sqrMagnitude <= 0f)
                direction = CurrentFacingSign() < 0f ? Vector2.left : Vector2.right;

            var result = _combat.ApplyDamage(new DamageRequest(_attacker, target, attackDefinition.BaseDamage,
                lines, attackDefinition.AttackId, attackDefinition.Tags, attackDefinition.ImpactProfile, direction));

            if (logAttacks)
                Debug.Log($"[Enemy Attack] {name} used {attackDefinition.DisplayName}: {result.FinalDamage:0.##}", this);
        }

        private IDamageable FindDamageable(Collider2D collider)
        {
            if (collider == null) return null;

            if (requirePlayerTarget)
                return collider.GetComponentInParent<PlayerClassController>();

            foreach (var behaviour in collider.GetComponentsInParent<MonoBehaviour>())
                if (behaviour is IDamageable damageable)
                    return damageable;

            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            var facingSign = CurrentFacingSign();
            var signedOffset = new Vector2(Mathf.Abs(attackOffset.x) * facingSign, attackOffset.y);
            Gizmos.DrawWireSphere((Vector2)transform.position + signedOffset, attackRange);
        }

        private float CurrentFacingSign()
        {
            if (_patrolAI != null)
                return _patrolAI.FacingSign;

            return transform.localScale.x < 0f ? -1f : 1f;
        }
    }

}
