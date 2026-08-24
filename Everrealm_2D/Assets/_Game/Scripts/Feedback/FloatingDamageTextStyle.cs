using System;
using UnityEngine;

namespace LetterHunter.Feedback
{
    [Serializable]
    public sealed class FloatingDamageTextStyle
    {
        [SerializeField] private FloatingDamageTextType type = FloatingDamageTextType.Normal;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private float fontSize = 5f;
        [SerializeField] private float lifetime = 0.85f;
        [SerializeField] private Vector2 velocity = new(0f, 1.75f);
        [SerializeField] private Vector2 randomVelocity = new(0.55f, 0.25f);
        [SerializeField] private float gravity = 0f;
        [SerializeField] private float startScale = 0.65f;
        [SerializeField] private float peakScale = 1.15f;
        [SerializeField] private float endScale = 0.8f;
        [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public FloatingDamageTextType Type => type;
        public Color Color => color;
        public float FontSize => Mathf.Max(0.1f, fontSize);
        public float Lifetime => Mathf.Max(0.05f, lifetime);
        public Vector2 Velocity => velocity;
        public Vector2 RandomVelocity => randomVelocity;
        public float Gravity => gravity;
        public float StartScale => Mathf.Max(0.01f, startScale);
        public float PeakScale => Mathf.Max(0.01f, peakScale);
        public float EndScale => Mathf.Max(0.01f, endScale);
        public AnimationCurve AlphaCurve => alphaCurve ?? AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        public AnimationCurve ScaleCurve => scaleCurve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public FloatingDamageTextStyle() { }

        public FloatingDamageTextStyle(FloatingDamageTextType type, Color color, float fontSize, float lifetime,
            Vector2 velocity, Vector2 randomVelocity, float gravity, float startScale, float peakScale, float endScale)
        {
            this.type = type;
            this.color = color;
            this.fontSize = fontSize;
            this.lifetime = lifetime;
            this.velocity = velocity;
            this.randomVelocity = randomVelocity;
            this.gravity = gravity;
            this.startScale = startScale;
            this.peakScale = peakScale;
            this.endScale = endScale;
        }
    }
}
