#if UNITY_EDITOR
using LetterHunter.Economy;
using LetterHunter.Items;
using LetterHunter.Save;
using LetterHunter.UI.Shop;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LetterHunter.Editor
{
    public static class ShopSetupBuilder
    {
        private const string RowPrefabPath = "Assets/_Game/Prefabs/UI/ShopItemRow.prefab";

        public static void SetupCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("Shop setup requires a Canvas in the scene.");
                return;
            }

            var rowPrefab = EnsureRowPrefab();
            var presenter = canvas.GetComponentInChildren<ShopWindowPresenter>(true);
            if (presenter == null)
                presenter = CreateShopWindow(canvas.transform, rowPrefab);

            AssignPresenter(presenter, scene, rowPrefab);
            EditorUtility.SetDirty(presenter);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Shop UI setup complete.");
        }

        private static ShopWindowPresenter CreateShopWindow(Transform canvas, ShopItemRowView rowPrefab)
        {
            var window = CreateUIObject("ShopWindow", canvas, new Vector2(420f, 470f));
            var rect = window.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-36f, 20f);

            window.AddComponent<CanvasGroup>();
            window.GetComponent<Image>().color = new Color(0.06f, 0.07f, 0.08f, 0.96f);

            var title = CreateText(window.transform, "Title", "Shop", new Vector2(360f, 38f), new Vector2(0f, 198f), 28f);
            title.alignment = TextAlignmentOptions.Center;

            var goldLabel = CreateText(window.transform, "GoldText", "0", new Vector2(120f, 32f), new Vector2(132f, 198f), 21f);
            goldLabel.alignment = TextAlignmentOptions.Right;

            var feedback = CreateText(window.transform, "FeedbackText", string.Empty, new Vector2(360f, 32f), new Vector2(0f, -198f), 18f);
            feedback.alignment = TextAlignmentOptions.Center;
            feedback.color = new Color(0.88f, 0.78f, 0.42f, 1f);

            var rows = CreateUIObject("Rows", window.transform, new Vector2(370f, 330f));
            rows.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var rowsRect = rows.GetComponent<RectTransform>();
            rowsRect.anchoredPosition = new Vector2(0f, 0f);
            var layout = rows.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var emptyText = CreateText(rows.transform, "EmptyText", "No sellable items", new Vector2(360f, 32f), Vector2.zero, 18f);
            emptyText.alignment = TextAlignmentOptions.Center;

            var presenter = window.AddComponent<ShopWindowPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("windowGroup").objectReferenceValue = window.GetComponent<CanvasGroup>();
            so.FindProperty("rowRoot").objectReferenceValue = rowsRect;
            so.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            so.FindProperty("emptyText").objectReferenceValue = emptyText;
            so.FindProperty("goldText").objectReferenceValue = goldLabel;
            so.FindProperty("feedbackText").objectReferenceValue = feedback;
            so.ApplyModifiedPropertiesWithoutUndo();
            return presenter;
        }

        private static void AssignPresenter(ShopWindowPresenter presenter, Scene scene, ShopItemRowView rowPrefab)
        {
            var so = new SerializedObject(presenter);
            so.FindProperty("inventory").objectReferenceValue = FindInScene<PlayerInventory>(scene);
            so.FindProperty("wallet").objectReferenceValue = FindInScene<CurrencyWallet>(scene);
            so.FindProperty("saveController").objectReferenceValue = FindInScene<PlayerSaveController>(scene);
            so.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ShopItemRowView EnsureRowPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<ShopItemRowView>(RowPrefabPath);
            if (prefab != null)
                return prefab;

            var row = CreateUIObject("ShopItemRow", null, new Vector2(370f, 44f));
            row.GetComponent<Image>().color = new Color(0.12f, 0.13f, 0.15f, 0.92f);
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;

            var nameText = CreateText(row.transform, "NameText", "Item", new Vector2(118f, 32f), Vector2.zero, 16f);
            var amountText = CreateText(row.transform, "AmountText", "x1", new Vector2(34f, 32f), Vector2.zero, 16f);
            var priceText = CreateText(row.transform, "PriceText", "0g", new Vector2(44f, 32f), Vector2.zero, 16f);
            var sellOne = CreateButton(row.transform, "SellOneButton", "Sell 1", new Vector2(64f, 32f));
            var sellStack = CreateButton(row.transform, "SellStackButton", "Stack", new Vector2(64f, 32f));

            var view = row.AddComponent<ShopItemRowView>();
            var so = new SerializedObject(view);
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("priceText").objectReferenceValue = priceText;
            so.FindProperty("amountText").objectReferenceValue = amountText;
            so.FindProperty("sellOneButton").objectReferenceValue = sellOne;
            so.FindProperty("sellStackButton").objectReferenceValue = sellStack;
            so.ApplyModifiedPropertiesWithoutUndo();

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(row, RowPrefabPath);
            Object.DestroyImmediate(row);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return savedPrefab.GetComponent<ShopItemRowView>();
        }

        private static GameObject CreateUIObject(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            if (parent != null)
                go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return go;
        }

        private static Button CreateButton(Transform parent, string name, string text, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.22f, 0.36f, 0.24f, 1f);

            var buttonText = CreateText(go.transform, "Text", text, size, Vector2.zero, 15f);
            buttonText.alignment = TextAlignmentOptions.Center;
            return go.GetComponent<Button>();
        }

        private static TMP_Text CreateText(Transform parent, string name, string text, Vector2 size, Vector2 anchoredPosition, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var label = go.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
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
