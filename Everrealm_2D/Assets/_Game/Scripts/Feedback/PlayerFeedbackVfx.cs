using LetterHunter.Stats;
using UnityEngine;

namespace LetterHunter.Feedback
{
    [DisallowMultipleComponent]
    public sealed class PlayerFeedbackVfx : MonoBehaviour
    {
        [SerializeField] private PlayerLevelProgression progression;
        [SerializeField] private Transform effectAnchor;
        [Header("One Shot Prefabs")]
        [SerializeField] private GameObject levelUpPrefab;
        [SerializeField] private GameObject healthPotionPrefab;
        [SerializeField] private GameObject manaPotionPrefab;
        [Header("Lifetime")]
        [Min(0.1f), SerializeField] private float levelUpLifetime = 2.2f;
        [Min(0.1f), SerializeField] private float potionLifetime = 1.5f;

        private void Awake()
        {
            if (progression == null)
                progression = GetComponent<PlayerLevelProgression>();
            if (effectAnchor == null)
                effectAnchor = transform;
        }

        private void OnEnable()
        {
            if (progression != null)
                progression.LevelIncreased += OnLevelIncreased;
        }

        private void OnDisable()
        {
            if (progression != null)
                progression.LevelIncreased -= OnLevelIncreased;
        }

        public void PlayHealthPotion() => Play(healthPotionPrefab, potionLifetime);

        public void PlayManaPotion() => Play(manaPotionPrefab, potionLifetime);

        public void PlayLevelUp() => Play(levelUpPrefab, levelUpLifetime);

        private void OnLevelIncreased(int previousLevel, int currentLevel)
        {
            if (currentLevel > previousLevel)
                PlayLevelUp();
        }

        private void Play(GameObject prefab, float lifetime)
        {
            if (prefab == null)
                return;

            var anchor = effectAnchor != null ? effectAnchor : transform;
            var instance = Instantiate(prefab, anchor.position, Quaternion.identity, anchor);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            Destroy(instance, Mathf.Max(0.1f, lifetime));
        }
    }
}
