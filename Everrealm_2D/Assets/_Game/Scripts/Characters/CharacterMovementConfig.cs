using UnityEngine;

namespace LetterHunter.Characters
{
    [CreateAssetMenu(menuName = "Letter Hunter/Characters/Movement Config")]
    public sealed class CharacterMovementConfig : ScriptableObject
    {
        [Min(0f), SerializeField] private float moveSpeed = 6f;
        [Min(0f), SerializeField] private float acceleration = 45f;
        [Min(0f), SerializeField] private float deceleration = 55f;
        [Min(0f), SerializeField] private float jumpForce = 12f;
        [Min(0f), SerializeField] private float gravityScale = 3f;
        [Range(0f, 1f), SerializeField] private float airControl = .65f;
        [Min(0f), SerializeField] private float minimumFallDistance = .5f;

        [Min(0f), SerializeField] private float coyoteTime = .1f;
        [Min(0f), SerializeField] private float jumpBufferTime = .12f;

        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float MoveSpeed => moveSpeed;
        public float Acceleration => acceleration;
        public float Deceleration => deceleration;
        public float JumpForce => jumpForce;
        public float GravityScale => gravityScale;
        public float AirControl => airControl;
        /// <summary>Vertical distance that must be lost before the Fall animation is shown.</summary>
        public float MinimumFallDistance => minimumFallDistance;
    }
}
