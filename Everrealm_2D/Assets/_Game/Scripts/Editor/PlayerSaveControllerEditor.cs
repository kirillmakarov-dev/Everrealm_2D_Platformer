#if UNITY_EDITOR
using LetterHunter.Save;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Editor
{
    [CustomEditor(typeof(PlayerSaveController))]
    public sealed class PlayerSaveControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Save Tools", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to change and save runtime progress.", MessageType.Info);
                return;
            }

            var controller = (PlayerSaveController)target;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Now"))
                    controller.SaveToDisk();
                if (GUILayout.Button("Load Save"))
                    controller.LoadFromDisk();
            }

            if (GUILayout.Button("Add Debug Gold"))
                controller.AddDebugGold();

            EditorGUILayout.Space(3f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Gold"))
                    controller.ResetGold();
                if (GUILayout.Button("Reset Inventory"))
                    controller.ResetInventory();
                if (GUILayout.Button("Reset Skills"))
                    controller.ResetSkills();
            }

            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.55f, 0.45f);
            if (GUILayout.Button("Reset All Progress") && EditorUtility.DisplayDialog(
                    "Reset all progress?",
                    "Gold, inventory, learned skills, skill tree and skill-bar assignments will return to their starting values.",
                    "Reset All", "Cancel"))
                controller.ResetProgress();
            GUI.backgroundColor = previousColor;
        }
    }
}
#endif
