using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LetterHunter.Characters
{
    public sealed class CharacterInputRouter : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Key moveLeftKey = Key.A;
        [SerializeField] private Key moveRightKey = Key.D;
        [SerializeField] private Key jumpKey = Key.Space;
        [SerializeField] private Key sprintKey = Key.LeftShift;

        [Header("Combat")]
        [SerializeField] private Key attackKey = Key.J;
        [SerializeField] private Key[] skillKeys = Array.Empty<Key>();
        [SerializeField] private Key[] secondarySkillKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };

        public event Action<Vector2> MoveRequested;
        public event Action JumpRequested;
        public event Action AttackRequested;
        public event Action<bool> SprintChanged;
        public event Action<int> SkillRequested;
        public bool IsBlocked { get; private set; }

        public void SetBlocked(bool blocked)
        {
            IsBlocked = blocked;
            if (blocked)
            {
                SprintChanged?.Invoke(false);
                MoveRequested?.Invoke(Vector2.zero);
            }
        }

        private void Update()
        {
            if (IsBlocked)
                return;
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            SprintChanged?.Invoke(IsPressed(keyboard, sprintKey));
            var move = Vector2.zero;
            if (IsPressed(keyboard, moveLeftKey)) move.x -= 1f;
            if (IsPressed(keyboard, moveRightKey)) move.x += 1f;
            MoveRequested?.Invoke(move);
            if (WasPressedThisFrame(keyboard, jumpKey)) JumpRequested?.Invoke();
            var mouse = Mouse.current;
            var mouseAttackPressed = mouse?.leftButton.wasPressedThisFrame == true && !IsPointerOverUi();
            if (WasPressedThisFrame(keyboard, attackKey) || mouseAttackPressed)
                AttackRequested?.Invoke();

            var configuredSkillCount = Mathf.Max(skillKeys?.Length ?? 0, secondarySkillKeys?.Length ?? 0);
            for (var i = 0; i < configuredSkillCount; i++)
                if (WasPrimarySkillPressed(keyboard, i) || WasSecondarySkillPressed(keyboard, i))
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

        private bool WasPrimarySkillPressed(Keyboard keyboard, int index)
        {
            return skillKeys != null &&
                   index >= 0 &&
                   index < skillKeys.Length &&
                   WasPressedThisFrame(keyboard, skillKeys[index]);
        }

        private bool WasSecondarySkillPressed(Keyboard keyboard, int index)
        {
            return secondarySkillKeys != null &&
                   index >= 0 &&
                   index < secondarySkillKeys.Length &&
                   WasPressedThisFrame(keyboard, secondarySkillKeys[index]);
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
