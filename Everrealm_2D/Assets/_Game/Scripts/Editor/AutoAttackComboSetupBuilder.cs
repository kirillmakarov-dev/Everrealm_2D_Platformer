#if UNITY_EDITOR
using LetterHunter.Characters;
using LetterHunter.Combat;
using LetterHunter.Feedback;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Editor
{
    public static class AutoAttackComboSetupBuilder
    {
        private const string ComboFolder = "Assets/_Game/Data/Combat/Combos";
        private const string WarriorComboPath = ComboFolder + "/WarriorBasicCombo.asset";

        [MenuItem("Letter Hunter/Setup Auto Attack Combo")]
        public static void SetupCurrentScene()
        {
            var profiles = AttackImpactSetupBuilder.EnsureDefaultProfiles();
            var combo = EnsureWarriorBasicCombo(profiles.TryGetValue("NormalSlash", out var normal) ? normal : null,
                profiles.TryGetValue("HeavySlash", out var heavy) ? heavy : null);
            AssignComboToPlayers(combo);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Auto Attack Combo setup complete. Basic J attack now supports a 3-hit combo.", combo);
        }

        public static AutoAttackComboDefinition EnsureWarriorBasicCombo(AttackImpactProfile normalImpact, AttackImpactProfile finisherImpact)
        {
            EnsureFolder(ComboFolder);
            var combo = AssetDatabase.LoadAssetAtPath<AutoAttackComboDefinition>(WarriorComboPath);
            if (combo == null)
            {
                combo = ScriptableObject.CreateInstance<AutoAttackComboDefinition>();
                AssetDatabase.CreateAsset(combo, WarriorComboPath);
            }

            var so = new SerializedObject(combo);
            so.FindProperty("comboId").stringValue = "warrior_basic_combo";
            so.FindProperty("resetTime").floatValue = 0.85f;

            var steps = so.FindProperty("steps");
            steps.arraySize = 3;
            ConfigureStep(steps.GetArrayElementAtIndex(0), "Hit 1", 1f, 0f, 1.5f, normalImpact);
            ConfigureStep(steps.GetArrayElementAtIndex(1), "Hit 2", 1f, 0f, 1.5f, normalImpact);
            ConfigureStep(steps.GetArrayElementAtIndex(2), "Finisher", 1f, 0.35f, 1.6f, finisherImpact);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(combo);
            return combo;
        }

        public static void AssignComboToPlayers(AutoAttackComboDefinition combo)
        {
            if (combo == null) return;

            foreach (var player in Object.FindObjectsByType<PlayerClassController>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(player);
                var property = so.FindProperty("autoAttackCombo");
                if (property != null && property.objectReferenceValue == null)
                {
                    property.objectReferenceValue = combo;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(player);
                }
            }
        }

        private static void ConfigureStep(SerializedProperty step, string displayName, float damageMultiplier,
            float criticalChance, float criticalDamageMultiplier, AttackImpactProfile impactProfile)
        {
            step.FindPropertyRelative("displayName").stringValue = displayName;
            step.FindPropertyRelative("damageMultiplier").floatValue = damageMultiplier;
            step.FindPropertyRelative("criticalChance").floatValue = criticalChance;
            step.FindPropertyRelative("criticalDamageMultiplier").floatValue = criticalDamageMultiplier;
            step.FindPropertyRelative("impactProfile").objectReferenceValue = impactProfile;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
#endif
