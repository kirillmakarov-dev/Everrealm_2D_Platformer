#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LetterHunter.Characters;
using LetterHunter.Debugging;
using LetterHunter.SkillTree;
using LetterHunter.Skills;
using LetterHunter.UI.SkillTree;
using LetterHunter.UI.Skills;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LetterHunter.EditorTools
{
    public static class SkillTreeSetupBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/InventoryLootDebug.unity";
        private const string DataFolder = "Assets/_Game/Data/Professions/Warrior";
        private const string PrefabFolder = "Assets/_Game/Prefabs/UI/SkillTree";
        private const string ProfessionPath = DataFolder + "/WarriorProfession.asset";
        private const string NodePrefabPath = PrefabFolder + "/SkillTreeNode.prefab";
        private const string ConnectionPrefabPath = PrefabFolder + "/SkillTreeConnection.prefab";
        private const string WindowPrefabPath = PrefabFolder + "/SkillTreeWindow.prefab";
        private const string ResourcesNodePrefabPath = "Assets/_Game/Resources/EnglishKingdomSkillTree/SkillTreeNode.prefab";
        private const string ResourcesConnectionPrefabPath = "Assets/_Game/Resources/EnglishKingdomSkillTree/SkillTreeConnection.prefab";
        private const string AtlasPath = "Assets/SoftKitty/InventoryEngine/Textures/Sprites/Main.png";
        private const string IconFolder = "Assets/SoftKitty/InventoryEngine/Textures/SkillIcon";

        private static Dictionary<string, Sprite> _atlas;

        private readonly struct NodeSpec
        {
            public NodeSpec(string id, string title, string description, int icon, int level, int price,
                Vector2 position, string[] parents, string abilityPath = null)
            {
                Id = id; Title = title; Description = description; Icon = icon; Level = level; Price = price;
                Position = position; Parents = parents; AbilityPath = abilityPath;
            }
            public string Id { get; }
            public string Title { get; }
            public string Description { get; }
            public int Icon { get; }
            public int Level { get; }
            public int Price { get; }
            public Vector2 Position { get; }
            public string[] Parents { get; }
            public string AbilityPath { get; }
        }

        private static readonly NodeSpec[] Specs =
        {
            new("training_roots", "Warrior Foundation", "Master the discipline required to enter the warrior profession.",
                7, 1, 0, new Vector2(0f, 185f), Array.Empty<string>(),
                "Assets/_Game/Data/Skills/Warrior/SpecialTraining.asset"),
            new("focus_slash", "Focus Slash", "Unlock a precise two-hit slash through the existing combat skill system.",
                1, 1, 15, new Vector2(280f, 55f), new[] { "training_roots" },
                "Assets/_Game/Data/Skills/SkillTree/TreeFocusSlash.asset"),
            new("defense_mastery", "Fortress Training", "Advance the defensive branch and prepare for Battle Mastery.",
                10, 2, 18, new Vector2(280f, 245f), new[] { "training_roots" },
                "Assets/_Game/Data/Skills/Warrior/IronStrength.asset"),
            new("rapid_assault", "Rapid Assault", "Advance the speed branch and prepare for Battle Mastery.",
                8, 2, 18, new Vector2(280f, 435f), new[] { "training_roots" },
                "Assets/_Game/Data/Skills/Warrior/FireSpin.asset"),
            new("focused_flow", "Focused Flow", "Deepen your control of Focus Slash and unlock the final tier.",
                9, 2, 20, new Vector2(570f, 55f), new[] { "focus_slash" },
                "Assets/_Game/Data/Skills/Warrior/ExtremeFocus.asset"),
            new("battle_mastery", "Battle Mastery", "Capstone training available after all three branches are completed.",
                12, 4, 35, new Vector2(860f, 245f), new[] { "focused_flow", "defense_mastery", "rapid_assault" },
                "Assets/_Game/Data/Skills/Warrior/ComboMaster.asset")
        };

        [MenuItem("Everrealm/Build Profession Skill Tree", priority = 2)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                Debug.LogError($"Open {ScenePath} before building the profession Skill Tree.");
                return;
            }
            EnsureFolder(DataFolder);
            EnsureFolder(PrefabFolder);
            LoadAtlas();
            RegisterSoftKittySettings();
            var profession = BuildData();
            // Keep the imported English Kingdom visual prefab intact. This tool owns
            // data and scene wiring; it must not recreate the old Everrealm UI.
            var windowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPrefabPath);
            if (windowPrefab == null)
                throw new InvalidOperationException($"Missing visual prefab at {WindowPrefabPath}.");
            AssignImportedVisualReferences();
            windowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPrefabPath);
            ValidateImportedVisualAssets(windowPrefab);
            IntegrateScene(scene, profession, windowPrefab);
            ValidateSkillLoadout(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Profession Skill Tree data, prefabs and scene integration built successfully.");
        }

        [MenuItem("Everrealm/Validate Profession Skill Tree", priority = 3)]
        public static void Validate()
        {
            var windowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPrefabPath);
            if (windowPrefab == null)
            {
                Debug.LogError($"Missing visual prefab at {WindowPrefabPath}.");
                return;
            }

            AssignImportedVisualReferences();
            windowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPrefabPath);
            ValidateImportedVisualAssets(windowPrefab);
            ValidateSkillLoadout(SceneManager.GetActiveScene());
            Debug.Log("Profession Skill Tree visual and skill-slot assignments validated.");
        }

        private static void ValidateImportedVisualAssets(GameObject windowPrefab)
        {
            var visual = windowPrefab.GetComponent<EverrealmSkillTreeVisual>();
            if (visual == null)
                throw new InvalidOperationException("SkillTreeWindow.prefab has no EverrealmSkillTreeVisual component.");

            var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesNodePrefabPath);
            var connectionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesConnectionPrefabPath);
            if (nodePrefab == null || nodePrefab.GetComponent<EverrealmSkillTreeNodeVisual>() == null)
                throw new InvalidOperationException($"Invalid Resources node prefab: {ResourcesNodePrefabPath}.");
            if (connectionPrefab == null || connectionPrefab.GetComponent<EverrealmSkillTreeConnectionVisual>() == null)
                throw new InvalidOperationException($"Invalid Resources connection prefab: {ResourcesConnectionPrefabPath}.");
        }

        private static void AssignImportedVisualReferences()
        {
            var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesNodePrefabPath);
            var connectionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResourcesConnectionPrefabPath);
            if (nodePrefab == null || nodePrefab.GetComponent<EverrealmSkillTreeNodeVisual>() == null ||
                connectionPrefab == null || connectionPrefab.GetComponent<EverrealmSkillTreeConnectionVisual>() == null)
                throw new InvalidOperationException("Everrealm skill-tree Resources prefabs are missing or invalid.");

            var contents = PrefabUtility.LoadPrefabContents(WindowPrefabPath);
            try
            {
                var visual = contents.GetComponent<EverrealmSkillTreeVisual>();
                if (visual == null)
                    throw new InvalidOperationException("SkillTreeWindow.prefab has no EverrealmSkillTreeVisual component.");

                var group = contents.GetComponent<CanvasGroup>() ?? contents.AddComponent<CanvasGroup>();
                var so = new SerializedObject(visual);
                so.FindProperty("_nodePrefab").objectReferenceValue = nodePrefab;
                so.FindProperty("_connectionPrefab").objectReferenceValue = connectionPrefab;
                so.FindProperty("_windowGroup").objectReferenceValue = group;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, WindowPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void ValidateSkillLoadout(Scene scene)
        {
            if (!scene.IsValid() || scene.path != ScenePath) return;
            var loadout = UnityEngine.Object.FindFirstObjectByType<PlayerSkillLoadout>(FindObjectsInactive.Include);
            if (loadout == null)
            {
                Debug.LogWarning("PlayerSkillLoadout was not found; skill slot validation skipped.");
                return;
            }

            var seen = new HashSet<int>();
            foreach (var binding in loadout.Slots)
            {
                if (binding == null || binding.Skill == null)
                    throw new InvalidOperationException("Skill loadout contains an empty binding.");
                if (!seen.Add(binding.SlotIndex))
                    throw new InvalidOperationException($"Skill loadout contains duplicate slot {binding.SlotIndex + 1}.");
            }

            var third = loadout.ResolveSkill(loadout.GetComponent<PlayerClassController>(), 2, out var label);
            if (third == null)
                throw new InvalidOperationException("Skill 3 has no assigned SkillDefinition.");
            if (label != "3")
                Debug.LogWarning($"Skill 3 is assigned to '{third.DisplayName}' but its input label is '{label}'.");
            else
                Debug.Log($"Skill 3 assignment OK: {third.DisplayName} ({third.SkillId}).");
        }

        public static void BuildFromBatchMode()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Build();
        }

        private static ProfessionDefinitionSO BuildData()
        {
            var nodesById = new Dictionary<string, SkillNodeDefinitionSO>();
            foreach (var spec in Specs)
            {
                var path = $"{DataFolder}/{spec.Id}.asset";
                var node = LoadOrCreate<SkillNodeDefinitionSO>(path);
                nodesById[spec.Id] = node;
                PrepareIcon(spec.Icon);
                var so = new SerializedObject(node);
                so.FindProperty("nodeId").stringValue = spec.Id;
                so.FindProperty("displayName").stringValue = spec.Title;
                so.FindProperty("description").stringValue = spec.Description;
                so.FindProperty("icon").objectReferenceValue = LoadIcon(spec.Icon);
                var ability = string.IsNullOrWhiteSpace(spec.AbilityPath)
                    ? null : AssetDatabase.LoadAssetAtPath<SkillDefinition>(spec.AbilityPath);
                if (!string.IsNullOrWhiteSpace(spec.AbilityPath) && ability == null)
                    throw new InvalidOperationException($"Skill asset is missing for node {spec.Id}: {spec.AbilityPath}.");
                so.FindProperty("abilityToGrant").objectReferenceValue = ability;
                so.FindProperty("requiredLevel").intValue = spec.Level;
                so.FindProperty("price").intValue = spec.Price;
                so.FindProperty("uiPosition").vector2Value = spec.Position;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(node);
            }

            foreach (var spec in Specs)
            {
                var so = new SerializedObject(nodesById[spec.Id]);
                var parents = so.FindProperty("parentNodes");
                parents.arraySize = spec.Parents.Length;
                for (var i = 0; i < spec.Parents.Length; i++)
                    parents.GetArrayElementAtIndex(i).objectReferenceValue = nodesById[spec.Parents[i]];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var profession = LoadOrCreate<ProfessionDefinitionSO>(ProfessionPath);
            var professionSo = new SerializedObject(profession);
            professionSo.FindProperty("professionId").stringValue = "warrior";
            professionSo.FindProperty("displayName").stringValue = "Warrior";
            professionSo.FindProperty("description").stringValue =
                "A disciplined melee profession built around decisive attacks, defense and battlefield control.";
            professionSo.FindProperty("icon").objectReferenceValue = LoadIcon(7);
            var nodes = professionSo.FindProperty("skillNodes");
            nodes.arraySize = Specs.Length;
            for (var i = 0; i < Specs.Length; i++)
                nodes.GetArrayElementAtIndex(i).objectReferenceValue = nodesById[Specs[i].Id];
            professionSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profession);
            return profession;
        }

        private static SkillTreeNodeView BuildNodePrefab()
        {
            var root = RectObject("SkillTreeNode", null, new Vector2(190f, 150f), Vector2.zero);
            var background = root.AddComponent<Image>();
            SetImage(background, "item", new Color(0.16f, 0.2f, 0.22f, 1f), true);
            var button = root.AddComponent<Button>();
            button.targetGraphic = background;

            var frame = ImageChild(root.transform, "Frame", new Vector2(190f, 150f), Vector2.zero,
                "item_frame", Color.white, true);
            var glow = ImageChild(root.transform, "PurchasedGlow", new Vector2(196f, 156f), Vector2.zero,
                "item_frame", new Color(1f, 0.58f, 0.12f, 0.9f), true);
            var selected = ImageChild(root.transform, "SelectedFrame", new Vector2(202f, 162f), Vector2.zero,
                "item_frame", new Color(0.3f, 0.8f, 1f, 1f), true);
            var iconBack = ImageChild(root.transform, "IconBack", new Vector2(82f, 82f), new Vector2(0f, 17f),
                "item", new Color(0.08f, 0.09f, 0.1f, 1f), true);
            var icon = ImageChild(iconBack.transform, "Icon", new Vector2(70f, 70f), Vector2.zero,
                null, Color.white, false);
            icon.preserveAspect = true;
            var locked = ImageChild(root.transform, "LockedOverlay", new Vector2(184f, 144f), Vector2.zero,
                null, new Color(0f, 0f, 0f, 0.5f), false);
            var title = TextChild(root.transform, "Title", new Vector2(170f, 26f), new Vector2(0f, -48f),
                "SKILL", 18f, TextAlignmentOptions.Center);
            var state = TextChild(root.transform, "State", new Vector2(170f, 22f), new Vector2(0f, -70f),
                "LOCKED", 13f, TextAlignmentOptions.Center);
            state.color = new Color(1f, 0.67f, 0.2f, 1f);
            glow.gameObject.SetActive(false);
            selected.gameObject.SetActive(false);

            var view = root.AddComponent<SkillTreeNodeView>();
            var so = new SerializedObject(view);
            so.FindProperty("button").objectReferenceValue = button;
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("frame").objectReferenceValue = frame;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("lockedOverlay").objectReferenceValue = locked.gameObject;
            so.FindProperty("purchasedGlow").objectReferenceValue = glow.gameObject;
            so.FindProperty("selectedFrame").objectReferenceValue = selected.gameObject;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("stateText").objectReferenceValue = state;
            so.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, NodePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<SkillTreeNodeView>();
        }

        private static SkillTreeConnectionView BuildConnectionPrefab()
        {
            var root = RectObject("SkillTreeConnection", null, new Vector2(100f, 18f), Vector2.zero);
            ConfigureTopLeftLayer(root.GetComponent<RectTransform>());
            root.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            var shadow = ImageChild(root.transform, "Shadow", new Vector2(100f, 15f), Vector2.zero,
                "line4", Color.black, true);
            var foreground = ImageChild(root.transform, "Foreground", new Vector2(100f, 9f), Vector2.zero,
                "line4", new Color(0.25f, 0.65f, 0.8f), true);
            var marker = ImageChild(root.transform, "DirectionMarker", new Vector2(14f, 14f), new Vector2(42f, 0f),
                "star", new Color(0.25f, 0.65f, 0.8f), false);
            var view = root.AddComponent<SkillTreeConnectionView>();
            var so = new SerializedObject(view);
            so.FindProperty("lineRoot").objectReferenceValue = root.transform;
            so.FindProperty("shadow").objectReferenceValue = shadow;
            so.FindProperty("foreground").objectReferenceValue = foreground;
            so.FindProperty("directionMarker").objectReferenceValue = marker;
            so.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ConnectionPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab.GetComponent<SkillTreeConnectionView>();
        }

        private static GameObject BuildWindowPrefab(ProfessionDefinitionSO profession, SkillTreeNodeView nodePrefab,
            SkillTreeConnectionView connectionPrefab)
        {
            var root = RectObject("SkillTreeWindow", null, new Vector2(1920f, 1080f), Vector2.zero);
            var overlay = root.AddComponent<Image>();
            overlay.color = new Color(0.015f, 0.02f, 0.025f, 0.76f);
            var group = root.AddComponent<CanvasGroup>();

            var panel = ImageChild(root.transform, "Panel", new Vector2(1320f, 760f), Vector2.zero,
                "bg2", new Color(0.12f, 0.14f, 0.15f, 1f), true);
            AddShadow(panel.gameObject, new Vector2(12f, -12f), new Color(0f, 0f, 0f, 0.78f));
            var header = ImageChild(panel.transform, "Header", new Vector2(1280f, 74f), new Vector2(0f, 323f),
                "bar2", new Color(0.08f, 0.27f, 0.34f, 1f), true);
            var title = TextChild(header.transform, "Title", new Vector2(480f, 50f), new Vector2(-370f, 0f),
                "PROFESSION SKILLS", 31f, TextAlignmentOptions.MidlineLeft);
            var level = TextChild(header.transform, "Level", new Vector2(180f, 40f), new Vector2(275f, 0f),
                "LEVEL  1", 19f, TextAlignmentOptions.Center);
            var coins = TextChild(header.transform, "Coins", new Vector2(190f, 40f), new Vector2(455f, 0f),
                "COINS  0", 19f, TextAlignmentOptions.Center);
            var close = ButtonChild(header.transform, "CloseButton", new Vector2(56f, 48f), new Vector2(600f, 0f),
                "X", 24f);

            var left = ImageChild(panel.transform, "ProfessionPanel", new Vector2(245f, 620f), new Vector2(-510f, -38f),
                "bg2", new Color(0.08f, 0.1f, 0.11f, 0.98f), true);
            var professionIcon = ImageChild(left.transform, "ProfessionIcon", new Vector2(112f, 112f), new Vector2(0f, 190f),
                "item", Color.white, true);
            professionIcon.preserveAspect = true;
            var professionName = TextChild(left.transform, "ProfessionName", new Vector2(210f, 44f), new Vector2(0f, 105f),
                "WARRIOR", 27f, TextAlignmentOptions.Center);
            var professionDescription = TextChild(left.transform, "ProfessionDescription", new Vector2(205f, 270f),
                new Vector2(0f, -70f), "Profession description", 17f, TextAlignmentOptions.TopLeft);
            professionDescription.textWrappingMode = TextWrappingModes.Normal;

            var center = ImageChild(panel.transform, "TreePanel", new Vector2(710f, 620f), new Vector2(-15f, -38f),
                "bg2", new Color(0.065f, 0.075f, 0.08f, 0.98f), true);
            var viewport = ImageChild(center.transform, "Viewport", new Vector2(646f, 540f), new Vector2(-8f, 18f),
                null, new Color(0f, 0f, 0f, 0.16f), false);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = RectObject("Content", viewport.transform, new Vector2(1200f, 540f), Vector2.zero);
            content.GetComponent<RectTransform>().anchorMin = content.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
            content.GetComponent<RectTransform>().pivot = new Vector2(0f, 1f);
            content.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            var connections = RectObject("Connections", content.transform, content.GetComponent<RectTransform>().sizeDelta, Vector2.zero);
            var nodesRoot = RectObject("Nodes", content.transform, content.GetComponent<RectTransform>().sizeDelta, Vector2.zero);
            ConfigureTopLeftLayer(connections.GetComponent<RectTransform>());
            ConfigureTopLeftLayer(nodesRoot.GetComponent<RectTransform>());
            nodesRoot.AddComponent<SkillTreeGraphLayoutGroup>();

            var horizontalScrollbarBack = ImageChild(center.transform, "HorizontalScrollbar", new Vector2(626f, 16f),
                new Vector2(-10f, -286f), "bar2", new Color(0.1f, 0.12f, 0.13f, 1f), true);
            var horizontalSlidingArea = RectObject("SlidingArea", horizontalScrollbarBack.transform,
                new Vector2(602f, 12f), Vector2.zero);
            var horizontalHandle = ImageChild(horizontalSlidingArea.transform, "Handle", new Vector2(180f, 12f),
                Vector2.zero,
                "button", new Color(0.92f, 0.55f, 0.12f, 1f), true);
            var horizontalScrollbar = horizontalScrollbarBack.gameObject.AddComponent<Scrollbar>();
            horizontalScrollbar.handleRect = horizontalHandle.rectTransform;
            horizontalScrollbar.targetGraphic = horizontalHandle;
            horizontalScrollbar.direction = Scrollbar.Direction.LeftToRight;

            var verticalScrollbarBack = ImageChild(center.transform, "VerticalScrollbar", new Vector2(16f, 526f),
                new Vector2(332f, 18f), "bar2", new Color(0.1f, 0.12f, 0.13f, 1f), true);
            var verticalSlidingArea = RectObject("SlidingArea", verticalScrollbarBack.transform,
                new Vector2(12f, 502f), Vector2.zero);
            var verticalHandle = ImageChild(verticalSlidingArea.transform, "Handle", new Vector2(12f, 150f),
                Vector2.zero, "button", new Color(0.92f, 0.55f, 0.12f, 1f), true);
            var verticalScrollbar = verticalScrollbarBack.gameObject.AddComponent<Scrollbar>();
            verticalScrollbar.handleRect = verticalHandle.rectTransform;
            verticalScrollbar.targetGraphic = verticalHandle;
            verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;

            var scroll = center.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content.GetComponent<RectTransform>();
            scroll.horizontal = true;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.horizontalScrollbar = horizontalScrollbar;
            scroll.verticalScrollbar = verticalScrollbar;
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.horizontalScrollbarSpacing = 4f;
            scroll.verticalScrollbarSpacing = 4f;
            scroll.scrollSensitivity = 36f;

            var right = ImageChild(panel.transform, "DetailsPanel", new Vector2(285f, 620f), new Vector2(500f, -38f),
                "bg2", new Color(0.08f, 0.1f, 0.11f, 0.98f), true);
            TextChild(right.transform, "DetailsHeader", new Vector2(240f, 30f), new Vector2(0f, 270f),
                "SKILL DETAILS", 16f, TextAlignmentOptions.Center).color = new Color(1f, 0.62f, 0.16f);
            var detailIcon = ImageChild(right.transform, "DetailIcon", new Vector2(104f, 104f), new Vector2(0f, 195f),
                "item", Color.white, true);
            detailIcon.preserveAspect = true;
            var detailName = TextChild(right.transform, "DetailName", new Vector2(250f, 38f), new Vector2(0f, 120f),
                "SKILL", 24f, TextAlignmentOptions.Center);
            var detailDescription = TextChild(right.transform, "DetailDescription", new Vector2(245f, 145f),
                new Vector2(0f, 22f), "Description", 16f, TextAlignmentOptions.TopLeft);
            detailDescription.textWrappingMode = TextWrappingModes.Normal;
            var requirements = TextChild(right.transform, "Requirements", new Vector2(245f, 130f),
                new Vector2(0f, -125f), "Requirements", 15f, TextAlignmentOptions.TopLeft);
            requirements.textWrappingMode = TextWrappingModes.Normal;
            var detailState = TextChild(right.transform, "State", new Vector2(240f, 28f), new Vector2(0f, -220f),
                "LOCKED", 17f, TextAlignmentOptions.Center);
            detailState.color = new Color(1f, 0.62f, 0.16f);
            var buy = ButtonChild(right.transform, "BuyButton", new Vector2(220f, 52f), new Vector2(0f, -270f),
                "BUY", 19f);
            var feedback = TextChild(panel.transform, "Feedback", new Vector2(650f, 28f), new Vector2(-15f, -354f),
                "K TOGGLE   •   ESC CLOSE", 14f, TextAlignmentOptions.Center);

            var nodeViews = new List<SkillTreeNodeView>();
            foreach (var node in profession.SkillNodes)
            {
                if (node == null) continue;
                var instance = PrefabUtility.InstantiatePrefab(nodePrefab.gameObject, nodesRoot.transform) as GameObject;
                instance.name = $"Node_{node.NodeId}";
                var view = instance.GetComponent<SkillTreeNodeView>();
                var viewSo = new SerializedObject(view);
                viewSo.FindProperty("configuredNode").objectReferenceValue = node;
                viewSo.ApplyModifiedPropertiesWithoutUndo();
                nodeViews.Add(view);
            }
            var edgeCount = profession.SkillNodes.Where(node => node != null).Sum(node => node.ParentNodes.Count);
            var connectionViews = new List<SkillTreeConnectionView>();
            for (var i = 0; i < edgeCount; i++)
            {
                var instance = PrefabUtility.InstantiatePrefab(connectionPrefab.gameObject, connections.transform) as GameObject;
                instance.name = $"Connection_{i + 1:00}";
                connectionViews.Add(instance.GetComponent<SkillTreeConnectionView>());
            }

            var presenter = root.AddComponent<SkillTreeWindowPresenter>();
            var presenterSo = new SerializedObject(presenter);
            presenterSo.FindProperty("windowGroup").objectReferenceValue = group;
            presenterSo.FindProperty("closeButton").objectReferenceValue = close;
            presenterSo.FindProperty("titleText").objectReferenceValue = title;
            presenterSo.FindProperty("levelText").objectReferenceValue = level;
            presenterSo.FindProperty("coinsText").objectReferenceValue = coins;
            presenterSo.FindProperty("feedbackText").objectReferenceValue = feedback;
            presenterSo.FindProperty("professionIcon").objectReferenceValue = professionIcon;
            presenterSo.FindProperty("professionNameText").objectReferenceValue = professionName;
            presenterSo.FindProperty("professionDescriptionText").objectReferenceValue = professionDescription;
            presenterSo.FindProperty("scrollContent").objectReferenceValue = content.GetComponent<RectTransform>();
            presenterSo.FindProperty("connectionRoot").objectReferenceValue = connections.transform;
            presenterSo.FindProperty("nodeRoot").objectReferenceValue = nodesRoot.transform;
            SetObjectList(presenterSo.FindProperty("nodeViews"), nodeViews);
            SetObjectList(presenterSo.FindProperty("connectionViews"), connectionViews);
            presenterSo.FindProperty("detailIcon").objectReferenceValue = detailIcon;
            presenterSo.FindProperty("detailNameText").objectReferenceValue = detailName;
            presenterSo.FindProperty("detailDescriptionText").objectReferenceValue = detailDescription;
            presenterSo.FindProperty("detailRequirementsText").objectReferenceValue = requirements;
            presenterSo.FindProperty("detailStateText").objectReferenceValue = detailState;
            presenterSo.FindProperty("buyButton").objectReferenceValue = buy;
            presenterSo.FindProperty("buyButtonText").objectReferenceValue = buy.GetComponentInChildren<TextMeshProUGUI>();
            presenterSo.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, WindowPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void IntegrateScene(Scene scene, ProfessionDefinitionSO profession, GameObject windowPrefab)
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<PlayerSkillTreeController>(FindObjectsInactive.Include);
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerClassController>(FindObjectsInactive.Include);
            if (controller == null && player != null) controller = player.gameObject.AddComponent<PlayerSkillTreeController>();
            if (controller == null) throw new InvalidOperationException("PlayerSkillTreeController not found.");
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("startingProfession").objectReferenceValue = profession;
            var professions = controllerSo.FindProperty("availableProfessions");
            professions.arraySize = 1;
            professions.GetArrayElementAtIndex(0).objectReferenceValue = profession;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            var old = UnityEngine.Object.FindFirstObjectByType<SkillTreeWindowPresenter>(FindObjectsInactive.Include);
            Transform parent = null;
            if (old != null)
            {
                parent = old.transform.parent;
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            var imported = UnityEngine.Object.FindFirstObjectByType<EverrealmSkillTreeVisual>(FindObjectsInactive.Include);
            if (imported != null)
            {
                parent ??= imported.transform.parent;
                UnityEngine.Object.DestroyImmediate(imported.gameObject);
            }
            if (parent == null)
            {
                var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(candidate => candidate.name == "SkillTreeCanvas");
                if (canvas == null) canvas = CreateCanvas(scene);
                parent = canvas.transform;
            }

            var instance = PrefabUtility.InstantiatePrefab(windowPrefab, parent) as GameObject;
            instance.name = "ProfessionSkillTreeWindow";
            var rect = (RectTransform)instance.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var presenter = instance.GetComponent<EverrealmSkillTreeVisual>();
            var so = new SerializedObject(presenter);
            so.FindProperty("_controller").objectReferenceValue = controller;
            so.ApplyModifiedPropertiesWithoutUndo();
            instance.SetActive(true);
        }

        private static Canvas CreateCanvas(Scene scene)
        {
            var go = new GameObject("SkillTreeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(go, scene);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void LoadAtlas()
        {
            _atlas = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>()
                .GroupBy(sprite => sprite.name).ToDictionary(group => group.Key, group => group.First());
        }
        private static Sprite Skin(string name) => !string.IsNullOrWhiteSpace(name) && _atlas.TryGetValue(name, out var sprite) ? sprite : null;
        private static Sprite LoadIcon(int number) => AssetDatabase.LoadAssetAtPath<Sprite>($"{IconFolder}/Skill_{number}.png");
        private static void PrepareIcon(int number)
        {
            var path = $"{IconFolder}/Skill_{number}.png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static GameObject RectObject(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return go;
        }
        private static Image ImageChild(Transform parent, string name, Vector2 size, Vector2 position,
            string spriteName, Color color, bool sliced)
        {
            var go = RectObject(name, parent, size, position);
            var image = go.AddComponent<Image>();
            SetImage(image, spriteName, color, sliced);
            image.raycastTarget = false;
            return image;
        }
        private static void SetImage(Image image, string spriteName, Color color, bool sliced)
        {
            image.sprite = Skin(spriteName);
            image.color = color;
            if (image.sprite != null && sliced) image.type = Image.Type.Sliced;
        }
        private static TextMeshProUGUI TextChild(Transform parent, string name, Vector2 size, Vector2 position,
            string text, float fontSize, TextAlignmentOptions alignment)
        {
            var go = RectObject(name, parent, size, position);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.92f, 0.9f, 0.82f, 1f);
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }
        private static Button ButtonChild(Transform parent, string name, Vector2 size, Vector2 position,
            string text, float fontSize)
        {
            var go = RectObject(name, parent, size, position);
            var image = go.AddComponent<Image>();
            SetImage(image, "button", new Color(0.75f, 0.43f, 0.12f, 1f), true);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            TextChild(go.transform, "Text", size, Vector2.zero, text, fontSize, TextAlignmentOptions.Center);
            return button;
        }
        private static void AddShadow(GameObject go, Vector2 distance, Color color)
        {
            var shadow = go.AddComponent<Shadow>();
            shadow.effectDistance = distance;
            shadow.effectColor = color;
        }
        private static void ConfigureTopLeftLayer(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
        }
        private static void SetObjectList<T>(SerializedProperty property, IList<T> values) where T : UnityEngine.Object
        {
            property.arraySize = values.Count;
            for (var i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
        private static void RegisterSoftKittySettings()
        {
            var settings = AssetDatabase.LoadMainAssetAtPath("Assets/SoftKitty/Data/SGD_Settings.asset");
            if (settings != null) EditorBuildSettings.AddConfigObject("com.SoftKitty.settings", settings, true);
        }
    }
}
#endif
