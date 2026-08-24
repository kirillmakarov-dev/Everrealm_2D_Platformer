using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LetterHunter.Characters
{
    public sealed class CharacterInputRouter : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Key moveLeftKey = Key.A;
        [SerializeField] private Key moveRightKey = Key.D;
        [SerializeField] private Key jumpKey = Key.Space;

        [Header("Combat")]
        [SerializeField] private Key attackKey = Key.J;
        [SerializeField] private Key[] skillKeys = { Key.U, Key.I, Key.O, Key.P };
        [SerializeField] private Key[] secondarySkillKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };

        public event Action<Vector2> MoveRequested;
        public event Action JumpRequested;
        public event Action AttackRequested;
        public event Action<int> SkillRequested;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            var move = Vector2.zero;
            if (IsPressed(keyboard, moveLeftKey)) move.x -= 1f;
            if (IsPressed(keyboard, moveRightKey)) move.x += 1f;
            MoveRequested?.Invoke(move);
            if (WasPressedThisFrame(keyboard, jumpKey)) JumpRequested?.Invoke();
            if (WasPressedThisFrame(keyboard, attackKey)) AttackRequested?.Invoke();

            for (var i = 0; i < skillKeys.Length; i++)
                if (WasPressedThisFrame(keyboard, skillKeys[i]) || WasSecondarySkillPressed(keyboard, i))
                    SkillRequested?.Invoke(i);
        }

        private static bool IsPressed(Keyboard keyboard, Key key)
        {
            return keyboard[key]?.isPressed == true;
        }

        private static bool WasPressedThisFrame(Keyboard keyboard, Key key)
        {
            return keyboard[key]?.wasPressedThisFrame == true;
        }

        private bool WasSecondarySkillPressed(Keyboard keyboard, int index)
        {
            return secondarySkillKeys != null &&
                   index >= 0 &&
                   index < secondarySkillKeys.Length &&
                   WasPressedThisFrame(keyboard, secondarySkillKeys[index]);
        }
    }
}
