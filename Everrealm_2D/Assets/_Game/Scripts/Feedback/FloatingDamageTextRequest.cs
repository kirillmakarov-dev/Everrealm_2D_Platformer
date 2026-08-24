using UnityEngine;

namespace LetterHunter.Feedback
{
    public readonly struct FloatingDamageTextRequest
    {
        public FloatingDamageTextRequest(Vector3 worldPosition, string text, FloatingDamageTextType type,
            bool isCritical = false, Transform followTarget = null)
        {
            WorldPosition = worldPosition;
            Text = text ?? string.Empty;
            Type = type;
            IsCritical = isCritical;
            FollowTarget = followTarget;
        }

        public Vector3 WorldPosition { get; }
        public string Text { get; }
        public FloatingDamageTextType Type { get; }
        public bool IsCritical { get; }
        public Transform FollowTarget { get; }
    }
}
