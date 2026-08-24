using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;

namespace LetterHunter.Feedback
{
    [DisallowMultipleComponent]
    public sealed class FloatingDamageTextManager : MonoBehaviour
    {
        private static FloatingDamageTextManager _instance;

        [Header("Pool")]
        [SerializeField] private FloatingDamageTextPopup popupPrefab;
        [SerializeField] private int defaultCapacity = 16;
        [SerializeField] private int maxPoolSize = 128;

        [Header("Spawn")]
        [SerializeField] private bool randomizeSpawnX = true;
        [SerializeField] private float spawnXVariance = 0.25f;
        [SerializeField] private float defaultVerticalOffset = 1.25f;

        [Header("Styles")]
        [SerializeField] private List<FloatingDamageTextStyle> styles = CreateDefaultStyles();

        private ObjectPool<FloatingDamageTextPopup> _pool;
        private readonly Dictionary<FloatingDamageTextType, FloatingDamageTextStyle> _styleByType = new();

        public static FloatingDamageTextManager Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = FindFirstObjectByType<FloatingDamageTextManager>();
                if (_instance != null) return _instance;

                var go = new GameObject("FloatingDamageTextManager");
                _instance = go.AddComponent<FloatingDamageTextManager>();
                return _instance;
            }
        }

        public float DefaultVerticalOffset => defaultVerticalOffset;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            RebuildStyleCache();
            _pool = new ObjectPool<FloatingDamageTextPopup>(CreatePopup, OnGet, OnRelease, OnDestroyPopup,
                true, Mathf.Max(1, defaultCapacity), Mathf.Max(1, maxPoolSize));
        }

        public static void Show(FloatingDamageTextRequest request)
        {
            Instance.ShowInternal(request);
        }

        public void ShowInternal(FloatingDamageTextRequest request)
        {
            if (_pool == null) Awake();

            var popup = _pool.Get();
            var position = request.WorldPosition;
            if (randomizeSpawnX)
                position.x += Random.Range(-spawnXVariance, spawnXVariance);

            var type = request.IsCritical ? FloatingDamageTextType.Critical : request.Type;
            popup.Play(new FloatingDamageTextRequest(position, request.Text, type, request.IsCritical, request.FollowTarget),
                GetStyle(type), ReleasePopup);
        }

        public FloatingDamageTextStyle GetStyle(FloatingDamageTextType type)
        {
            if (_styleByType.Count == 0) RebuildStyleCache();
            if (_styleByType.TryGetValue(type, out var style)) return style;
            if (_styleByType.TryGetValue(FloatingDamageTextType.Normal, out var normal)) return normal;
            return new FloatingDamageTextStyle();
        }

        public void SetPrefab(FloatingDamageTextPopup prefab)
        {
            popupPrefab = prefab;
        }

        private FloatingDamageTextPopup CreatePopup()
        {
            FloatingDamageTextPopup popup;
            if (popupPrefab != null)
            {
                popup = Instantiate(popupPrefab, transform);
            }
            else
            {
                var go = new GameObject("FloatingDamageText");
                go.transform.SetParent(transform, false);
                go.AddComponent<TextMeshPro>();
                popup = go.AddComponent<FloatingDamageTextPopup>();
            }

            popup.EnsureReady();
            popup.gameObject.SetActive(false);
            return popup;
        }

        private static void OnGet(FloatingDamageTextPopup popup)
        {
            popup.gameObject.SetActive(true);
        }

        private static void OnRelease(FloatingDamageTextPopup popup)
        {
            popup.transform.localScale = Vector3.one;
            popup.transform.rotation = Quaternion.identity;
            popup.gameObject.SetActive(false);
        }

        private static void OnDestroyPopup(FloatingDamageTextPopup popup)
        {
            if (popup != null) Destroy(popup.gameObject);
        }

        private void ReleasePopup(FloatingDamageTextPopup popup)
        {
            _pool.Release(popup);
        }

        private void RebuildStyleCache()
        {
            _styleByType.Clear();
            if (styles == null || styles.Count == 0)
                styles = CreateDefaultStyles();

            foreach (var style in styles)
            {
                if (style == null) continue;
                _styleByType[style.Type] = style;
            }
        }

        public static List<FloatingDamageTextStyle> CreateDefaultStyles()
        {
            return new List<FloatingDamageTextStyle>
            {
                new(FloatingDamageTextType.Normal, new Color(1f, 0.92f, 0.75f), 5f, 0.85f, new Vector2(0f, 1.75f), new Vector2(0.45f, 0.2f), 0f, 0.65f, 1.1f, 0.82f),
                new(FloatingDamageTextType.Critical, new Color(1f, 0.82f, 0.12f), 6.5f, 1f, new Vector2(0f, 2.2f), new Vector2(0.6f, 0.35f), -0.35f, 0.75f, 1.35f, 0.9f),
                new(FloatingDamageTextType.Skill, new Color(0.65f, 0.85f, 1f), 5.6f, 0.9f, new Vector2(0f, 1.9f), new Vector2(0.5f, 0.25f), 0f, 0.7f, 1.18f, 0.82f),
                new(FloatingDamageTextType.Fire, new Color(1f, 0.35f, 0.12f), 5.4f, 0.9f, new Vector2(0f, 2f), new Vector2(0.5f, 0.3f), -0.15f, 0.7f, 1.2f, 0.82f),
                new(FloatingDamageTextType.Ice, new Color(0.38f, 0.95f, 1f), 5.4f, 0.95f, new Vector2(0f, 1.65f), new Vector2(0.35f, 0.25f), 0f, 0.7f, 1.15f, 0.82f),
                new(FloatingDamageTextType.Poison, new Color(0.65f, 1f, 0.28f), 5.2f, 1f, new Vector2(0f, 1.55f), new Vector2(0.35f, 0.2f), 0f, 0.7f, 1.1f, 0.82f),
                new(FloatingDamageTextType.Heal, new Color(0.25f, 1f, 0.45f), 5.4f, 0.9f, new Vector2(0f, 1.9f), new Vector2(0.45f, 0.25f), 0f, 0.7f, 1.16f, 0.82f),
                new(FloatingDamageTextType.Blocked, new Color(0.72f, 0.76f, 0.82f), 4.7f, 0.7f, new Vector2(0f, 1.25f), new Vector2(0.25f, 0.15f), 0f, 0.7f, 1f, 0.75f),
                new(FloatingDamageTextType.Miss, new Color(0.8f, 0.8f, 0.8f), 4.7f, 0.7f, new Vector2(0f, 1.25f), new Vector2(0.5f, 0.15f), 0f, 0.7f, 1f, 0.75f)
            };
        }
    }
}
