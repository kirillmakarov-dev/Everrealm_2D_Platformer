using UnityEngine;

namespace LetterHunter.Characters
{
    public sealed class CharacterFacingView2D : MonoBehaviour, ICharacterFacingView
    {
        [SerializeField] private Transform visualRoot;
        private Vector3 _initialScale;

        private void Awake()
        {
            if (visualRoot == null) visualRoot = transform;
            _initialScale = visualRoot.localScale;
        }

        public void ApplyFacing(Vector2 direction)
        {
            var scale = _initialScale;
            scale.x = Mathf.Abs(_initialScale.x) * (direction.x < 0f ? -1f : 1f);
            visualRoot.localScale = scale;
        }
    }
}
