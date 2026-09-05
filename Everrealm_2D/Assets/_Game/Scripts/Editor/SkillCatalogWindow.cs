using System.Linq;
using LetterHunter.Skills;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.EditorTools
{
    public sealed class SkillCatalogWindow : EditorWindow
    {
        private Vector2 _scroll;
        private SkillDefinition[] _skills;
        [MenuItem("Tools/Everrealm/Skill Tree/All Skills — Effects and Types")]
        public static void Open() => GetWindow<SkillCatalogWindow>("Skill Reference");
        private void OnEnable() => Refresh();
        private void Refresh() => _skills = AssetDatabase.FindAssets("t:SkillDefinition", new[] { "Assets/_Game/Data" })
            .Select(g => AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(s => s != null).OrderBy(s => s.ClassType).ThenBy(s => s.DisplayName).ToArray();
        private void OnGUI()
        {
            if (GUILayout.Button("Refresh skills")) Refresh();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var skill in _skills ?? System.Array.Empty<SkillDefinition>())
            {
                if (skill == null) continue;
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.ObjectField(skill.DisplayName + " / " + skill.ClassType, skill, typeof(SkillDefinition), false);
                    EditorGUILayout.LabelField(SkillDescription.Build(skill), EditorStyles.wordWrappedLabel);
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
