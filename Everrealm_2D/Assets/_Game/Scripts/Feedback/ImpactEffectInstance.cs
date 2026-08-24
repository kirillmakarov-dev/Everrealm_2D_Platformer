using UnityEngine;

namespace LetterHunter.Feedback
{
    [DisallowMultipleComponent]
    public sealed class ImpactEffectInstance : MonoBehaviour
    {
        private ImpactEffectPool _owner;
        private float _remainingLifetime;

        public GameObject SourcePrefab { get; private set; }

        public void Initialize(GameObject sourcePrefab)
        {
            SourcePrefab = sourcePrefab;
        }

        public void Play(ImpactEffectPool owner, Vector3 position, Quaternion rotation, Vector3 scale, float lifetime)
        {
            _owner = owner;
            _remainingLifetime = Mathf.Max(0.05f, lifetime);
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
            gameObject.SetActive(true);

            foreach (var particle in GetComponentsInChildren<ParticleSystem>(true))
                particle.Play(true);
        }

        private void Update()
        {
            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
                Release();
        }

        private void Release()
        {
            if (_owner != null)
                _owner.Release(this);
            else
                gameObject.SetActive(false);
        }
    }
}
