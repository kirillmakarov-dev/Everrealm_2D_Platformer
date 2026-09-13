#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LetterHunter.UI.Pause;
using LetterHunter.Audio;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LetterHunter.EditorTools
{
    public static class PauseMenuSetupBuilder
    {
        private const string AtlasPath = "Assets/SoftKitty/InventoryEngine/Textures/Sprites/Main.png";
        private const string PrefabFolder = "Assets/_Game/Prefabs/UI/Pause";
        private const string PrefabPath = PrefabFolder + "/PauseMenu.prefab";
        private const string ScenePath = "Assets/_Game/Scenes/Game level 1.unity";

        private static readonly Color Gold = new(1f, 0.58f, 0.08f, 1f);
        private static readonly Color Ivory = new(0.94f, 0.91f, 0.84f, 1f);
        private static readonly Color Muted = new(0.62f, 0.66f, 0.66f, 1f);
        private static Dictionary<string, Sprite> _sprites;

        [MenuItem("Tools/Everrealm/UI/Build Pause Menu")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            LoadSprites();
            EnsureFolder(PrefabFolder);
            var prefab = BuildPrefab();
            IntegrateActiveScene(prefab);
            AssetDatabase.SaveAssets();
            Debug.Log("Soft Kitty pause menu prefab built and connected to Game level 1.");
        }

        private static GameObject BuildPrefab()
        {
            var root = NewUi("PauseMenuCanvas", null);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            var group = root.AddComponent<CanvasGroup>();
            var controller = root.AddComponent<PauseMenuController>();

            var scrim = Image("Scrim", root.transform, null, new Color(0.015f, 0.025f, 0.028f, 0.82f));
            Stretch(scrim.rectTransform);

            var glow = Image("WarmGlow", root.transform, Sprite("glow1"), new Color(1f, 0.34f, 0.03f, 0.16f));
            SetRect(glow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 720f));
            glow.raycastTarget = false;

            var card = Image("MenuCard", root.transform, Sprite("bg1", "bg2", "bg3"), new Color(0.13f, 0.14f, 0.14f, 0.99f));
            SetRect(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 670f));
            SetImageType(card);

            var frame = Image("Frame", card.transform, Sprite("frame1", "frame4", "item_frame"), Color.white);
            Stretch(frame.rectTransform, 9f);
            frame.raycastTarget = false;
            SetImageType(frame);

            var headerLine = Image("HeaderLine", card.transform, Sprite("line2", "line5", "bar1"), Gold);
            SetRect(headerLine.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -151f), new Vector2(440f, 8f));
            SetImageType(headerLine);

            Text("PausedLabel", card.transform, "GAME PAUSED", 17f, Muted, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, -62f), new Vector2(440f, 34f), new Vector2(0.5f, 1f));
            Text("Title", card.transform, "MENU", 54f, Ivory, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, -105f), new Vector2(440f, 74f), new Vector2(0.5f, 1f));

            var mainPanel = NewUi("MainPanel", card.transform);
            SetRect((RectTransform)mainPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -42f), new Vector2(420f, 360f));
            var restart = Button("RestartButton", mainPanel.transform, "RESTART", "arrow5", new Vector2(0f, 112f));
            var settings = Button("SettingsButton", mainPanel.transform, "SETTINGS", "icon_craft", new Vector2(0f, 18f));
            var exit = Button("ExitButton", mainPanel.transform, "EXIT", "close", new Vector2(0f, -76f));
            Text("Hint", mainPanel.transform, "ESC  •  RESUME", 16f, Muted, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, -156f), new Vector2(360f, 36f));

            var settingsPanel = NewUi("SettingsPanel", card.transform);
            SetRect((RectTransform)settingsPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(420f, 430f));
            settingsPanel.SetActive(false);
            Text("SettingsTitle", settingsPanel.transform, "SETTINGS", 27f, Ivory, FontStyles.Bold, TextAlignmentOptions.Center,
                new Vector2(0f, 192f), new Vector2(380f, 44f));
            var master = VolumePanel("MasterVolumePanel", settingsPanel.transform, "MASTER", "100%", new Vector2(0f, 137f));
            var music = VolumePanel("MusicVolumePanel", settingsPanel.transform, "MUSIC", "65%", new Vector2(0f, 61f));
            var sfx = VolumePanel("SfxVolumePanel", settingsPanel.transform, "SOUND FX", "80%", new Vector2(0f, -15f));
            var fullscreen = Toggle(settingsPanel.transform, new Vector2(0f, -91f));
            var back = Button("BackButton", settingsPanel.transform, "BACK", "arrow2", new Vector2(0f, -174f));

            var serialized = new SerializedObject(controller);
            serialized.FindProperty("windowGroup").objectReferenceValue = group;
            serialized.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            serialized.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
            serialized.FindProperty("exitButton").objectReferenceValue = exit;
            serialized.FindProperty("restartButton").objectReferenceValue = restart;
            serialized.FindProperty("settingsButton").objectReferenceValue = settings;
            serialized.FindProperty("masterVolumeSlider").objectReferenceValue = master.slider;
            serialized.FindProperty("masterVolumeValueText").objectReferenceValue = master.value;
            serialized.FindProperty("musicVolumeSlider").objectReferenceValue = music.slider;
            serialized.FindProperty("musicVolumeValueText").objectReferenceValue = music.value;
            serialized.FindProperty("sfxVolumeSlider").objectReferenceValue = sfx.slider;
            serialized.FindProperty("sfxVolumeValueText").objectReferenceValue = sfx.value;
            serialized.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
            serialized.FindProperty("backButton").objectReferenceValue = back;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void IntegrateActiveScene(GameObject prefab)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before building the pause menu.");

            foreach (var existing in scene.GetRootGameObjects()
                         .Where(x => x.GetComponent<PauseMenuController>() != null)
                         .ToArray())
                UnityEngine.Object.DestroyImmediate(existing);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Button Button(string name, Transform parent, string label, string iconName, Vector2 position)
        {
            var shadow = Image(name + "Shadow", parent, Sprite("button"), new Color(0f, 0f, 0f, 0.55f));
            SetRect(shadow.rectTransform, new Vector2(0.5f, 0.5f), position + new Vector2(0f, -5f), new Vector2(386f, 82f));
            SetImageType(shadow);
            shadow.raycastTarget = false;

            var image = Image(name, parent, Sprite("button"), new Color(0.31f, 0.30f, 0.28f, 1f));
            SetRect(image.rectTransform, new Vector2(0.5f, 0.5f), position, new Vector2(386f, 82f));
            SetImageType(image);

            var frame = Image("Frame", image.transform, Sprite("button_frame"), new Color(0.72f, 0.66f, 0.56f, 1f));
            Stretch(frame.rectTransform, 2f);
            SetImageType(frame);
            frame.raycastTarget = false;

            var hover = Image("Highlight", image.transform, Sprite("button_glow"), new Color(1f, 0.55f, 0.12f, 0f));
            Stretch(hover.rectTransform, 3f);
            SetImageType(hover);

            var button = image.gameObject.AddComponent<Button>();
            image.gameObject.AddComponent<UiButtonSound>();
            button.targetGraphic = hover;
            var colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 0.74f, 0.38f, 0.72f);
            colors.pressedColor = new Color(1f, 0.43f, 0.08f, 0.95f);
            colors.selectedColor = new Color(1f, 0.64f, 0.24f, 0.42f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.2f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            var iconPlate = Image("IconPlate", image.transform, Sprite("item"), new Color(0.12f, 0.12f, 0.12f, 0.92f));
            SetRect(iconPlate.rectTransform, new Vector2(0f, 0.5f), new Vector2(45f, 0f), new Vector2(58f, 58f));
            iconPlate.raycastTarget = false;
            var iconFrame = Image("IconFrame", iconPlate.transform, Sprite("item_frame"), new Color(0.92f, 0.68f, 0.34f, 0.85f));
            Stretch(iconFrame.rectTransform);
            iconFrame.raycastTarget = false;
            var icon = Image("Icon", iconPlate.transform, Sprite(iconName), Ivory);
            SetRect(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
            if (label == "BACK")
                icon.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
            icon.raycastTarget = false;

            Text("Label", image.transform, label, 23f, Ivory, FontStyles.Bold, TextAlignmentOptions.Left,
                new Vector2(35f, 0f), new Vector2(230f, 54f));
            var arrow = Image("Arrow", image.transform, Sprite("arrow1", "arrow2"), new Color(0.94f, 0.70f, 0.35f, 0.9f));
            SetRect(arrow.rectTransform, new Vector2(1f, 0.5f), new Vector2(-28f, 0f), new Vector2(20f, 20f));
            arrow.raycastTarget = false;
            return button;
        }

        private static Image Panel(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var panel = Image(name, parent, Sprite("field1", "bg3"), new Color(0.12f, 0.13f, 0.13f, 0.96f));
            SetRect(panel.rectTransform, new Vector2(0.5f, 0.5f), position, size);
            SetImageType(panel);
            panel.raycastTarget = false;
            var frame = Image("Frame", panel.transform, Sprite("button_frame", "frame1"), new Color(0.52f, 0.49f, 0.43f, 0.85f));
            Stretch(frame.rectTransform, 2f);
            SetImageType(frame);
            frame.raycastTarget = false;
            return panel;
        }

        private static (Slider slider, TMP_Text value) VolumePanel(string name, Transform parent,
            string label, string initialValue, Vector2 position)
        {
            var panel = Panel(name, parent, position, new Vector2(380f, 70f));
            Text("Label", panel.transform, label, 14f, Ivory, FontStyles.Bold, TextAlignmentOptions.Left,
                new Vector2(-58f, 18f), new Vector2(230f, 28f));
            var value = Text("Value", panel.transform, initialValue, 14f, Gold, FontStyles.Bold,
                TextAlignmentOptions.Right, new Vector2(145f, 18f), new Vector2(68f, 28f));
            var slider = Slider(panel.transform, new Vector2(0f, -18f));
            return (slider, value);
        }

        private static Slider Slider(Transform parent, Vector2 position)
        {
            var root = NewUi("MasterVolumeSlider", parent);
            SetRect((RectTransform)root.transform, new Vector2(0.5f, 0.5f), position, new Vector2(334f, 34f));
            var background = Image("Background", root.transform, Sprite("field1", "bar1"), new Color(0.1f, 0.11f, 0.11f, 1f));
            Stretch(background.rectTransform, 0f, 9f);
            SetImageType(background);
            var fillArea = NewUi("Fill Area", root.transform);
            Stretch((RectTransform)fillArea.transform, 12f, 12f);
            var fill = Image("Fill", fillArea.transform, Sprite("bar1", "line2"), Gold);
            Stretch(fill.rectTransform);
            SetImageType(fill);
            var handleArea = NewUi("Handle Slide Area", root.transform);
            Stretch((RectTransform)handleArea.transform, 15f, 0f);
            var handle = Image("Handle", handleArea.transform, Sprite("circle_item", "item"), Gold);
            SetRect(handle.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
            var handleFrame = Image("Frame", handle.transform, Sprite("item_frame"), Ivory);
            Stretch(handleFrame.rectTransform);
            handleFrame.raycastTarget = false;
            var slider = root.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        private static Toggle Toggle(Transform parent, Vector2 position)
        {
            var root = NewUi("FullscreenToggle", parent);
            SetRect((RectTransform)root.transform, new Vector2(0.5f, 0.5f), position, new Vector2(380f, 68f));
            var background = Image("Panel", root.transform, Sprite("button"), new Color(0.27f, 0.27f, 0.25f, 1f));
            Stretch(background.rectTransform);
            SetImageType(background);
            var frame = Image("Frame", root.transform, Sprite("button_frame"), new Color(0.66f, 0.61f, 0.53f, 0.9f));
            Stretch(frame.rectTransform, 2f);
            SetImageType(frame);
            frame.raycastTarget = false;
            var box = Image("Background", root.transform, Sprite("item"), new Color(0.1f, 0.11f, 0.11f, 1f));
            SetRect(box.rectTransform, new Vector2(1f, 0.5f), new Vector2(-39f, 0f), new Vector2(46f, 46f));
            SetImageType(box);
            var boxFrame = Image("Frame", box.transform, Sprite("item_frame"), new Color(0.92f, 0.68f, 0.34f, 0.85f));
            Stretch(boxFrame.rectTransform);
            boxFrame.raycastTarget = false;
            var check = Image("Checkmark", box.transform, Sprite("star", "button_glow"), Gold);
            Stretch(check.rectTransform, 10f);
            check.raycastTarget = false;
            Text("Label", root.transform, "FULLSCREEN", 18f, Ivory, FontStyles.Bold, TextAlignmentOptions.Left,
                new Vector2(164f, 0f), new Vector2(285f, 44f), new Vector2(0f, 0.5f));
            var toggle = root.AddComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = check;
            toggle.isOn = Screen.fullScreen;
            return toggle;
        }

        private static TMP_Text Text(string name, Transform parent, string content, float size, Color color,
            FontStyles style, TextAlignmentOptions alignment, Vector2 position, Vector2 rect,
            Vector2? anchor = null)
        {
            var go = NewUi(name, parent);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = alignment;
            text.raycastTarget = false;
            SetRect(text.rectTransform, anchor ?? new Vector2(0.5f, 0.5f), position, rect);
            return text;
        }

        private static Image Image(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = NewUi(name, parent);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        private static GameObject NewUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
                go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, float inset = 0f, float verticalInset = -1f)
        {
            if (verticalInset < 0f) verticalInset = inset;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, verticalInset);
            rect.offsetMax = new Vector2(-inset, -verticalInset);
        }

        private static void SetImageType(Image image)
        {
            image.type = image.sprite != null && image.sprite.border.sqrMagnitude > 0f
                ? UnityEngine.UI.Image.Type.Sliced
                : UnityEngine.UI.Image.Type.Simple;
        }

        private static Sprite Sprite(params string[] names)
        {
            foreach (var name in names)
                if (_sprites.TryGetValue(name, out var sprite))
                    return sprite;
            return null;
        }

        private static void LoadSprites()
        {
            _sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>()
                .GroupBy(x => x.name).ToDictionary(x => x.Key, x => x.First());
            if (_sprites.Count == 0)
                throw new InvalidOperationException($"Soft Kitty atlas is unavailable: {AtlasPath}");
        }

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }
    }
}
#endif
