#if UNITY_EDITOR
using LetterHunter.Debugging;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Editor
{
    [CustomEditor(typeof(CharacterStatsDebug))]
    public sealed class CharacterStatsDebugEditor : UnityEditor.Editor
    {
        private float _health;
        private float _mana;
        private float _attackPower;
        private float _defense;
        private float _attackSpeed;
        private bool _hasSnapshot;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var debug = (CharacterStatsDebug)target;
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Runtime stats are created in Play Mode. Start Play Mode to inspect and edit them.", MessageType.Info);
                _hasSnapshot = false;
                return;
            }

            if (!debug.IsInitialized)
            {
                EditorGUILayout.HelpBox("Combat stats are not initialized. Check PlayerClassController and ClassDefinition.", MessageType.Warning);
                return;
            }

            if (!_hasSnapshot) ReadSnapshot(debug);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live Runtime Stats", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Live Health", debug.Stats.CurrentHealth);
                EditorGUILayout.FloatField("Live Mana", debug.Stats.CurrentMana);
                EditorGUILayout.FloatField("Live Attack", debug.Stats.AttackPower);
                EditorGUILayout.FloatField("Live Defense", debug.Stats.Defense);
                EditorGUILayout.FloatField("Live Attack Speed", debug.Stats.AttackSpeed);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Test Overrides", EditorStyles.boldLabel);
            _health = EditorGUILayout.Slider("Current Health", _health, 0f, debug.Stats.MaxHealth);
            _mana = EditorGUILayout.Slider("Current Mana", _mana, 0f, debug.Stats.MaxMana);
            _attackPower = EditorGUILayout.FloatField("Attack Power", _attackPower);
            _defense = EditorGUILayout.FloatField("Defense", _defense);
            _attackSpeed = EditorGUILayout.FloatField("Attack Speed", _attackSpeed);
            EditorGUILayout.LabelField("Maximum Health", debug.Stats.MaxHealth.ToString("0.##"));
            EditorGUILayout.LabelField("Maximum Mana", debug.Stats.MaxMana.ToString("0.##"));
            EditorGUILayout.LabelField("Move Speed", debug.Stats.MoveSpeed.ToString("0.##"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Values")) { debug.Apply(_health, _mana, _attackPower, _defense, _attackSpeed); ReadSnapshot(debug); }
            if (GUILayout.Button("Refresh")) ReadSnapshot(debug);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Restore HP + Mana")) { debug.RestoreResources(); ReadSnapshot(debug); }
            if (GUILayout.Button("Clear Stat Overrides")) { debug.ClearDebugModifiers(); ReadSnapshot(debug); }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Take 10 Damage")) { debug.Stats.TakeDamage(10f); ReadSnapshot(debug); }
        }

        private void ReadSnapshot(CharacterStatsDebug debug)
        {
            _health = debug.Stats.CurrentHealth;
            _mana = debug.Stats.CurrentMana;
            _attackPower = debug.Stats.AttackPower;
            _defense = debug.Stats.Defense;
            _attackSpeed = debug.Stats.AttackSpeed;
            _hasSnapshot = true;
            Repaint();
        }
    }
}
#endif
