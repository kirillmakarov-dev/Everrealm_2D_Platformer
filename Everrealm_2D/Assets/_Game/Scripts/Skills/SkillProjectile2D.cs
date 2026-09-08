using System;
using LetterHunter.Combat;
using UnityEngine;

namespace LetterHunter.Skills
{
    public sealed class SkillProjectile2D : MonoBehaviour
    {
        [Header("Hit")]
        [Min(.01f), SerializeField] private float hitRadius = .12f;
        [SerializeField] private LayerMask hitLayers = ~0;
        [Header("Fallback Visual")]
        [SerializeField] private bool createFallbackVisual = true;
        [SerializeField] private Color fallbackColor = new(1f, .8f, .12f, 1f);
        [Min(.01f), SerializeField] private float fallbackLength = .36f;
        [Min(.01f), SerializeField] private float fallbackWidth = .09f;

        private Vector2 _direction;
        private float _speed;
        private float _remainingLifetime;
        private ICombatActor _owner;
        private Action<IDamageable> _hit;
        private Sprite _skillIcon;
        private static Sprite _fallbackSprite;

        public void Launch(ICombatActor owner, PreparedSkillCast cast, float speed, float lifetime,
            Action<IDamageable> hit)
        {
            _owner = owner;
            _direction = cast.Direction;
            _speed = Mathf.Max(0f, speed);
            _remainingLifetime = Mathf.Max(.01f, lifetime);
            _hit = hit;
            _skillIcon = cast.Definition != null ? cast.Definition.Icon : null;
            transform.right = _direction;
            ApplySkillIconVisual();
            EnsureFallbackVisual();
        }

        public void Launch(ICombatActor owner, Vector2 direction, float speed, float lifetime,
            Action<IDamageable> hit)
        {
            _owner = owner;
            _direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
            _speed = Mathf.Max(0f, speed);
            _remainingLifetime = Mathf.Max(.01f, lifetime);
            _hit = hit;
            _skillIcon = null;
            transform.right = _direction;
            EnsureFallbackVisual();
        }

        private void ApplySkillIconVisual()
        {
            if (_skillIcon == null) return;

            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                renderer.sprite = _skillIcon;
        }

        private void EnsureFallbackVisual()
        {
            if (!createFallbackVisual || GetComponentInChildren<Renderer>(true) != null) return;

            var visual = new GameObject("Projectile Visual");
            visual.transform.SetParent(transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = _skillIcon != null ? _skillIcon : GetFallbackSprite();
            renderer.color = fallbackColor;
            renderer.sortingOrder = 100;
            visual.transform.localScale = new Vector3(fallbackLength / .02f, fallbackWidth / .02f, 1f);
        }

        private static Sprite GetFallbackSprite()
        {
            if (_fallbackSprite != null) return _fallbackSprite;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "Runtime Skill Projectile Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            for (var x = 0; x < 2; x++)
                for (var y = 0; y < 2; y++)
                    texture.SetPixel(x, y, Color.white);
            texture.Apply();
            _fallbackSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f),
                new Vector2(.5f, .5f), 100f);
            _fallbackSprite.name = "Runtime Skill Projectile Sprite";
            return _fallbackSprite;
        }

        private void Update()
        {
            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var start = (Vector2)transform.position;
            var travel = _direction * (_speed * Time.deltaTime);
            var distance = travel.magnitude;
            if (distance > 0f)
            {
                foreach (var hit in Physics2D.CircleCastAll(start, hitRadius, _direction, distance, hitLayers))
                {
                    if (TryResolveHit(hit.collider))
                        return;
                }
            }

            transform.position = start + travel;
            foreach (var collider in Physics2D.OverlapCircleAll(transform.position, hitRadius, hitLayers))
                if (TryResolveHit(collider))
                    return;
        }

        private bool TryResolveHit(Collider2D collider)
        {
            var target = FindDamageable(collider);
            if (target == null || ReferenceEquals(target, _owner) || !target.IsAlive)
                return false;

            _hit?.Invoke(target);
            Destroy(gameObject);
            return true;
        }

        private static IDamageable FindDamageable(Collider2D collider)
        {
            foreach (var behaviour in collider.GetComponentsInParent<MonoBehaviour>(true))
                if (behaviour is IDamageable damageable) return damageable;
            return null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }
    }
}
