using System;
using UnityEngine;

namespace LetterHunter.Characters
{
    [DefaultExecutionOrder(-100)]
    public sealed class CharacterGroundDetector : MonoBehaviour, IGroundDetector
    {
        [SerializeField] private Transform checkPoint;
        [Min(.01f), SerializeField] private float checkRadius = .15f;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private bool drawGizmo = true;

        public bool IsGrounded { get; private set; }
        public float CheckRadius => checkRadius;
        public event Action Landed;
        public event Action LeftGround;

        private Vector3 _groundedCheckPointLocalPosition;
        private readonly RaycastHit2D[] _groundHits = new RaycastHit2D[16];

        private void Awake()
        {
            if (checkPoint != null)
                _groundedCheckPointLocalPosition = checkPoint.localPosition;
        }

        public bool TryGetGroundDistance(float maxDistance, out float distance)
        {
            distance = 0f;
            if (checkPoint == null || maxDistance <= 0f) return false;

            var parent = checkPoint.parent;
            var origin = parent != null
                ? parent.TransformPoint(_groundedCheckPointLocalPosition)
                : _groundedCheckPointLocalPosition;
            var hitCount = Physics2D.RaycastNonAlloc(origin, Vector2.down, _groundHits,
                maxDistance, groundLayers);
            var nearestDistance = float.PositiveInfinity;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = _groundHits[i];
                if (hit.collider == null || hit.collider.isTrigger ||
                    hit.transform.root == transform.root)
                    continue;
                nearestDistance = Mathf.Min(nearestDistance, hit.distance);
            }

            if (float.IsPositiveInfinity(nearestDistance)) return false;
            distance = nearestDistance;
            return true;
        }

        private void FixedUpdate()
        {
            var position = checkPoint != null ? checkPoint.position : transform.position;
            var grounded = false;
            foreach (var hit in Physics2D.OverlapCircleAll(position, checkRadius, groundLayers))
            {
                if (hit.isTrigger || hit.transform.root == transform.root) continue;
                grounded = true;
                break;
            }
            if (grounded == IsGrounded) return;
            IsGrounded = grounded;
            if (grounded) Landed?.Invoke(); else LeftGround?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo) return;
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(checkPoint != null ? checkPoint.position : transform.position, checkRadius);
        }
    }
}
