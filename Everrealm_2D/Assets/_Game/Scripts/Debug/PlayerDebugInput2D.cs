using LetterHunter.Characters;
using LetterHunter.UI.Skills;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LetterHunter.Debugging
{
    [RequireComponent(typeof(PlayerClassController))]
    public sealed class PlayerDebugInput2D : MonoBehaviour
    {
        private PlayerClassController _player;
        private SkillBarPresenter _skillBar;
        private void Awake() => _player = GetComponent<PlayerClassController>();

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (_skillBar == null)
                _skillBar = FindFirstObjectByType<SkillBarPresenter>();
            if (keyboard.jKey.wasPressedThisFrame) LogAttack();
            if (_skillBar != null && _skillBar.IsAssigningSkill)
                return;
            if (keyboard.uKey.wasPressedThisFrame) LogSkill(0);
            if (keyboard.iKey.wasPressedThisFrame) LogSkill(1);
            if (keyboard.oKey.wasPressedThisFrame) LogSkill(2);
        }

        private void LogAttack() => Debug.Log($"Auto attack resolved {_player.AutoAttack().Count} target(s).", this);
        private void LogSkill(int index)
        {
            var result = _player.UseSkill(index);
            Debug.Log(result.Success ? $"Skill slot {index + 1} used." : $"Skill failed: {result.Failure}", this);
        }
    }
}
