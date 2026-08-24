using UnityEngine;

namespace LetterHunter.Characters
{
    public sealed class CharacterFacingController : IFacingController
    {
        private readonly CharacterRuntime _runtime;
        private readonly ICharacterFacingView _view;
        public CharacterFacingController(CharacterRuntime runtime, ICharacterFacingView view)
        { _runtime = runtime; _view = view; }
        public Vector2 Direction => _runtime.FacingDirection;

        public void UpdateFacing(Vector2 movementDirection)
        {
            if (Mathf.Abs(movementDirection.x) < .001f) return;
            _runtime.FacingDirection = movementDirection.x < 0f ? Vector2.left : Vector2.right;
            _view?.ApplyFacing(_runtime.FacingDirection);
        }
    }

}
