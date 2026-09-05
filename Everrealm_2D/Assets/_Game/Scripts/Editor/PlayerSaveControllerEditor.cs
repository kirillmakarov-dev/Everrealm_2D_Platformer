#if UNITY_EDITOR
using LetterHunter.Save;
using LetterHunter.SkillTree;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Editor
{
    [CustomEditor(typeof(PlayerSaveController))]
    public sealed class PlayerSaveControllerEditor : UnityEditor.Editor
    {
        private int _gold = 100;
        private string _status;
        public override bool RequiresConstantRepaint() => Application.isPlaying;
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            PlayerSaveTools.Draw((PlayerSaveController)target, ref _gold, ref _status);
        }
    }

    public sealed class PlayerSaveInspectorWindow : EditorWindow
    {
        private PlayerSaveController _player;
        private int _gold = 100;
        private string _status;
        private Vector2 _scroll;

        [MenuItem("Tools/Everrealm/Save Inspector")]
        [MenuItem("Everrealm/Save/Runtime Inspector")]
        public static void Open() => GetWindow<PlayerSaveInspectorWindow>("Save Inspector");
        private void OnInspectorUpdate() => Repaint();
        private void OnGUI()
        {
            if (Application.isPlaying && _player == null)
                _player = FindFirstObjectByType<PlayerSaveController>();
            _player = (PlayerSaveController)EditorGUILayout.ObjectField("Live player", _player,
                typeof(PlayerSaveController), true);
            using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
            _scroll = scroll.scrollPosition;
            PlayerSaveTools.Draw(_player, ref _gold, ref _status);
        }
    }

    internal static class PlayerSaveTools
    {
        public static void Draw(PlayerSaveController controller, ref int gold, ref string status)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Save Tools", EditorStyles.boldLabel);
            if (!Application.isPlaying || controller == null || EditorUtility.IsPersistent(controller))
            {
                EditorGUILayout.HelpBox("Enter Play Mode and select the live player to edit saved progress.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("Save file", controller.SavePath, EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Now")) controller.SaveToDisk();
                if (GUILayout.Button("Load Save")) controller.LoadFromDisk();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Coins: {controller.CurrentGold}", EditorStyles.boldLabel);
            gold = Mathf.Max(1, EditorGUILayout.IntField("Amount", gold));
            if (GUILayout.Button("Add Coins + Save"))
            {
                controller.AddDebugGold(gold);
                status = $"Added {gold} coins and saved.";
            }

            var tree = controller.SkillTreeController;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Skill Tree", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Buy uses normal level, parent and price rules. Grant + Parents unlocks the selected branch for free, ignoring level. Both update the live player and save.", MessageType.None);
            if (tree?.ActiveProfession != null && tree.Player?.SkillService != null)
            {
                EditorGUILayout.LabelField($"{tree.ActiveProfession.DisplayName} | Level {tree.CurrentLevel}");
                foreach (var node in tree.ActiveProfession.SkillNodes)
                {
                    if (node == null) continue;
                    var state = tree.Service.GetState(node);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(node.DisplayName, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"{state} | Level {node.RequiredLevel} | {node.Price} coins");
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            using (new EditorGUI.DisabledScope(state != SkillTreeNodeState.Available))
                                if (GUILayout.Button("Buy")) controller.TryDebugSkill(node, false, out status);
                            using (new EditorGUI.DisabledScope(state == SkillTreeNodeState.Purchased))
                                if (GUILayout.Button("Grant + Parents")) controller.TryDebugSkill(node, true, out status);
                        }
                    }
                }
            }
            else EditorGUILayout.HelpBox("Player Skill Tree is not ready.", MessageType.Warning);
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset Gold")) controller.ResetGold();
                if (GUILayout.Button("Reset Inventory") && Confirm("inventory")) controller.ResetInventory();
                if (GUILayout.Button("Reset Skills") && Confirm("skills and skill bar")) controller.ResetSkills();
            }
            if (GUILayout.Button("Reset All Progress") && Confirm("all progress")) controller.ResetProgress();
        }

        private static bool Confirm(string scope) => EditorUtility.DisplayDialog("Reset progress",
            $"Reset {scope} on this player and overwrite the save?", "Reset", "Cancel");
    }
}
#endif
