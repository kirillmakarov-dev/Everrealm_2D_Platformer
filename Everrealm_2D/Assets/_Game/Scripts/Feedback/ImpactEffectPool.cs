using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace LetterHunter.Feedback
{
    [DisallowMultipleComponent]
    public sealed class ImpactEffectPool : MonoBehaviour
    {
        private static ImpactEffectPool _instance;

        [SerializeField] private int defaultCapacityPerPrefab = 4;
        [SerializeField] private int maxPoolSizePerPrefab = 32;

        private readonly Dictionary<GameObject, ObjectPool<ImpactEffectInstance>> _pools = new();

        public static ImpactEffectPool Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = FindFirstObjectByType<ImpactEffectPool>();
                if (_instance != null) return _instance;

                var go = new GameObject("ImpactEffectPool");
                _instance = go.AddComponent<ImpactEffectPool>();
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        public static void Play(AttackImpactProfile profile, Vector3 position, Vector2 attackerDirection)
        {
            if (profile == null || profile.HitEffectPrefab == null) return;
            Instance.PlayInternal(profile, position, attackerDirection);
        }

        private void PlayInternal(AttackImpactProfile profile, Vector3 position, Vector2 attackerDirection)
        {
            var prefab = profile.HitEffectPrefab;
            var pool = GetPool(prefab);
            var instance = pool.Get();

            var scale = Vector3.one;
            if (profile.FlipEffectByAttackerDirection && attackerDirection.x < -0.01f)
                scale.x = -1f;

            instance.Play(this, position, Quaternion.identity, scale, profile.EffectLifetime);
        }

        public void Release(ImpactEffectInstance instance)
        {
            if (instance == null) return;

            if (instance.SourcePrefab != null && _pools.TryGetValue(instance.SourcePrefab, out var pool))
            {
                pool.Release(instance);
                return;
            }

            instance.gameObject.SetActive(false);
        }

        private ObjectPool<ImpactEffectInstance> GetPool(GameObject prefab)
        {
            if (_pools.TryGetValue(prefab, out var pool)) return pool;

            pool = new ObjectPool<ImpactEffectInstance>(
                () => CreateInstance(prefab),
                instance => instance.gameObject.SetActive(true),
                instance => instance.gameObject.SetActive(false),
                instance =>
                {
                    if (instance != null) Destroy(instance.gameObject);
                },
                true,
                Mathf.Max(1, defaultCapacityPerPrefab),
                Mathf.Max(1, maxPoolSizePerPrefab));

            _pools[prefab] = pool;
            return pool;
        }

        private ImpactEffectInstance CreateInstance(GameObject prefab)
        {
            var instance = Instantiate(prefab, transform);
            instance.name = $"{prefab.name}_Pooled";

            if (!instance.TryGetComponent<ImpactEffectInstance>(out var effectInstance))
                effectInstance = instance.AddComponent<ImpactEffectInstance>();

            effectInstance.Initialize(prefab);
            instance.SetActive(false);
            return effectInstance;
        }
    }
}
