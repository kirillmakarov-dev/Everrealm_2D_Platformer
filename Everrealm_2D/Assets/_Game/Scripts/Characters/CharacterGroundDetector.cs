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
        public event Action Landed;
        public event Action LeftGround;

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
