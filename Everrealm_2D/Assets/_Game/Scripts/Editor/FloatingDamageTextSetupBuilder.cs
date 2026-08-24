#if UNITY_EDITOR
using LetterHunter.Debugging;
using LetterHunter.Feedback;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LetterHunter.Editor
{
    public static class FloatingDamageTextSetupBuilder
    {
        private const string PrefabPath = "Assets/_Game/Prefabs/Feedback/FloatingDamageText.prefab";

        [MenuItem("Letter Hunter/Setup Floating Damage Text")]
        public static void SetupCurrentScene()
        {
            EnsureFolder("Assets/_Game/Prefabs/Feedback");
            var prefab = EnsurePrefab();
            var manager = EnsureManager(prefab, SceneManager.GetActiveScene());
            var enemiesUpdated = AttachControllersToEnemies();

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Floating Damage Text setup complete. Prefab: {PrefabPath}. Enemy controllers updated: {enemiesUpdated}.", manager);
        }

        public static FloatingDamageTextPopup EnsurePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<FloatingDamageTextPopup>(PrefabPath);
            if (existing != null) return existing;

            var go = new GameObject("FloatingDamageText");
            var label = go.AddComponent<TextMeshPro>();
            label.text = "12";
            label.fontSize = 5f;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = new Color(1f, 0.92f, 0.75f);

            if (go.TryGetComponent<MeshRenderer>(out var renderer))
                renderer.sortingOrder = 250;

            var popup = go.AddComponent<FloatingDamageTextPopup>();
            popup.EnsureReady();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<FloatingDamageTextPopup>();
        }

        public static FloatingDamageTextManager EnsureManager(FloatingDamageTextPopup prefab)
        {
            return EnsureManager(prefab, SceneManager.GetActiveScene());
        }

        public static FloatingDamageTextManager EnsureManager(FloatingDamageTextPopup prefab, Scene scene)
        {
            var manager = FindManagerInScene(scene);
            if (manager == null)
            {
                var go = new GameObject("FloatingDamageTextManager");
                if (scene.IsValid())
                    SceneManager.MoveGameObjectToScene(go, scene);
                manager = go.AddComponent<FloatingDamageTextManager>();
                Undo.RegisterCreatedObjectUndo(go, "Create Floating Damage Text Manager");
            }

            manager.SetPrefab(prefab);
            return manager;
        }

        private static FloatingDamageTextManager FindManagerInScene(Scene scene)
        {
            if (!scene.IsValid()) return Object.FindFirstObjectByType<FloatingDamageTextManager>();

            foreach (var root in scene.GetRootGameObjects())
            {
                var manager = root.GetComponentInChildren<FloatingDamageTextManager>(true);
                if (manager != null) return manager;
            }

            return null;
        }

        public static int AttachControllersToEnemies()
        {
            var count = 0;
            foreach (var enemy in Object.FindObjectsByType<DummyEnemy2D>(FindObjectsSortMode.None))
            {
                if (enemy.GetComponent<FloatingDamageTextController>() == null)
                {
                    Undo.AddComponent<FloatingDamageTextController>(enemy.gameObject);
                    count++;
                }

                EditorUtility.SetDirty(enemy);
            }

            return count;
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
