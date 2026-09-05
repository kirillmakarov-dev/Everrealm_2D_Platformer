using UnityEngine;

namespace LetterHunter.Environment
{
    /// <summary>Moves authored decorative segments after the camera, without spawning objects.</summary>
    [DefaultExecutionOrder(10000)]
    public sealed class ParallaxLayer2D : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [Tooltip("0 = world-fixed; 1 = follows the camera completely.")]
        [SerializeField] private Vector2 cameraFollow = new(.7f, 1f);
        [Tooltip("Width of the complete repeating set, not one sprite.")]
        [Min(0f), SerializeField] private float repeatWidth;
        [SerializeField] private Transform[] segments = System.Array.Empty<Transform>();

        private Vector3[] _origins;
        private Vector3 _cameraOrigin;

        private void OnEnable()
        {
            if (cameraTransform == null || segments.Length == 0)
            {
                Debug.LogError("ParallaxLayer2D requires a camera and authored segments.", this);
                enabled = false;
                return;
            }
            _cameraOrigin = cameraTransform.position;
            _origins = new Vector3[segments.Length];
            for (var i = 0; i < segments.Length; i++)
                if (segments[i] != null) _origins[i] = segments[i].position;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;
            for (var i = 0; i < segments.Length; i++)
                if (segments[i] != null)
                    segments[i].position = CalculatePosition(_origins[i], _cameraOrigin,
                        cameraTransform.position, cameraFollow, repeatWidth);
        }

        private void OnDisable()
        {
            if (_origins == null) return;
            for (var i = 0; i < segments.Length; i++)
                if (segments[i] != null) segments[i].position = _origins[i];
            _origins = null;
        }

        public static Vector3 CalculatePosition(Vector3 origin, Vector3 cameraOrigin,
            Vector3 cameraPosition, Vector2 follow, float period)
        {
            var delta = cameraPosition - cameraOrigin;
            var result = origin + new Vector3(delta.x * follow.x, delta.y * follow.y, 0f);
            if (period > 0f)
                result.x += Mathf.Round((cameraPosition.x - result.x) / period) * period;
            return result;
        }
    }
}
