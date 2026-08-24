#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using LetterHunter.Items;
using LetterHunter.Save;
using LetterHunter.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LetterHunter.Editor
{
    public static class SaveSetupBuilder
    {
        private const string DatabaseFolder = "Assets/_Game/Data/Databases";
        private const string ItemDatabasePath = DatabaseFolder + "/ItemDatabase.asset";
        private const string SkillDatabasePath = DatabaseFolder + "/SkillDatabase.asset";

        public static void SetupCurrentScene()
        {
            EnsureDatabases(out var itemDatabase, out var skillDatabase);
            foreach (var player in Object.FindObjectsByType<PlayerInventory>(FindObjectsSortMode.None))
            {
                var save = player.GetComponent<PlayerSaveController>();
                if (save == null)
                    save = player.gameObject.AddComponent<PlayerSaveController>();

                var so = new SerializedObject(save);
                so.FindProperty("itemDatabase").objectReferenceValue = itemDatabase;
                so.FindProperty("skillDatabase").objectReferenceValue = skillDatabase;
                so.FindProperty("loadOnStart").boolValue = true;
                so.FindProperty("autoSaveProgress").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(save);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Save databases setup complete.");
        }

        public static void EnsureDatabases(out ItemDatabase itemDatabase, out SkillDatabase skillDatabase)
        {
            EnsureFolder(DatabaseFolder);
            itemDatabase = LoadOrCreate<ItemDatabase>(ItemDatabasePath);
            skillDatabase = LoadOrCreate<SkillDatabase>(SkillDatabasePath);
            FillDatabase(itemDatabase, "items", AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/_Game/Data/Items" }));
            FillDatabase(skillDatabase, "skills", AssetDatabase.FindAssets("t:SkillDefinition", new[] { "Assets/_Game/Data/Skills" }));
        }

        private static void FillDatabase(Object database, string listPropertyName, string[] guids)
        {
            var assets = new List<Object>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                if (asset != null)
                    assets.Add(asset);
            }

            var so = new SerializedObject(database);
            var list = so.FindProperty(listPropertyName);
            list.arraySize = assets.Count;
            for (var i = 0; i < assets.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrWhiteSpace(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
