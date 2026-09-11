using LetterHunter.Combat;
using UnityEngine;

namespace LetterHunter.Characters
{
    [DisallowMultipleComponent]
    //[RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyPatrolAI2D : MonoBehaviour
    {
        [Header("Patrol Zone")]
        [Min(0f), SerializeField] private float leftDistance = 2f;
        [Min(0f), SerializeField] private float rightDistance = 2f;
        [Min(0f), SerializeField] private float patrolSpeed = 1.5f;
        [Min(0f), SerializeField] private float returnToPatrolSpeed = 2.25f;
        [Min(0f), SerializeField] private float edgeWaitTime = 0.35f;

        [Header("Player Detection")]
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private bool requirePlayerTarget = true;
        [Min(0f), SerializeField] private float detectionRange = 5f;
        [Min(0f), SerializeField] private float loseTargetDistance = 7f;
        [Min(0f), SerializeField] private float chaseSpeed = 2.25f;
        [Min(0.1f), SerializeField] private float attackStopDistance = 1.2f;
        [Min(0.05f), SerializeField] private float targetRefreshInterval = 0.2f;

        [Header("Facing")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Enabled for SpriteRenderer visuals. Disabled for a 3D model that must rotate around Y.")]
        [SerializeField] private bool useSpriteFlip = true;
        [SerializeField] private bool visualFacesRightByDefault;

        private readonly Collider2D[] _hits = new Collider2D[8];
        private Rigidbody2D _body;
        private Collider2D _collider;
        private EnemyAttackController2D _attackController;
        private CharacterHitReaction2D _hitReaction;
        private ICombatActor _self;
        private IDamageable _target;
        private Transform _targetTransform;
        private Collider2D _targetCollider;
        private ContactFilter2D _targetFilter;
        private Vector3 _initialScale;
        private Quaternion _initialRotation;
        private float _spawnX;
        private float _edgeWaitRemaining;
        private float _targetRefreshRemaining;
        private int _patrolDirection = 1;

        public int FacingSign => _patrolDirection < 0 ? -1 : 1;

        private float LeftLimit => _spawnX - leftDistance;
        private float RightLimit => _spawnX + rightDistance;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.freezeRotation = true;
            _collider = GetComponent<Collider2D>();
            _attackController = GetComponent<EnemyAttackController2D>();
            _hitReaction = GetComponent<CharacterHitReaction2D>();
            _self = GetComponent<ICombatActor>();
            _targetFilter = new ContactFilter2D();
            _targetFilter.SetLayerMask(targetLayers);
            _targetFilter.useTriggers = true;
            _spawnX = transform.position.x;

            if (visualRoot == null)
                visualRoot = transform;
            _initialScale = visualRoot.localScale;
            _initialRotation = visualRoot.localRotation;
        }

        private void OnDisable()
        {
            if (_body != null)
                SetHorizontalVelocity(0f);
        }

        private void Update()
        {
            _targetRefreshRemaining -= Time.deltaTime;
            if (_targetRefreshRemaining <= 0f)
            {
                _targetRefreshRemaining = targetRefreshInterval;
                RefreshTarget();
            }
        }

        private void FixedUpdate()
        {
            if (_self != null && !_self.IsAlive)
            {
                SetHorizontalVelocity(0f);
                return;
            }

            if (_hitReaction == null)
                _hitReaction = GetComponent<CharacterHitReaction2D>();

            if (_hitReaction != null && _hitReaction.IsMovementLocked)
                return;

            if (_target != null && _target.IsAlive && _targetTransform != null)
            {
                ChaseTarget();
                return;
            }

            Patrol();
        }

        private void Patrol()
        {
            if (ReturnToPatrolZone())
                return;

            if (_edgeWaitRemaining > 0f)
            {
                _edgeWaitRemaining -= Time.fixedDeltaTime;
                SetHorizontalVelocity(0f);
                return;
            }

            var x = transform.position.x;
            if (x <= LeftLimit)
            {
                _patrolDirection = 1;
                _edgeWaitRemaining = edgeWaitTime;
            }
            else if (x >= RightLimit)
            {
                _patrolDirection = -1;
                _edgeWaitRemaining = edgeWaitTime;
            }

            Face(_patrolDirection);
            MoveWithinZone(_patrolDirection, patrolSpeed);
        }

        private bool ReturnToPatrolZone()
        {
            var x = transform.position.x;
            if (x < LeftLimit)
            {
                _edgeWaitRemaining = 0f;
                _patrolDirection = 1;
                Face(_patrolDirection);
                SetHorizontalVelocity(returnToPatrolSpeed);
                return true;
            }

            if (x > RightLimit)
            {
                _edgeWaitRemaining = 0f;
                _patrolDirection = -1;
                Face(_patrolDirection);
                SetHorizontalVelocity(-returnToPatrolSpeed);
                return true;
            }

            return false;
        }

        private void ChaseTarget()
        {
            var targetPosition = _targetTransform.position;
            var direction = Mathf.Sign(targetPosition.x - transform.position.x);
            if (Mathf.Approximately(direction, 0f))
                direction = _patrolDirection;

            Face(direction);

            var horizontalDistance = GetHorizontalDistanceToTarget();
            if (horizontalDistance <= attackStopDistance)
            {
                SetHorizontalVelocity(0f);
                _attackController?.TryAttack();
                return;
            }

            MoveTowardAttackStopDistance(direction, horizontalDistance);
        }

        private void MoveTowardAttackStopDistance(float direction, float horizontalDistance)
        {
            var remainingDistance = horizontalDistance - attackStopDistance;
            if (remainingDistance <= 0f)
            {
                SetHorizontalVelocity(0f);
                return;
            }

            var frameSpeed = remainingDistance / Time.fixedDeltaTime;
            SetHorizontalVelocity(direction * Mathf.Min(chaseSpeed, frameSpeed));
        }

        private void MoveWithinZone(float direction, float speed)
        {
            var x = transform.position.x;
            if ((direction < 0f && x <= LeftLimit) || (direction > 0f && x >= RightLimit))
            {
                SetHorizontalVelocity(0f);
                return;
            }

            SetHorizontalVelocity(direction * speed);
        }

        private void RefreshTarget()
        {
            if (_target != null && _target.IsAlive && _targetTransform != null)
            {
                var targetPosition = _targetTransform.position;
                var distance = Vector2.Distance(transform.position, targetPosition);
                if (distance <= loseTargetDistance)
                    return;
            }

            ClearTarget();
            FindTargetInDetectionRange();
        }

        private void FindTargetInDetectionRange()
        {
            _targetFilter.SetLayerMask(targetLayers);
            var count = Physics2D.OverlapCircle(transform.position, detectionRange, _targetFilter, _hits);
            IDamageable nearestDamageable = null;
            Transform nearestTransform = null;
            Collider2D nearestCollider = null;
            var nearestDistance = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                if (!TryGetTarget(_hits[i], out var damageable, out var targetTransform, out var targetCollider))
                    continue;

                if (ReferenceEquals(damageable, _self) || !damageable.IsAlive)
                    continue;

                var targetPosition = targetTransform.position;
                var distance = ((Vector2)targetPosition - (Vector2)transform.position).sqrMagnitude;
                if (distance >= nearestDistance)
                    continue;

                nearestDamageable = damageable;
                nearestTransform = targetTransform;
                nearestCollider = targetCollider;
                nearestDistance = distance;
            }

            if (nearestDamageable == null)
                return;

            _target = nearestDamageable;
            _targetTransform = nearestTransform;
            _targetCollider = nearestCollider;
        }

        private bool TryGetTarget(Collider2D collider, out IDamageable damageable, out Transform targetTransform, out Collider2D targetCollider)
        {
            damageable = null;
            targetTransform = null;
            targetCollider = null;

            if (collider == null)
                return false;

            if (requirePlayerTarget)
            {
                var player = collider.GetComponentInParent<PlayerClassController>();
                if (player == null)
                    return false;

                damageable = player;
                targetTransform = player.transform;
                targetCollider = collider;
                return true;
            }

            foreach (var behaviour in collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IDamageable foundDamageable)
                {
                    damageable = foundDamageable;
                    targetTransform = behaviour.transform;
                    targetCollider = collider;
                    return true;
                }
            }

            return false;
        }

        private void ClearTarget()
        {
            _target = null;
            _targetTransform = null;
            _targetCollider = null;
        }

        private float GetHorizontalDistanceToTarget()
        {
            if (_collider == null || _targetCollider == null)
                return Mathf.Abs(_targetTransform.position.x - transform.position.x);

            var selfBounds = _collider.bounds;
            var targetBounds = _targetCollider.bounds;

            if (selfBounds.max.x < targetBounds.min.x)
                return targetBounds.min.x - selfBounds.max.x;
            if (targetBounds.max.x < selfBounds.min.x)
                return selfBounds.min.x - targetBounds.max.x;

            return 0f;
        }

        private void SetHorizontalVelocity(float velocity)
        {
            var current = _body.linearVelocity;
            _body.linearVelocity = new Vector2(velocity, current.y);
        }

        private void Face(float direction)
        {
            if (Mathf.Abs(direction) < 0.001f || visualRoot == null)
                return;

            var requestedFacingSign = direction < 0f ? -1 : 1;
            var defaultFacingSign = visualFacesRightByDefault ? 1 : -1;
            var reverseVisual = requestedFacingSign != defaultFacingSign;

            if (useSpriteFlip)
            {
                var scale = _initialScale;
                scale.x = Mathf.Abs(_initialScale.x) * defaultFacingSign * requestedFacingSign;
                visualRoot.localScale = scale;
            }
            else
            {
                visualRoot.localRotation = _initialRotation * Quaternion.Euler(0f, reverseVisual ? 180f : 0f, 0f);
            }
            _patrolDirection = requestedFacingSign;
        }

        private void OnDrawGizmosSelected()
        {
            var centerX = Application.isPlaying ? _spawnX : transform.position.x;
            var left = centerX - leftDistance;
            var right = centerX + rightDistance;
            var y = transform.position.y;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(left, y, 0f), new Vector3(right, y, 0f));
            Gizmos.DrawWireSphere(new Vector3(left, y, 0f), 0.12f);
            Gizmos.DrawWireSphere(new Vector3(right, y, 0f), 0.12f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, loseTargetDistance);
        }
    }
}
