#if UNITY_EDITOR
using LetterHunter.Economy;
using LetterHunter.Items;
using LetterHunter.Save;
using LetterHunter.UI.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LetterHunter.Editor
{
    public static class ShopSetupBuilder
    {
        private const string RowPrefabPath = "Assets/_Game/Prefabs/UI/ShopItemRow.prefab";

        public static void SetupCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            var presenter = Object.FindFirstObjectByType<ShopWindowPresenter>(FindObjectsInactive.Include);
            var rowPrefab = AssetDatabase.LoadAssetAtPath<ShopItemRowView>(RowPrefabPath);

            if (presenter == null || rowPrefab == null)
            {
                Debug.LogWarning("Shop setup requires the authored ShopWindow prefab, ShopItemRow prefab, and shop catalog asset.");
                return;
            }

            var so = new SerializedObject(presenter);
            so.FindProperty("inventory").objectReferenceValue = FindInScene<PlayerInventory>(scene);
            so.FindProperty("wallet").objectReferenceValue = FindInScene<CurrencyWallet>(scene);
            so.FindProperty("saveController").objectReferenceValue = FindInScene<PlayerSaveController>(scene);
            so.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Shop setup complete. UI remains authored in ShopWindow.prefab.");
        }

        private static T FindInScene<T>(Scene scene) where T : Object
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            return null;
        }
    }
}
#endif
