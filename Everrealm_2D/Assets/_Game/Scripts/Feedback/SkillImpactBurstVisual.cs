using UnityEngine;

namespace LetterHunter.Feedback
{
    [DisallowMultipleComponent]
    public sealed class SkillImpactBurstVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] renderers;
        [Min(0.05f), SerializeField] private float duration = 0.55f;
        [Min(0.01f), SerializeField] private float startScale = 0.35f;
        [Min(0.01f), SerializeField] private float endScale = 1.35f;
        [SerializeField] private float rotationSpeed = 220f;

        private Color[] _baseColors;
        private float _elapsed;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<SpriteRenderer>(true);
            CacheColors();
        }

        private void OnEnable()
        {
            _elapsed = 0f;
            if (_baseColors == null || _baseColors.Length != renderers.Length)
                CacheColors();
            ApplyVisual(0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            ApplyVisual(Mathf.Clamp01(_elapsed / Mathf.Max(0.05f, duration)));
        }

        private void CacheColors()
        {
            renderers ??= System.Array.Empty<SpriteRenderer>();
            _baseColors = new Color[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
                _baseColors[i] = renderers[i] != null ? renderers[i].color : Color.white;
        }

        private void ApplyVisual(float progress)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, progress);
            transform.localRotation = Quaternion.Euler(0f, 0f, rotationSpeed * _elapsed);
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                var color = _baseColors[i];
                color.a *= 1f - progress;
                renderers[i].color = color;
            }
        }
    }
}
