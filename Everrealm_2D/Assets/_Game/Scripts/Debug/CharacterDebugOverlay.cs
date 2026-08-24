using LetterHunter.Characters;
using UnityEngine;

namespace LetterHunter.Debugging
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterRoot), typeof(PlayerClassController))]
    public sealed class CharacterDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool visible = true;
        [SerializeField] private Vector2 position = new(12f, 12f);
        [Min(260f), SerializeField] private float width = 370f;
        private CharacterRoot _root;
        private PlayerClassController _combat;

        private void Awake()
        {
            _root = GetComponent<CharacterRoot>();
            _combat = GetComponent<PlayerClassController>();
        }

        private void OnGUI()
        {
            if (!visible || _combat == null || _combat.Stats == null) return;
            var skills = _combat.UsableSkills;
            var height = 190f + skills.Count * 22f;
            GUILayout.BeginArea(new Rect(position.x, position.y, width, height), GUI.skin.box);
            GUILayout.Label($"CLASS: {_combat.ClassType}   STATE: {_root.Runtime?.CurrentState}");
            GUILayout.Label($"HP: {_combat.Stats.CurrentHealth:0.##} / {_combat.Stats.MaxHealth:0.##}");
            GUILayout.Label($"MANA: {_combat.Stats.CurrentMana:0.##} / {_combat.Stats.MaxMana:0.##}");
            GUILayout.Label($"ATK: {_combat.Stats.AttackPower:0.##}   DEF: {_combat.Stats.Defense:0.##}   ASPD: {_combat.Stats.AttackSpeed:0.##}");
            GUILayout.Label($"VELOCITY: {_root.Runtime?.CurrentVelocity}   GROUNDED: {_root.Runtime?.Grounded}");
            GUILayout.Label(_combat.EmpowerState?.IsActive == true
                ? $"EMPOWER: {_combat.EmpowerState.SourceSkillId} (armed)"
                : "EMPOWER: none");
            GUILayout.Space(4f);
            for (var i = 0; i < skills.Count; i++)
            {
                var cooldown = 0f;
                if (_combat.SkillService != null && _combat.SkillService.TryGetRuntimeState(skills[i].SkillId, out var state))
                    cooldown = state.CooldownRemaining;
                GUILayout.Label($"[{SlotKey(i)}] {skills[i].DisplayName} — CD {cooldown:0.0}s");
            }
            GUILayout.Label("A/D Move | Space Jump | J Attack | U/I/O/P Skills");
            GUILayout.EndArea();
        }

        private static string SlotKey(int index) => index switch { 0 => "U", 1 => "I", 2 => "O", 3 => "P", _ => "?" };
    }
}
