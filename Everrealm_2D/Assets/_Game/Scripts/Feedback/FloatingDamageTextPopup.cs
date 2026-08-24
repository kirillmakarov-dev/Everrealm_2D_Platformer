using System;
using TMPro;
using UnityEngine;

namespace LetterHunter.Feedback
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class FloatingDamageTextPopup : MonoBehaviour
    {
        private TextMeshPro _label;
        private FloatingDamageTextStyle _style;
        private Action<FloatingDamageTextPopup> _release;
        private Vector3 _velocity;
        private float _elapsed;
        private bool _active;

        private TextMeshPro Label => _label != null ? _label : _label = GetComponent<TextMeshPro>();

        private void Awake()
        {
            EnsureReady();
        }

        public void EnsureReady()
        {
            var label = Label;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.richText = false;

            if (TryGetComponent<MeshRenderer>(out var renderer))
            {
                renderer.sortingOrder = 250;
            }
        }

        public void Play(FloatingDamageTextRequest request, FloatingDamageTextStyle style, Action<FloatingDamageTextPopup> release)
        {
            _style = style;
            _release = release;
            _elapsed = 0f;
            _active = true;

            var label = Label;
            label.text = request.Text;
            label.fontSize = _style.FontSize;
            label.color = _style.Color;

            transform.position = request.WorldPosition;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * _style.StartScale;

            var random = new Vector2(
                UnityEngine.Random.Range(-_style.RandomVelocity.x, _style.RandomVelocity.x),
                UnityEngine.Random.Range(0f, _style.RandomVelocity.y));
            _velocity = _style.Velocity + random;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_active || _style == null) return;

            _elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(_elapsed / _style.Lifetime);

            _velocity.y += _style.Gravity * Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;

            var label = Label;
            var color = _style.Color;
            color.a = Mathf.Clamp01(_style.AlphaCurve.Evaluate(t));
            label.color = color;

            var scaleProgress = Mathf.Clamp01(_style.ScaleCurve.Evaluate(t));
            var scale = t < 0.35f
                ? Mathf.Lerp(_style.StartScale, _style.PeakScale, scaleProgress / 0.35f)
                : Mathf.Lerp(_style.PeakScale, _style.EndScale, (scaleProgress - 0.35f) / 0.65f);
            transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

            if (_elapsed >= _style.Lifetime)
                Release();
        }

        private void Release()
        {
            if (!_active) return;

            _active = false;
            _release?.Invoke(this);
        }
    }
}
