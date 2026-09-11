using UnityEngine;

namespace LetterHunter.Feedback
{
    [DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
    public sealed class LevelUpBannerView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform banner;
        [Header("Screen Positions")]
        [SerializeField] private Vector2 hiddenLeft = new(-760f, 220f);
        [SerializeField] private Vector2 shown = new(0f, 220f);
        [SerializeField] private Vector2 hiddenRight = new(760f, 220f);
        [Header("Timing")]
        [Min(0.05f), SerializeField] private float fadeInDuration = 0.35f;
        [Min(0f), SerializeField] private float holdDuration = 0.95f;
        [Min(0.05f), SerializeField] private float fadeOutDuration = 0.45f;

        private float _elapsed;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            _elapsed = 0f;
            Apply(0f, hiddenLeft);
        }

        private void Update()
        {
            if (canvasGroup == null || banner == null)
                return;

            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < fadeInDuration)
            {
                var progress = Mathf.Clamp01(_elapsed / fadeInDuration);
                var eased = Mathf.SmoothStep(0f, 1f, progress);
                Apply(eased, Vector2.LerpUnclamped(hiddenLeft, shown, eased));
                return;
            }

            var fadeOutStart = fadeInDuration + holdDuration;
            if (_elapsed < fadeOutStart)
            {
                Apply(1f, shown);
                return;
            }

            var fadeProgress = Mathf.Clamp01((_elapsed - fadeOutStart) / fadeOutDuration);
            var fadeEased = Mathf.SmoothStep(0f, 1f, fadeProgress);
            Apply(1f - fadeEased, Vector2.LerpUnclamped(shown, hiddenRight, fadeEased));
        }

        private void Apply(float alpha, Vector2 position)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(alpha);
            if (banner != null)
                banner.anchoredPosition = position;
        }
    }
}
