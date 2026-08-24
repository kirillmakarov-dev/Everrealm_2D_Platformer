#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using LetterHunter.Characters;
using LetterHunter.Classes;
using LetterHunter.Skills;
using LetterHunter.UI.Hud;
using LetterHunter.UI.Skills;
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
    public static class SkillBarSetupBuilder
    {
        private const string SlotPrefabPath = "Assets/_Game/Prefabs/UI/SkillSlot.prefab";
        private const string IconFolder = "Assets/_Game/Data/UI/SkillIcons";
        private const string HudSpriteFolder = "Assets/_Game/Data/UI/Hud";
        private static readonly string[] DefaultLabels = { "U", "I", "O", "P" };

        [MenuItem("Letter Hunter/Setup Skill Bar UI")]
        public static void SetupCurrentScene()
        {
            SetupScene(SceneManager.GetActiveScene());
        }

        public static SkillBarPresenter SetupScene(Scene scene)
        {
            EnsureFolder("Assets/_Game/Prefabs/UI");
            EnsureFolder(IconFolder);

            var icons = EnsureSkillIcons();
            AssignIconsToSkillAssets(icons);
            var prefab = EnsureSlotPrefab();
            SetupPlayers(scene);
            var canvas = EnsureCanvas(scene, prefab);
            EnsureVitalsHud(canvas.GetComponentInParent<Canvas>(), scene);

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Skill Bar UI setup complete. Press Play and use U/I/O/P or click the skill slots.", canvas);
            return canvas;
        }

        public static SkillSlotView EnsureSlotPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SkillSlotView>(SlotPrefabPath);
            if (existing != null) return existing;

            var root = CreateUIObject("SkillSlot", new Vector2(76f, 76f));
            var background = root.AddComponent<Image>();
            background.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);
            var button = root.AddComponent<Button>();
            button.targetGraphic = background;

            var icon = CreateChildImage(root.transform, "Icon", new Vector2(56f, 56f), Vector2.zero);
            icon.color = Color.white;
            icon.preserveAspect = true;

            var overlay = CreateChildImage(root.transform, "CooldownOverlay", new Vector2(56f, 56f), Vector2.zero);
            overlay.color = new Color(0f, 0f, 0f, 0.68f);
            overlay.type = Image.Type.Filled;
            overlay.fillMethod = Image.FillMethod.Radial360;
            overlay.fillOrigin = 2;
            overlay.fillClockwise = false;
            overlay.fillAmount = 0f;

            var cooldown = CreateChildText(root.transform, "CooldownText", new Vector2(56f, 32f), Vector2.zero, "0", 26f);
            cooldown.alignment = TextAlignmentOptions.Center;
            cooldown.color = Color.white;
            cooldown.fontStyle = FontStyles.Bold;

            var key = CreateChildText(root.transform, "KeyText", new Vector2(24f, 18f), new Vector2(-24f, -25f), "U", 13f);
            key.alignment = TextAlignmentOptions.Center;
            key.color = new Color(1f, 0.92f, 0.55f);
            key.fontStyle = FontStyles.Bold;

            var title = CreateChildText(root.transform, "NameText", new Vector2(70f, 18f), new Vector2(0f, 28f), "Skill", 11f);
            title.alignment = TextAlignmentOptions.Center;
            title.color = Color.white;
            title.enableAutoSizing = true;
            title.fontSizeMin = 7f;
            title.fontSizeMax = 11f;

            var view = root.AddComponent<SkillSlotView>();
            var so = new SerializedObject(view);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("cooldownOverlay").objectReferenceValue = overlay;
            so.FindProperty("cooldownText").objectReferenceValue = cooldown;
            so.FindProperty("keyText").objectReferenceValue = key;
            so.FindProperty("nameText").objectReferenceValue = title;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, SlotPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<SkillSlotView>();
        }

        private static SkillBarPresenter EnsureCanvas(Scene scene, SkillSlotView slotPrefab)
        {
            var presenter = FindPresenterInScene(scene);
            if (presenter != null)
            {
                AssignPresenterPrefab(presenter, slotPrefab);
                return presenter;
            }

            var canvasObject = new GameObject("SkillBarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (scene.IsValid())
                SceneManager.MoveGameObjectToScene(canvasObject, scene);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var bar = CreateUIObject("SkillBar", new Vector2(360f, 92f));
            bar.transform.SetParent(canvasObject.transform, false);
            var barRect = (RectTransform)bar.transform;
            barRect.anchorMin = new Vector2(0.5f, 0f);
            barRect.anchorMax = new Vector2(0.5f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 24f);

            var layout = bar.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 10f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var presenterComponent = bar.AddComponent<SkillBarPresenter>();
            AssignPresenterPrefab(presenterComponent, slotPrefab);
            EnsureEventSystem(scene);
            return presenterComponent;
        }

        private static PlayerVitalsHudPresenter EnsureVitalsHud(Canvas canvas, Scene scene)
        {
            if (canvas == null)
                return null;

            var existing = canvas.GetComponentInChildren<PlayerVitalsHudPresenter>(true);
            if (existing != null)
            {
                AssignVitalsHud(existing, FindPlayerInScene(scene));
                return existing;
            }

            EnsureFolder(HudSpriteFolder);
            var orbSprite = EnsureOrbSprite("Orb_Mask", Color.white, Color.white);
            var rimSprite = EnsureOrbRimSprite("Orb_Rim");
            var healthSprite = EnsureOrbSprite("Orb_Health", new Color(0.95f, 0.04f, 0.02f), new Color(0.28f, 0.01f, 0.02f));
            var manaSprite = EnsureOrbSprite("Orb_Mana", new Color(0.1f, 0.5f, 1f), new Color(0.01f, 0.05f, 0.35f));

            var root = CreateUIObject("VitalsHud", new Vector2(620f, 176f));
            root.transform.SetParent(canvas.transform, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 18f);

            var health = CreateOrb(root.transform, "HealthOrb", new Vector2(-250f, 42f), "HP",
                new Color(0.18f, 0.01f, 0.01f, 0.95f), healthSprite, orbSprite, rimSprite);
            var mana = CreateOrb(root.transform, "ManaOrb", new Vector2(250f, 42f), "MP",
                new Color(0.01f, 0.03f, 0.16f, 0.95f), manaSprite, orbSprite, rimSprite);

            var presenter = root.AddComponent<PlayerVitalsHudPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("player").objectReferenceValue = FindPlayerInScene(scene);
            so.FindProperty("healthOrb").objectReferenceValue = health;
            so.FindProperty("manaOrb").objectReferenceValue = mana;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(presenter);
            return presenter;
        }

        private static void AssignVitalsHud(PlayerVitalsHudPresenter presenter, PlayerClassController player)
        {
            var so = new SerializedObject(presenter);
            so.FindProperty("player").objectReferenceValue = player;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }

        private static VitalsOrbView CreateOrb(Transform parent, string name, Vector2 position, string label,
            Color backgroundColor, Sprite fillSprite, Sprite orbSprite, Sprite rimSprite)
        {
            var root = CreateUIObject(name, new Vector2(156f, 156f));
            root.transform.SetParent(parent, false);
            ((RectTransform)root.transform).anchoredPosition = position;

            var back = CreateChildImage(root.transform, "OrbBack", new Vector2(142f, 142f), Vector2.zero);
            back.sprite = orbSprite;
            back.color = backgroundColor;
            back.preserveAspect = true;

            var fill = CreateChildImage(root.transform, "OrbFill", new Vector2(132f, 132f), Vector2.zero);
            fill.sprite = fillSprite;
            fill.color = Color.white;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Vertical;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            fill.preserveAspect = true;

            var shine = CreateChildImage(root.transform, "OrbShine", new Vector2(122f, 122f), new Vector2(-8f, 14f));
            shine.sprite = orbSprite;
            shine.color = new Color(1f, 1f, 1f, 0.12f);
            shine.preserveAspect = true;

            var rim = CreateChildImage(root.transform, "OrbRim", new Vector2(156f, 156f), Vector2.zero);
            rim.sprite = rimSprite;
            rim.color = Color.white;
            rim.preserveAspect = true;

            var value = CreateChildText(root.transform, "ValueText", new Vector2(112f, 34f), new Vector2(0f, -4f), "0/0", 18f);
            value.alignment = TextAlignmentOptions.Center;
            value.color = Color.white;
            value.fontStyle = FontStyles.Bold;

            var labelText = CreateChildText(root.transform, "LabelText", new Vector2(64f, 22f), new Vector2(0f, -54f), label, 14f);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(1f, 0.86f, 0.45f);
            labelText.fontStyle = FontStyles.Bold;

            var view = root.AddComponent<VitalsOrbView>();
            var so = new SerializedObject(view);
            so.FindProperty("fill").objectReferenceValue = fill;
            so.FindProperty("valueText").objectReferenceValue = value;
            so.FindProperty("labelText").objectReferenceValue = labelText;
            so.FindProperty("label").stringValue = label;
            so.FindProperty("showNumbers").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static void AssignPresenterPrefab(SkillBarPresenter presenter, SkillSlotView prefab)
        {
            var player = FindPlayerInScene(presenter.gameObject.scene);
            var loadout = player != null ? player.GetComponent<PlayerSkillLoadout>() : null;

            var so = new SerializedObject(presenter);
            so.FindProperty("player").objectReferenceValue = player;
            so.FindProperty("loadout").objectReferenceValue = loadout;
            so.FindProperty("slotPrefab").objectReferenceValue = prefab;
            so.FindProperty("slotRoot").objectReferenceValue = presenter.transform;
            so.FindProperty("maxSlots").intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }

        private static void SetupPlayers(Scene scene)
        {
            foreach (var player in FindPlayersInScene(scene))
            {
                var loadout = player.GetComponent<PlayerSkillLoadout>();
                if (loadout == null)
                    loadout = Undo.AddComponent<PlayerSkillLoadout>(player.gameObject);

                var bindings = BuildBindingsFromClassDefinition(player);
                loadout.SetSlots(bindings);
                EditorUtility.SetDirty(loadout);
            }
        }

        private static List<SkillSlotBinding> BuildBindingsFromClassDefinition(PlayerClassController player)
        {
            var result = new List<SkillSlotBinding>();
            var playerSo = new SerializedObject(player);
            var classDefinition = playerSo.FindProperty("classDefinition")?.objectReferenceValue as ClassDefinition;
            if (classDefinition == null) return result;

            var slotIndex = 0;
            foreach (var skill in classDefinition.StartingSkills)
            {
                if (skill == null) continue;
                if (skill.SkillType is LetterHunter.Core.SkillType.Passive or LetterHunter.Core.SkillType.AutoAttackUpgrade)
                    continue;
                if (slotIndex >= DefaultLabels.Length) break;

                result.Add(new SkillSlotBinding(slotIndex, skill, DefaultLabels[slotIndex]));
                slotIndex++;
            }

            return result;
        }

        private static Dictionary<string, Sprite> EnsureSkillIcons()
        {
            var icons = new Dictionary<string, Sprite>
            {
                ["fire"] = EnsureIcon("Icon_Fire", new Color(1f, 0.28f, 0.08f), new Color(1f, 0.9f, 0.18f), "F"),
                ["iron"] = EnsureIcon("Icon_Iron", new Color(0.58f, 0.63f, 0.72f), new Color(0.95f, 0.98f, 1f), "I"),
                ["focus"] = EnsureIcon("Icon_Focus", new Color(0.35f, 0.25f, 1f), new Color(0.8f, 0.9f, 1f), "O"),
                ["star"] = EnsureIcon("Icon_Star", new Color(0.12f, 0.8f, 1f), new Color(0.95f, 1f, 1f), "S"),
                ["speed"] = EnsureIcon("Icon_Speed", new Color(0.1f, 1f, 0.55f), new Color(0.95f, 1f, 0.55f), "V"),
                ["default"] = EnsureIcon("Icon_DefaultSkill", new Color(0.75f, 0.35f, 1f), new Color(1f, 0.85f, 1f), "*")
            };

            return icons;
        }

        private static Sprite EnsureIcon(string name, Color inner, Color outer, string letter)
        {
            var path = $"{IconFolder}/{name}.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (var y = 0; y < 64; y++)
                {
                    for (var x = 0; x < 64; x++)
                    {
                        var dx = (x - 31.5f) / 31.5f;
                        var dy = (y - 31.5f) / 31.5f;
                        var dist = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                        var color = Color.Lerp(inner, outer, dist);
                        color.a = dist > 0.95f ? 0f : 1f;
                        texture.SetPixel(x, y, color);
                    }
                }

                DrawLetter(texture, letter, Color.black);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite EnsureOrbSprite(string name, Color top, Color bottom)
        {
            var path = $"{HudSpriteFolder}/{name}.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                for (var y = 0; y < 128; y++)
                {
                    for (var x = 0; x < 128; x++)
                    {
                        var dx = (x - 63.5f) / 63.5f;
                        var dy = (y - 63.5f) / 63.5f;
                        var distance = Mathf.Sqrt(dx * dx + dy * dy);
                        var alpha = distance <= 1f ? 1f : 0f;
                        var vertical = Mathf.Clamp01((y / 127f) + 0.12f * (1f - distance));
                        var color = Color.Lerp(bottom, top, vertical);
                        var rim = Mathf.InverseLerp(0.72f, 1f, distance);
                        color = Color.Lerp(color, Color.black, rim * 0.28f);
                        color.a = alpha;
                        texture.SetPixel(x, y, color);
                    }
                }

                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite EnsureOrbRimSprite(string name)
        {
            var path = $"{HudSpriteFolder}/{name}.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                var gold = new Color(0.94f, 0.72f, 0.32f, 1f);
                var darkGold = new Color(0.25f, 0.14f, 0.04f, 1f);

                for (var y = 0; y < 128; y++)
                {
                    for (var x = 0; x < 128; x++)
                    {
                        var dx = (x - 63.5f) / 63.5f;
                        var dy = (y - 63.5f) / 63.5f;
                        var distance = Mathf.Sqrt(dx * dx + dy * dy);
                        var outer = Mathf.SmoothStep(1f, 0.92f, distance);
                        var inner = Mathf.SmoothStep(0.76f, 0.84f, distance);
                        var alpha = Mathf.Clamp01(outer * inner);
                        var highlight = Mathf.Clamp01((dy + 1f) * 0.5f);
                        var color = Color.Lerp(darkGold, gold, highlight);
                        color.a = alpha;
                        texture.SetPixel(x, y, color);
                    }
                }

                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void AssignIconsToSkillAssets(IReadOnlyDictionary<string, Sprite> icons)
        {
            var guids = AssetDatabase.FindAssets("t:SkillDefinition", new[] { "Assets/_Game/Data/Skills" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
                if (skill == null) continue;

                var icon = SelectIcon(skill, icons);
                var so = new SerializedObject(skill);
                var iconProperty = so.FindProperty("icon");
                var shortNameProperty = so.FindProperty("shortName");
                var inputLabelProperty = so.FindProperty("inputLabel");

                if (iconProperty != null && iconProperty.objectReferenceValue == null)
                    iconProperty.objectReferenceValue = icon;
                if (shortNameProperty != null && string.IsNullOrWhiteSpace(shortNameProperty.stringValue))
                    shortNameProperty.stringValue = CreateShortName(skill);
                if (inputLabelProperty != null && string.IsNullOrWhiteSpace(inputLabelProperty.stringValue))
                    inputLabelProperty.stringValue = string.Empty;

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(skill);
            }
        }

        private static Sprite SelectIcon(SkillDefinition skill, IReadOnlyDictionary<string, Sprite> icons)
        {
            var key = $"{skill.SkillId} {skill.DisplayName} {skill.name}".ToLowerInvariant();
            if (key.Contains("fire") && icons.TryGetValue("fire", out var fire)) return fire;
            if ((key.Contains("iron") || key.Contains("strength")) && icons.TryGetValue("iron", out var iron)) return iron;
            if (key.Contains("focus") && icons.TryGetValue("focus", out var focus)) return focus;
            if ((key.Contains("star") || key.Contains("throw")) && icons.TryGetValue("star", out var star)) return star;
            if (key.Contains("speed") && icons.TryGetValue("speed", out var speed)) return speed;
            return icons.TryGetValue("default", out var fallback) ? fallback : null;
        }

        private static string CreateShortName(SkillDefinition skill)
        {
            var name = string.IsNullOrWhiteSpace(skill.DisplayName) ? skill.name : skill.DisplayName;
            return name.Length <= 10 ? name : name[..10];
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            if (scene.IsValid())
                SceneManager.MoveGameObjectToScene(go, scene);

#if ENABLE_INPUT_SYSTEM
            go.AddComponent<InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
        }

        private static SkillBarPresenter FindPresenterInScene(Scene scene)
        {
            if (!scene.IsValid()) return Object.FindFirstObjectByType<SkillBarPresenter>();

            foreach (var root in scene.GetRootGameObjects())
            {
                var presenter = root.GetComponentInChildren<SkillBarPresenter>(true);
                if (presenter != null) return presenter;
            }

            return null;
        }

        private static PlayerClassController FindPlayerInScene(Scene scene)
        {
            foreach (var player in FindPlayersInScene(scene))
                return player;

            return null;
        }

        private static IEnumerable<PlayerClassController> FindPlayersInScene(Scene scene)
        {
            if (!scene.IsValid())
                return Object.FindObjectsByType<PlayerClassController>(FindObjectsSortMode.None);

            var players = new List<PlayerClassController>();
            foreach (var root in scene.GetRootGameObjects())
                players.AddRange(root.GetComponentsInChildren<PlayerClassController>(true));

            return players;
        }

        private static GameObject CreateUIObject(string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }

        private static Image CreateChildImage(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var go = CreateUIObject(name, size);
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).anchoredPosition = position;
            return go.AddComponent<Image>();
        }

        private static TMP_Text CreateChildText(Transform parent, string name, Vector2 size, Vector2 position, string text, float fontSize)
        {
            var go = CreateUIObject(name, size);
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).anchoredPosition = position;
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.raycastTarget = false;
            return label;
        }

        private static void DrawLetter(Texture2D texture, string letter, Color color)
        {
            // Tiny block-glyph marker. It is intentionally simple; real icons can replace these sprites later.
            var hash = string.IsNullOrEmpty(letter) ? 0 : letter[0];
            for (var y = 22; y < 43; y++)
            {
                for (var x = 24; x < 41; x++)
                {
                    var draw = ((x + y + hash) % 7 == 0) || x is 24 or 40 || y is 22 or 42;
                    if (!draw) continue;
                    var pixel = color;
                    pixel.a = 0.45f;
                    texture.SetPixel(x, y, pixel);
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
#endif
