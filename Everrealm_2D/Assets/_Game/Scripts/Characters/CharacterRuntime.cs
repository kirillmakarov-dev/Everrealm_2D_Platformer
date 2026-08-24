using UnityEngine;

namespace LetterHunter.Characters
{
    public enum CharacterStateId { Idle, Run, Jump, Fall, Attack }

    public sealed class CharacterRuntime
    {
        public Vector2 CurrentVelocity { get; internal set; }
        public Vector2 FacingDirection { get; internal set; } = Vector2.right;
        public CharacterStateId CurrentState { get; internal set; } = CharacterStateId.Idle;
        public bool Grounded { get; internal set; }
        public bool Jumping => CurrentState == CharacterStateId.Jump;
        public bool Falling => CurrentState == CharacterStateId.Fall;
    }
}
