using UnityEngine;

namespace LetterHunter.Characters
{
    /// <summary>Receives Animation Events on the visual Animator object and forwards them to CharacterRoot.</summary>
    public sealed class CharacterAnimationEventRelay : MonoBehaviour
    {
        private CharacterRoot _character;
        private void Awake() => _character = GetComponentInParent<CharacterRoot>();
        public void Shoot() => _character?.Shoot();
    }
}
