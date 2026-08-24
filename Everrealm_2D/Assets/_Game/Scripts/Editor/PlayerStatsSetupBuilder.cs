#if UNITY_EDITOR
using LetterHunter.Characters;
using LetterHunter.UI.Hud;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace LetterHunter.Editor
{
    public static class PlayerStatsSetupBuilder
    {
        private const string PrefabPath = "Assets/_Game/Prefabs/UI/PlayerStatsPanel.prefab";

        [MenuItem("Letter Hunter/Setup Player Stats UI")]
        public static void SetupCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            var canvas = EnsureCanvas(scene);
            var prefab = EnsurePrefab();
            var existing = FindInScene<PlayerStatsPanelPresenter>(scene);
            if (existing == null)
            {
                var instance = PrefabUtility.InstantiatePrefab(prefab.gameObject, scene) as GameObject;
                existing = instance != null ? instance.GetComponent<PlayerStatsPanelPresenter>() : null;
                if (existing == null)
                {
                    Debug.LogError("Could not instantiate PlayerStatsPanel prefab.");
                    return;
                }
                existing.transform.SetParent(canvas.transform, false);
            }
            else if (existing.transform.parent != canvas.transform)
            {
                existing.transform.SetParent(canvas.transform, false);
            }

            ConfigurePanelRect((RectTransform)existing.transform);
            AssignPlayer(existing, scene);
            EditorUtility.SetDirty(existing);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Player Stats UI setup complete. Press P in Play Mode to toggle it.");
        }

        [MenuItem("Letter Hunter/Rebuild Default Player Stats UI Prefab")]
        public static void RebuildPrefab()
        {
            BuildPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Default Player Stats panel prefab rebuilt.");
        }

        private static PlayerStatsPanelPresenter EnsurePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<PlayerStatsPanelPresenter>(PrefabPath);
            return existing != null ? existing : BuildPrefab();
        }

        private static PlayerStatsPanelPresenter BuildPrefab()
        {
            var root = CreateUIObject("PlayerStatsPanel", new Vector2(300f, 330f));
            var background = root.AddComponent<Image>();
            background.color = new Color(0.045f, 0.055f, 0.075f, 0.96f);
            var group = root.AddComponent<CanvasGroup>();

            var title = CreateText(root.transform, "Title", new Vector2(260f, 34f), new Vector2(0f, 140f),
                "PLAYER STATS", 22f, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(0.95f, 0.8f, 0.3f);

            var level = CreateRow(root.transform, "Level", 96f);
            var attack = CreateRow(root.transform, "Attack", 56f);
            var defense = CreateRow(root.transform, "Defense", 16f);
            var moveSpeed = CreateRow(root.transform, "Move Speed", -24f);
            var attackSpeed = CreateRow(root.transform, "Attack Speed", -64f);
            var jumpHeight = CreateRow(root.transform, "Jump Height", -104f);

            var presenter = root.AddComponent<PlayerStatsPanelPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("panelGroup").objectReferenceValue = group;
            so.FindProperty("levelText").objectReferenceValue = level;
            so.FindProperty("attackText").objectReferenceValue = attack;
            so.FindProperty("defenseText").objectReferenceValue = defense;
            so.FindProperty("moveSpeedText").objectReferenceValue = moveSpeed;
            so.FindProperty("attackSpeedText").objectReferenceValue = attackSpeed;
            so.FindProperty("jumpHeightText").objectReferenceValue = jumpHeight;
            so.FindProperty("startVisible").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<PlayerStatsPanelPresenter>();
        }

        private static TMP_Text CreateRow(Transform parent, string label, float y)
        {
            var row = CreateUIObject(label.Replace(" ", string.Empty) + "Row", new Vector2(252f, 34f));
            row.transform.SetParent(parent, false);
            ((RectTransform)row.transform).anchoredPosition = new Vector2(0f, y);
            var image = row.AddComponent<Image>();
            image.color = new Color(0.09f, 0.11f, 0.15f, 0.9f);
            image.raycastTarget = false;
            var name = CreateText(row.transform, "Label", new Vector2(150f, 28f), new Vector2(-46f, 0f),
                label, 15f, TextAlignmentOptions.Left);
            name.color = new Color(0.72f, 0.78f, 0.88f);
            var value = CreateText(row.transform, "Value", new Vector2(78f, 28f), new Vector2(80f, 0f),
                "0", 17f, TextAlignmentOptions.Right);
            value.fontStyle = FontStyles.Bold;
            value.color = Color.white;
            return value;
        }

        private static Canvas EnsureCanvas(Scene scene)
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.gameObject.scene == scene && canvas.name == "PlayerStatsCanvas")
                    return canvas;

            var go = new GameObject("PlayerStatsCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(go, scene);
            var result = go.GetComponent<Canvas>();
            result.renderMode = RenderMode.ScreenSpaceOverlay;
            result.sortingOrder = 90;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            EnsureEventSystem(scene);
            return result;
        }

        private static void AssignPlayer(PlayerStatsPanelPresenter presenter, Scene scene)
        {
            var player = FindInScene<PlayerClassController>(scene);
            var so = new SerializedObject(presenter);
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("characterRoot").objectReferenceValue = player != null
                ? player.GetComponent<CharacterRoot>()
                : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePanelRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-24f, -24f);
            rect.sizeDelta = new Vector2(300f, 330f);
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (FindInScene<EventSystem>(scene) != null)
                return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            SceneManager.MoveGameObjectToScene(go, scene);
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        private static T FindInScene<T>(Scene scene) where T : Object
        {
            foreach (var item in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
                if (item is Component component && component.gameObject.scene == scene)
                    return item;
            return null;
        }

        private static GameObject CreateUIObject(string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            ((RectTransform)go.transform).sizeDelta = size;
            return go;
        }

        private static TMP_Text CreateText(Transform parent, string name, Vector2 size, Vector2 position,
            string text, float fontSize, TextAlignmentOptions alignment)
        {
            var go = CreateUIObject(name, size);
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).anchoredPosition = position;
            var value = go.AddComponent<TextMeshProUGUI>();
            value.text = text;
            value.fontSize = fontSize;
            value.alignment = alignment;
            value.raycastTarget = false;
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.overflowMode = TextOverflowModes.Ellipsis;
            return value;
        }
    }
}
#endif
