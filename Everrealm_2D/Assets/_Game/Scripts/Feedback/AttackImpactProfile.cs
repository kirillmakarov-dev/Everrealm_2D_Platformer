using UnityEngine;

namespace LetterHunter.Feedback
{
    [CreateAssetMenu(menuName = "Everrealm/Feedback/Attack Impact Profile")]
    public sealed class AttackImpactProfile : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string impactId = "normal_hit";
        [SerializeField] private string displayName = "Normal Hit";

        [Header("Floating Text")]
        [SerializeField] private FloatingDamageTextType floatingTextType = FloatingDamageTextType.Normal;
        [SerializeField] private FloatingDamageTextType criticalFloatingTextType = FloatingDamageTextType.Critical;

        [Header("Hit VFX")]
        [SerializeField] private GameObject hitEffectPrefab;
        [Tooltip("Fallback offset from the target pivot when the attack has no collision point.")]
        [SerializeField] private Vector2 hitEffectOffset = new(0f, 0.75f);
        [SerializeField] private bool flipEffectByAttackerDirection = true;
        [Min(0.05f), SerializeField] private float effectLifetime = 1.25f;

        [Header("Debug")]
        [SerializeField] private bool logFeedback;

        public string ImpactId => string.IsNullOrWhiteSpace(impactId) ? name : impactId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public FloatingDamageTextType FloatingTextType => floatingTextType;
        public FloatingDamageTextType CriticalFloatingTextType => criticalFloatingTextType;
        public GameObject HitEffectPrefab => hitEffectPrefab;
        public Vector2 HitEffectOffset => hitEffectOffset;
        public bool FlipEffectByAttackerDirection => flipEffectByAttackerDirection;
        public float EffectLifetime => Mathf.Max(0.05f, effectLifetime);
        public bool LogFeedback => logFeedback;

        public FloatingDamageTextType GetTextType(bool critical)
        {
            return critical ? criticalFloatingTextType : floatingTextType;
        }
    }
}
