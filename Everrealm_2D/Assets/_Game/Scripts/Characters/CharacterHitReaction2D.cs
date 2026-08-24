using UnityEngine;

namespace LetterHunter.Characters
{
    [DisallowMultipleComponent]
    public sealed class CharacterHitReaction2D : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [Min(0.01f), SerializeField] private float defaultMovementLockDuration = 0.18f;
        [Min(0.01f), SerializeField] private float blinkInterval = 0.08f;

        private Rigidbody2D _body;
        private Renderer[] _renderers;
        private float _movementLockRemaining;
        private float _invulnerabilityRemaining;
        private float _blinkRemaining;
        private bool _visible = true;

        public bool IsMovementLocked => _movementLockRemaining > 0f;
        public bool IsInvulnerable => _invulnerabilityRemaining > 0f;

        private void Awake()
        {
            TryGetComponent(out _body);
            CacheRenderers();
        }

        private void Update()
        {
            TickMovementLock();
            TickInvulnerability();
        }

        public void ApplyKnockback(Vector2 attackDirection, float horizontalVelocity, float verticalVelocity,
            float movementLockDuration = -1f)
        {
            if (_body == null)
                return;

            var direction = attackDirection.sqrMagnitude > 0f ? attackDirection.normalized : Vector2.right;
            var horizontalSign = direction.x < -0.001f ? -1f : 1f;
            var current = _body.linearVelocity;
            _body.linearVelocity = new Vector2(horizontalSign * Mathf.Max(0f, horizontalVelocity),
                Mathf.Max(current.y, Mathf.Max(0f, verticalVelocity)));
            _movementLockRemaining = movementLockDuration > 0f ? movementLockDuration : defaultMovementLockDuration;
        }

        public void BeginInvulnerability(float duration)
        {
            _invulnerabilityRemaining = Mathf.Max(_invulnerabilityRemaining, duration);
            _blinkRemaining = 0f;
            SetVisible(false);
        }

        private void TickMovementLock()
        {
            if (_movementLockRemaining <= 0f)
                return;

            _movementLockRemaining -= Time.deltaTime;
        }

        private void TickInvulnerability()
        {
            if (_invulnerabilityRemaining <= 0f)
            {
                SetVisible(true);
                return;
            }

            _invulnerabilityRemaining -= Time.deltaTime;
            _blinkRemaining -= Time.deltaTime;
            if (_blinkRemaining > 0f)
                return;

            _blinkRemaining = blinkInterval;
            SetVisible(!_visible);
        }

        private void CacheRenderers()
        {
            var root = visualRoot != null ? visualRoot : transform;
            _renderers = root.GetComponentsInChildren<Renderer>(true);
        }

        private void SetVisible(bool visible)
        {
            if (_visible == visible)
                return;

            _visible = visible;
            if (_renderers == null || _renderers.Length == 0)
                CacheRenderers();

            foreach (var renderer in _renderers)
                if (renderer != null)
                    renderer.enabled = visible;
        }
    }
}
