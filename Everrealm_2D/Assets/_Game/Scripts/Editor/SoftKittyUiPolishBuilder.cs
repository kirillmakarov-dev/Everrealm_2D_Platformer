#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LetterHunter.Items;
using LetterHunter.Characters;
using LetterHunter.UI.Hud;
using LetterHunter.UI.Inventory;
using LetterHunter.UI.SkillTree;
using LetterHunter.UI.Skills;
using LetterHunter.UI.Shop;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LetterHunter.EditorTools
{
    /// <summary>
    /// Applies the local SoftKitty Inventory Engine art to authored scene UI and prefabs.
    /// Runtime presenters only update serialized views and never create visual objects.
    /// </summary>
    public static class SoftKittyUiPolishBuilder
    {
        private const string AtlasPath = "Assets/SoftKitty/InventoryEngine/Textures/Sprites/Main.png";
        private const string CurrencyPath = "Assets/SoftKitty/InventoryEngine/Textures/Currency/Currency0.png";
        private const string InventoryWindowPrefabPath = "Assets/_Game/Prefabs/UI/InventoryWindow.prefab";
        private const string ShopWindowPrefabPath = "Assets/_Game/Prefabs/UI/ShopWindow.prefab";

        private static readonly Color Parchment = new(0.93f, 0.87f, 0.73f, 1f);
        private static readonly Color Gold = new(0.92f, 0.69f, 0.27f, 1f);
        private static readonly Color Steel = new(0.72f, 0.76f, 0.78f, 1f);
        private static Dictionary<string, Sprite> sprites;

        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return;
            if (!LoadSoftKittySprites())
            {
                Debug.LogError($"SoftKitty atlas is missing or not imported as multiple sprites: {AtlasPath}");
                return;
            }

            SkinProjectPrefabs();
            AssignSoftKittyContentIcons();
            RemoveLegacyInventoryHud();

            foreach (var vitals in UnityEngine.Object.FindObjectsByType<PlayerVitalsHudPresenter>(FindObjectsSortMode.None))
                PolishVitals(vitals.transform);
            foreach (var stats in UnityEngine.Object.FindObjectsByType<PlayerStatsPanelPresenter>(FindObjectsSortMode.None))
                PolishStats(stats.transform);
            foreach (var inventory in UnityEngine.Object.FindObjectsByType<InventoryWindowPresenter>(FindObjectsSortMode.None))
                BuildInventory(inventory);
            foreach (var skillTree in UnityEngine.Object.FindObjectsByType<SkillTreeWindowPresenter>(FindObjectsSortMode.None))
                BuildSkillTree(skillTree);
            foreach (var skillBar in UnityEngine.Object.FindObjectsByType<SkillBarPresenter>(FindObjectsSortMode.None))
                BuildSkillBar(skillBar);
            foreach (var shop in Resources.FindObjectsOfTypeAll<ShopWindowPresenter>())
            {
                if (shop != null && shop.gameObject.scene.path == scene.path)
                    BuildShop(shop);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("SoftKitty UI built from package sprites. Scene objects and project UI prefabs are Inspector-editable.");
        }

        [MenuItem("Tools/Letter Hunter/UI/Build SoftKitty Inventory")]
        public static void BuildInventoryOnly()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return;
            if (!LoadSoftKittySprites())
            {
                Debug.LogError($"SoftKitty atlas is missing or not imported as multiple sprites: {AtlasPath}");
                return;
            }

            SkinPrefab("Assets/_Game/Prefabs/UI/InventorySlot.prefab", SkinInventorySlot);
            foreach (var inventory in UnityEngine.Object.FindObjectsByType<InventoryWindowPresenter>(FindObjectsSortMode.None))
                BuildInventory(inventory);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("SoftKitty inventory visuals built. Runtime inventory logic was preserved.");
        }

        [MenuItem("Tools/Letter Hunter/UI/Build SoftKitty Player Stats")]
        public static void BuildPlayerStatsOnly()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !LoadSoftKittySprites())
                return;

            BuildPlayerStatsPrefabOnly();
            foreach (var stats in UnityEngine.Object.FindObjectsByType<PlayerStatsPanelPresenter>(FindObjectsSortMode.None))
                PolishStats(stats.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("SoftKitty Player Stats visuals built. Runtime stats logic and serialized value bindings were preserved.");
        }

        public static void BuildPlayerStatsPrefabOnly()
        {
            if (sprites == null && !LoadSoftKittySprites())
                return;
            SkinPrefab("Assets/_Game/Prefabs/UI/PlayerStatsPanel.prefab", PolishStats);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildPlayerStatsDebugScene()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/InventoryLootDebug.unity", OpenSceneMode.Single);
            BuildPlayerStatsOnly();
        }

        public static void BuildInventoryDebugScene()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/InventoryLootDebug.unity", OpenSceneMode.Single);
            BuildInventoryOnly();
        }

        [MenuItem("Tools/Letter Hunter/UI/Build SoftKitty Shop")]
        public static void BuildShopOnly()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return;
            if (!LoadSoftKittySprites())
            {
                Debug.LogError($"SoftKitty atlas is missing or not imported as multiple sprites: {AtlasPath}");
                return;
            }

            SkinPrefab("Assets/_Game/Prefabs/UI/ShopItemRow.prefab", SkinShopRow);
            ReserveShopToggleKey(scene);
            foreach (var shop in Resources.FindObjectsOfTypeAll<ShopWindowPresenter>())
            {
                if (shop != null && shop.gameObject.scene.path == scene.path)
                    BuildShop(shop);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("SoftKitty shop visuals built. Shop logic and the O toggle were preserved.");
        }

        private static void ReserveShopToggleKey(Scene scene)
        {
            foreach (var router in Resources.FindObjectsOfTypeAll<CharacterInputRouter>())
            {
                if (router == null || router.gameObject.scene.path != scene.path)
                    continue;
                var routerSo = new SerializedObject(router);
                var legacySkillKeys = routerSo.FindProperty("skillKeys");
                if (legacySkillKeys == null)
                    continue;
                legacySkillKeys.arraySize = 0;
                routerSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(router);
            }
        }

        public static void BuildShopDebugScene()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/InventoryLootDebug.unity", OpenSceneMode.Single);
            BuildShopOnly();
        }

        public static void BuildInventoryAndShopDebugScene()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/InventoryLootDebug.unity", OpenSceneMode.Single);
            BuildInventoryOnly();
            BuildShopOnly();
        }

        [MenuItem("Tools/Letter Hunter/UI/Create Editable Inventory And Shop Prefabs")]
        public static void CreateEditableWindowPrefabs()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !LoadSoftKittySprites())
                return;

            SkinPrefab("Assets/_Game/Prefabs/UI/InventorySlot.prefab", SkinInventorySlot);
            SkinPrefab("Assets/_Game/Prefabs/UI/ShopItemRow.prefab", SkinShopRow);

            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryWindowPresenter>(FindObjectsInactive.Include);
            if (inventory != null)
            {
                BuildInventory(inventory);
                SaveOrApplyWindowPrefab(inventory.gameObject, InventoryWindowPrefabPath);
            }

            var shop = UnityEngine.Object.FindFirstObjectByType<ShopWindowPresenter>(FindObjectsInactive.Include);
            if (shop != null)
            {
                BuildShop(shop);
                SaveOrApplyWindowPrefab(shop.gameObject, ShopWindowPrefabPath);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Inventory and shop are authored prefab instances. Open their prefab assets to tune every UI component.");
        }

        public static void CreateEditableWindowPrefabsDebugScene()
        {
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/InventoryLootDebug.unity", OpenSceneMode.Single);
            CreateEditableWindowPrefabs();
        }

        private static void SaveOrApplyWindowPrefab(GameObject root, string path)
        {
            var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(root);
            if (instanceRoot == root)
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(root);
                if (source != null && AssetDatabase.GetAssetPath(source) == path)
                {
                    PrefabUtility.ApplyPrefabInstance(root, InteractionMode.AutomatedAction);
                    return;
                }
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
        }

        private static bool LoadSoftKittySprites()
        {
            sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>()
                .GroupBy(sprite => sprite.name)
                .ToDictionary(group => group.Key, group => group.First());
            return sprites.Count > 0;
        }

        private static Sprite Skin(string name) =>
            sprites != null && sprites.TryGetValue(name, out var sprite) ? sprite : null;

        private static void SkinProjectPrefabs()
        {
            SkinPrefab("Assets/_Game/Prefabs/UI/InventorySlot.prefab", SkinInventorySlot);
            SkinPrefab("Assets/_Game/Prefabs/UI/SkillSlot.prefab", SkinSkillSlot);
            SkinPrefab("Assets/_Game/Prefabs/UI/SkillTreeNode.prefab", SkinSkillTreeNode);
            SkinPrefab("Assets/_Game/Prefabs/UI/ShopItemRow.prefab", SkinShopRow);
            SkinPrefab("Assets/_Game/Prefabs/UI/PlayerStatsPanel.prefab", root => StylePanel(root, "bg2"));
            SkinPrefab("Assets/_Game/Prefabs/UI/PlayerVitalsHud.prefab", PolishVitals);
        }

        private static void SkinPrefab(string path, Action<Transform> action)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                action(root.transform);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssignSoftKittyContentIcons()
        {
            SetAssetSprite("Assets/_Game/Data/Items/RustySword.asset", "icon", Skin("icon_equip"));
            SetAssetSprite("Assets/_Game/Data/Items/TrainingShard.asset", "icon", Skin("icon_material"));

            var skillIcons = new[]
            {
                Skin("icon_hammer"), Skin("icon_craft"), Skin("icon_forge"), Skin("icon_equip"),
                Skin("Enchant"), Skin("star"), Skin("up"), Skin("Plus"), Skin("icon_lock")
            }.Where(sprite => sprite != null).ToArray();
            if (skillIcons.Length == 0)
                return;

            var skillAssets = AssetDatabase.FindAssets("t:SkillDefinition", new[] { "Assets/_Game/Data" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
            for (var i = 0; i < skillAssets.Length; i++)
                SetAssetSprite(skillAssets[i], "icon", skillIcons[i % skillIcons.Length]);

            foreach (var treeGuid in AssetDatabase.FindAssets("t:SkillTreeDefinition", new[] { "Assets/_Game/Data" }))
            {
                var tree = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(treeGuid));
                if (tree == null)
                    continue;
                var so = new SerializedObject(tree);
                var nodes = so.FindProperty("nodes");
                if (nodes == null)
                    continue;
                for (var i = 0; i < nodes.arraySize; i++)
                {
                    var icon = nodes.GetArrayElementAtIndex(i).FindPropertyRelative("icon");
                    if (icon != null)
                        icon.objectReferenceValue = skillIcons[i % skillIcons.Length];
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tree);
            }
        }

        private static void SetAssetSprite(string path, string propertyName, Sprite sprite)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null || sprite == null)
                return;
            var so = new SerializedObject(asset);
            var property = so.FindProperty(propertyName);
            if (property == null)
                return;
            property.objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void RemoveLegacyInventoryHud()
        {
            foreach (var legacy in Resources.FindObjectsOfTypeAll<InventoryHudPresenter>())
            {
                if (legacy != null && legacy.gameObject.scene.IsValid())
                    Undo.DestroyObjectImmediate(legacy.gameObject);
            }
        }

        private static void PolishVitals(Transform root)
        {
            foreach (var orb in root.GetComponentsInChildren<VitalsOrbView>(true))
            {
                if (orb.transform is RectTransform rect)
                    rect.sizeDelta = new Vector2(142f, 142f);
                SetImage(orb.GetComponent<Image>(), "circle_frame", Steel, true);
                AddShadow(orb.gameObject, 8f);
            }
            StyleText(root);
        }

        private static void PolishStats(Transform root)
        {
            StylePanel(root, "bg5");
            var panelRect = root.GetComponent<RectTransform>();
            if (panelRect != null)
                panelRect.sizeDelta = new Vector2(360f, 380f);

            var header = EnsureImage(root, "StatsHeader");
            SetRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(.5f, 1f), new Vector2(0f, -10f), new Vector2(-24f, 62f));
            SetImage(header, "bg3", new Color(.24f, .19f, .14f, .98f), true);
            header.raycastTarget = false;
            header.transform.SetAsFirstSibling();

            var headerLine = EnsureImage(root, "StatsHeaderLine");
            SetRect(headerLine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(.5f, 1f), new Vector2(0f, -72f), new Vector2(-36f, 5f));
            SetImage(headerLine, "line2", Gold, true);
            headerLine.raycastTarget = false;

            var title = FindDeep(root, "Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.text = "PLAYER STATS";
                title.fontSize = 25f;
                title.fontStyle = FontStyles.Bold;
                title.alignment = TextAlignmentOptions.Center;
                title.color = Gold;
                SetRect(title.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f),
                    new Vector2(.5f, 1f), new Vector2(0f, -33f), new Vector2(240f, 42f));
            }

            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                var name = image.name.ToLowerInvariant();
                if (name.Contains("row") || name.Contains("background"))
                {
                    SetImage(image, "field1", new Color(.82f, .72f, .55f, .92f), true);
                    image.raycastTarget = false;
                }
            }

            var statsIcons = new[]
            {
                ("LevelRow", "star"),
                ("AttackRow", "icon_hammer"),
                ("DefenseRow", "icon_lock"),
                ("MoveSpeedRow", "up"),
                ("AttackSpeedRow", "icon_craft"),
                ("JumpHeightRow", "Plus")
            };
            foreach (var (rowName, spriteName) in statsIcons)
            {
                var row = FindDeep(root, rowName);
                if (row == null)
                    continue;

                var rowRect = row.GetComponent<RectTransform>();
                if (rowRect != null)
                {
                    rowRect.anchorMin = new Vector2(.5f, .5f);
                    rowRect.anchorMax = new Vector2(.5f, .5f);
                    rowRect.pivot = new Vector2(.5f, .5f);
                    rowRect.sizeDelta = new Vector2(320f, 36f);
                    var rowIndex = Array.IndexOf(statsIcons, (rowName, spriteName));
                    rowRect.anchoredPosition = new Vector2(0f, 70f - rowIndex * 42f);
                }

                var icon = EnsureImage(row, "StatIcon");
                SetRect(icon.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                    new Vector2(.5f, .5f), new Vector2(14f, 0f), new Vector2(24f, 24f));
                SetImage(icon, spriteName, Gold, false);
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                var label = row.Find("Label")?.GetComponent<TMP_Text>();
                if (label != null)
                {
                    SetRect(label.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f),
                        new Vector2(0f, .5f), new Vector2(44f, 0f), new Vector2(190f, 28f));
                    label.color = Parchment;
                    label.fontSize = 15f;
                }

                var value = row.Find("Value")?.GetComponent<TMP_Text>();
                if (value != null)
                {
                    SetRect(value.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f),
                        new Vector2(1f, .5f), new Vector2(-12f, 0f), new Vector2(64f, 28f));
                    value.color = Color.white;
                    value.fontSize = 18f;
                    value.fontStyle = FontStyles.Bold;
                }
            }

            StyleText(root);
        }

        private static void BuildInventory(InventoryWindowPresenter presenter)
        {
            var so = new SerializedObject(presenter);
            var slotRoot = so.FindProperty("slotRoot").objectReferenceValue as Transform;
            if (slotRoot == null)
                slotRoot = presenter.transform.Find("Slots") ?? presenter.transform.Find("SlotGrid");
            if (slotRoot == null)
                return;

            var capacity = 24;
            var inventory = so.FindProperty("inventory").objectReferenceValue as PlayerInventory;
            if (inventory != null)
                capacity = Mathf.Max(1, new SerializedObject(inventory).FindProperty("capacity").intValue);
            var prefab = AssetDatabase.LoadAssetAtPath<InventorySlotView>("Assets/_Game/Prefabs/UI/InventorySlot.prefab");
            if (prefab == null)
                return;

            var grid = slotRoot.GetComponent<GridLayoutGroup>() ?? slotRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(72f, 72f);
            grid.spacing = new Vector2(10f, 10f);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            var slots = so.FindProperty("slotViews");
            slots.arraySize = capacity;
            for (var i = 0; i < capacity; i++)
            {
                var slot = FindOrCreateChild(slotRoot, $"Slot_{i + 1:00}", prefab);
                if (slot == null)
                    continue;
                slot.gameObject.SetActive(true);
                slots.GetArrayElementAtIndex(i).objectReferenceValue = slot;
                SkinInventorySlot(slot.transform);
            }
            so.FindProperty("dragGhost").objectReferenceValue = EnsureGhost(presenter.transform, "DraggedItemIcon", 72f);
            so.ApplyModifiedPropertiesWithoutUndo();
            LayoutInventoryWindow(presenter);
            AssignCurrencySprites(presenter.transform);
        }

        private static void LayoutInventoryWindow(InventoryWindowPresenter presenter)
        {
            var root = presenter.transform;
            var rootRect = root as RectTransform;
            if (rootRect != null)
            {
                rootRect.anchorMin = new Vector2(0f, 0.5f);
                rootRect.anchorMax = new Vector2(0f, 0.5f);
                rootRect.pivot = new Vector2(0f, 0.5f);
                rootRect.anchoredPosition = new Vector2(24f, 0f);
                rootRect.sizeDelta = new Vector2(500f, 650f);
            }

            SetImage(root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>(), "bg5",
                new Color(0.16f, 0.13f, 0.11f, 0.99f), true);
            AddFrame(root, "SoftKittyFrame", "frame2", new Color(0.7f, 0.55f, 0.32f, 1f));
            AddShadow(root.gameObject, 12f);

            var header = EnsureImage(root, "InventoryHeader");
            SetRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-28f, 86f));
            SetImage(header, "bg3", new Color(0.24f, 0.19f, 0.14f, 0.98f), true);
            header.raycastTarget = false;
            header.transform.SetAsFirstSibling();

            var headerLine = EnsureImage(root, "InventoryHeaderLine");
            SetRect(headerLine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -91f), new Vector2(-42f, 6f));
            SetImage(headerLine, "line2", Gold, true);
            headerLine.raycastTarget = false;

            var title = FindDeep(root, "Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.text = "INVENTORY";
                title.fontSize = 30f;
                title.fontStyle = FontStyles.Bold;
                title.alignment = TextAlignmentOptions.Center;
                title.color = new Color(0.94f, 0.78f, 0.45f, 1f);
                SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -27f), new Vector2(320f, 48f));
            }

            var inventoryIcon = EnsureImage(root, "InventoryTitleIcon");
            SetRect(inventoryIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(43f, -52f), new Vector2(42f, 42f));
            SetImage(inventoryIcon, "icon_inventory", new Color(0.94f, 0.78f, 0.45f, 1f), false);
            inventoryIcon.preserveAspect = true;
            inventoryIcon.raycastTarget = false;

            var close = EnsureButton(root, "CloseButton");
            SetRect(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(-40f, -51f), new Vector2(34f, 34f));
            SetImage(close.GetComponent<Image>(), "close", Parchment, false);
            ClearPersistentListeners(close);
            UnityEventTools.AddPersistentListener(close.onClick, presenter.CloseInventory);

            var slotRoot = new SerializedObject(presenter).FindProperty("slotRoot").objectReferenceValue as RectTransform;
            if (slotRoot != null)
                SetRect(slotRoot, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(0f, 1f), new Vector2(45f, -118f), new Vector2(400f, 400f));

            var footer = FindDeep(root, "GoldRow");
            if (footer != null)
            {
                var footerRect = footer as RectTransform;
                SetRect(footerRect, new Vector2(0f, 0f), new Vector2(1f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(-58f, 54f));
                SetImage(footer.GetComponent<Image>() ?? footer.gameObject.AddComponent<Image>(), "bar1",
                    new Color(0.38f, 0.3f, 0.19f, 1f), true);
                AddFrame(footer, "FooterFrame", "frame1", new Color(0.68f, 0.55f, 0.34f, 0.9f));
            }

            var goldText = new SerializedObject(presenter).FindProperty("goldText").objectReferenceValue as TMP_Text;
            if (goldText != null)
            {
                goldText.fontSize = 24f;
                goldText.alignment = TextAlignmentOptions.MidlineLeft;
                goldText.color = new Color(0.96f, 0.82f, 0.48f, 1f);
                SetRect(goldText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(56f, 0f), new Vector2(-16f, 38f));
            }

            var coinIcon = FindDeep(root, "CoinIcon")?.GetComponent<Image>();
            if (coinIcon != null)
            {
                SetRect(coinIcon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0.5f, 0.5f), new Vector2(30f, 0f), new Vector2(24f, 24f));
                coinIcon.preserveAspect = true;
                coinIcon.raycastTarget = false;
            }

            var capacityText = new SerializedObject(presenter).FindProperty("capacityText").objectReferenceValue as TMP_Text;
            if (capacityText != null)
            {
                capacityText.fontSize = 22f;
                capacityText.alignment = TextAlignmentOptions.MidlineRight;
                capacityText.color = Steel;
                SetRect(capacityText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(-12f, 38f));
            }

            var hint = FindDeep(root, "Hint")?.GetComponent<TMP_Text>();
            if (hint != null)
            {
                hint.text = "B  CLOSE     •     RMB  SPLIT STACK";
                hint.fontSize = 15f;
                hint.alignment = TextAlignmentOptions.Center;
                hint.color = new Color(0.66f, 0.62f, 0.55f, 1f);
                SetRect(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0.5f, 0f), new Vector2(0f, 93f), new Vector2(420f, 28f));
            }

            var ghost = FindDeep(root, "DraggedItemIcon");
            ghost?.SetAsLastSibling();
            StyleText(root);
        }

        private static void BuildSkillTree(SkillTreeWindowPresenter presenter)
        {
            var so = new SerializedObject(presenter);
            var nodeRoot = so.FindProperty("nodeRoot").objectReferenceValue as Transform;
            var connectionRoot = so.FindProperty("connectionRoot").objectReferenceValue as Transform;
            var controller = so.FindProperty("controller").objectReferenceValue as LetterHunter.SkillTree.PlayerSkillTreeController;
            var tree = controller != null ? controller.SkillTree : null;
            var nodePrefab = AssetDatabase.LoadAssetAtPath<SkillTreeNodeView>("Assets/_Game/Prefabs/UI/SkillTreeNode.prefab");
            var connectionPrefab = AssetDatabase.LoadAssetAtPath<SkillTreeConnectionView>("Assets/_Game/Prefabs/UI/SkillTreeConnection.prefab");
            if (nodeRoot == null || connectionRoot == null || tree == null || nodePrefab == null || connectionPrefab == null)
                return;

            var nodeViews = so.FindProperty("nodeViews");
            nodeViews.arraySize = tree.Nodes.Count;
            for (var i = 0; i < tree.Nodes.Count; i++)
            {
                var node = tree.Nodes[i];
                if (node == null)
                    continue;
                var view = FindOrCreateChild(nodeRoot, $"AuthoredNode_{node.NodeId}", nodePrefab);
                if (view == null)
                    continue;
                view.gameObject.SetActive(true);
                var viewSo = new SerializedObject(view);
                viewSo.FindProperty("configuredNodeId").stringValue = node.NodeId;
                viewSo.ApplyModifiedPropertiesWithoutUndo();
                nodeViews.GetArrayElementAtIndex(i).objectReferenceValue = view;
                SkinSkillTreeNode(view.transform);
            }

            var edgeCount = tree.Nodes.Where(node => node != null).Sum(node => node.PrerequisiteNodeIds.Count);
            var connectionViews = so.FindProperty("connectionViews");
            connectionViews.arraySize = edgeCount;
            for (var i = 0; i < edgeCount; i++)
            {
                var connection = FindOrCreateChild(connectionRoot, $"AuthoredConnection_{i + 1:00}", connectionPrefab);
                if (connection == null)
                    continue;
                connection.gameObject.SetActive(true);
                connectionViews.GetArrayElementAtIndex(i).objectReferenceValue = connection;
                SetImage(connection.GetComponent<Image>(), "line4", Gold, false);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            StylePanel(presenter.transform, "bg2");
        }

        private static void BuildSkillBar(SkillBarPresenter presenter)
        {
            var so = new SerializedObject(presenter);
            var slotRoot = so.FindProperty("slotRoot").objectReferenceValue as Transform ?? presenter.transform;
            var prefab = AssetDatabase.LoadAssetAtPath<SkillSlotView>("Assets/_Game/Prefabs/UI/SkillSlot.prefab");
            if (prefab == null)
                return;
            var maxSlots = Mathf.Max(1, so.FindProperty("maxSlots").intValue);
            var authored = so.FindProperty("authoredSlots");
            authored.arraySize = maxSlots;
            for (var i = 0; i < maxSlots; i++)
            {
                var view = FindOrCreateChild(slotRoot, $"SkillSlot_{i + 1}", prefab);
                if (view == null)
                    continue;
                authored.GetArrayElementAtIndex(i).objectReferenceValue = view;
                view.gameObject.SetActive(true);
                SkinSkillSlot(view.transform);
            }
            so.FindProperty("assignmentGhost").objectReferenceValue = EnsureGhost(presenter.transform, "DraggedSkillIcon", 64f);
            so.ApplyModifiedPropertiesWithoutUndo();
            StylePanel(presenter.transform, "bar2");
        }

        private static void BuildShop(ShopWindowPresenter presenter)
        {
            var so = new SerializedObject(presenter);
            var rowRoot = so.FindProperty("rowRoot").objectReferenceValue as Transform;
            var prefab = AssetDatabase.LoadAssetAtPath<ShopItemRowView>("Assets/_Game/Prefabs/UI/ShopItemRow.prefab");
            if (rowRoot == null || prefab == null)
                return;
            var authored = so.FindProperty("authoredRows");
            const int rowCount = 12;
            var layout = rowRoot.GetComponent<VerticalLayoutGroup>() ?? rowRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            authored.arraySize = rowCount;
            for (var i = 0; i < rowCount; i++)
            {
                var row = FindOrCreateChild(rowRoot, $"AuthoredShopRow_{i + 1:00}", prefab);
                if (row == null)
                    continue;
                row.gameObject.SetActive(false);
                authored.GetArrayElementAtIndex(i).objectReferenceValue = row;
                SkinShopRow(row.transform);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            LayoutShopWindow(presenter, rowRoot);
            AssignCurrencySprites(presenter.transform);
        }

        private static void LayoutShopWindow(ShopWindowPresenter presenter, Transform rowRoot)
        {
            var root = presenter.transform;
            var rootRect = root as RectTransform;
            if (rootRect != null)
            {
                rootRect.anchorMin = new Vector2(1f, 0.5f);
                rootRect.anchorMax = new Vector2(1f, 0.5f);
                rootRect.pivot = new Vector2(1f, 0.5f);
                rootRect.anchoredPosition = new Vector2(-24f, 0f);
                rootRect.sizeDelta = new Vector2(540f, 700f);
            }

            SetImage(root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>(), "bg5",
                new Color(0.16f, 0.13f, 0.11f, 0.99f), true);
            AddFrame(root, "SoftKittyFrame", "frame2", new Color(0.7f, 0.55f, 0.32f, 1f));
            AddShadow(root.gameObject, 12f);

            var header = EnsureImage(root, "ShopHeader");
            SetRect(header.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-28f, 86f));
            SetImage(header, "bg3", new Color(0.24f, 0.19f, 0.14f, 0.98f), true);
            header.raycastTarget = false;
            header.transform.SetAsFirstSibling();

            var headerLine = EnsureImage(root, "ShopHeaderLine");
            SetRect(headerLine.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -91f), new Vector2(-42f, 6f));
            SetImage(headerLine, "line2", Gold, true);
            headerLine.raycastTarget = false;

            var title = FindDeep(root, "Title")?.GetComponent<TMP_Text>();
            if (title != null)
            {
                title.text = "MERCHANT";
                title.fontSize = 30f;
                title.fontStyle = FontStyles.Bold;
                title.alignment = TextAlignmentOptions.Center;
                title.color = new Color(0.94f, 0.78f, 0.45f, 1f);
                SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -27f), new Vector2(340f, 48f));
            }

            var shopIcon = EnsureImage(root, "ShopTitleIcon");
            SetRect(shopIcon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(43f, -52f), new Vector2(42f, 42f));
            SetImage(shopIcon, "icon_store", new Color(0.94f, 0.78f, 0.45f, 1f), false);
            shopIcon.preserveAspect = true;
            shopIcon.raycastTarget = false;

            var close = EnsureButton(root, "ShopCloseButton");
            SetRect(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(-40f, -51f), new Vector2(34f, 34f));
            SetImage(close.GetComponent<Image>(), "close", Parchment, false);
            ClearPersistentListeners(close);
            UnityEventTools.AddPersistentListener(close.onClick, presenter.CloseShop);

            if (rowRoot is RectTransform rowsRect)
            {
                SetRect(rowsRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(490f, 520f));
                SetImage(rowRoot.GetComponent<Image>() ?? rowRoot.gameObject.AddComponent<Image>(), "bg3",
                    new Color(0.12f, 0.1f, 0.085f, 0.88f), true);
                AddFrame(rowRoot, "ShopRowsFrame", "frame1", new Color(0.58f, 0.48f, 0.32f, 0.75f));
            }

            var footer = EnsureImage(root, "ShopFooter");
            SetRect(footer.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-58f, 48f));
            SetImage(footer, "bar1", new Color(0.38f, 0.3f, 0.19f, 1f), true);
            footer.raycastTarget = false;
            footer.transform.SetSiblingIndex(Mathf.Min(1, root.childCount - 1));

            var so = new SerializedObject(presenter);
            var goldText = so.FindProperty("goldText").objectReferenceValue as TMP_Text;
            if (goldText != null)
            {
                goldText.fontSize = 23f;
                goldText.alignment = TextAlignmentOptions.MidlineLeft;
                goldText.color = new Color(0.96f, 0.82f, 0.48f, 1f);
                SetRect(goldText.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 0f), new Vector2(72f, 23f), new Vector2(-15f, 38f));
            }

            var feedback = so.FindProperty("feedbackText").objectReferenceValue as TMP_Text;
            if (feedback != null)
            {
                feedback.fontSize = 18f;
                feedback.alignment = TextAlignmentOptions.MidlineRight;
                feedback.color = new Color(0.72f, 0.86f, 0.56f, 1f);
                SetRect(feedback.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 0f),
                    new Vector2(1f, 0f), new Vector2(-32f, 23f), new Vector2(-18f, 38f));
            }

            var empty = so.FindProperty("emptyText").objectReferenceValue as TMP_Text;
            if (empty != null)
            {
                empty.fontSize = 22f;
                empty.alignment = TextAlignmentOptions.Center;
                empty.color = new Color(0.72f, 0.68f, 0.61f, 1f);
                SetRect(empty.rectTransform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(-40f, -40f));
                empty.transform.SetAsLastSibling();
            }

            StyleText(root);
        }

        private static void SkinInventorySlot(Transform root)
        {
            SetImage(root.GetComponent<Image>(), "item", new Color(0.34f, 0.3f, 0.25f, 1f), true);
            AddFrame(root, "InventorySlotFrame", "item_frame", new Color(0.63f, 0.55f, 0.42f, 0.72f));
            var frame = FindDeep(root, "InventorySlotFrame")?.GetComponent<Image>();
            if (frame != null)
                frame.fillCenter = false;
            var highlight = FindDeep(root, "Highlight")?.GetComponent<Image>();
            SetImage(highlight, "item_frame", new Color(1f, 0.65f, 0.2f, 0.85f), true);
            if (highlight != null)
            {
                highlight.fillCenter = false;
                highlight.raycastTarget = false;
            }
            var icon = FindDeep(root, "Icon")?.GetComponent<Image>();
            if (icon != null)
            {
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.color = Color.white;
                var rect = icon.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.offsetMin = new Vector2(8f, 8f);
                rect.offsetMax = new Vector2(-8f, -8f);
                icon.transform.SetAsLastSibling();
            }
            var amount = FindDeep(root, "AmountText");
            if (amount != null)
                amount.SetAsLastSibling();
            var view = root.GetComponent<InventorySlotView>();
            if (view != null)
            {
                var viewSo = new SerializedObject(view);
                viewSo.FindProperty("emptyColor").colorValue = new Color(0.18f, 0.16f, 0.13f, 0.94f);
                viewSo.FindProperty("filledColor").colorValue = new Color(0.28f, 0.23f, 0.17f, 0.98f);
                viewSo.ApplyModifiedPropertiesWithoutUndo();
            }
            AddShadow(root.gameObject, 4f);
            StyleText(root);
        }

        private static void SkinSkillSlot(Transform root)
        {
            SetImage(root.GetComponent<Image>(), "item", Color.white, true);
            SetImage(FindDeep(root, "CooldownOverlay")?.GetComponent<Image>(), "circle_item",
                new Color(0.08f, 0.07f, 0.06f, 0.78f), false);
            AddFrame(root, "SkillSlotSoftKittyFrame", "item_frame", Gold);
            StyleText(root);
        }

        private static void SkinSkillTreeNode(Transform root)
        {
            SetImage(root.GetComponent<Image>(), "card1", Color.white, true);
            AddFrame(root, "NodeSoftKittyFrame", "frame1", Steel);
            var icon = FindDeep(root, "Icon")?.GetComponent<Image>();
            if (icon != null)
                icon.preserveAspect = true;
            StyleButtons(root);
            StyleText(root);
            AddShadow(root.gameObject, 5f);
        }

        private static void SkinShopRow(Transform root)
        {
            if (root is RectTransform rect)
                rect.sizeDelta = new Vector2(490f, 38f);
            SetImage(root.GetComponent<Image>(), "field1", new Color(0.35f, 0.3f, 0.24f, 0.96f), true);
            var layout = root.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(10, 8, 4, 4);
                layout.spacing = 6f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            var itemIcon = EnsureImage(root, "ItemIcon");
            itemIcon.preserveAspect = true;
            itemIcon.raycastTarget = false;
            itemIcon.transform.SetAsFirstSibling();
            var iconLayout = itemIcon.GetComponent<LayoutElement>() ?? itemIcon.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 30f;
            iconLayout.preferredHeight = 30f;
            iconLayout.flexibleWidth = 0f;
            ConfigureLayoutElement(FindDeep(root, "NameText"), 120f, 30f, 1f);
            ConfigureLayoutElement(FindDeep(root, "AmountText"), 44f, 30f);
            ConfigureLayoutElement(FindDeep(root, "PriceText"), 58f, 30f);
            ConfigureLayoutElement(FindDeep(root, "SellOneButton"), 70f, 30f);
            ConfigureLayoutElement(FindDeep(root, "SellStackButton"), 76f, 30f);
            var view = root.GetComponent<ShopItemRowView>();
            if (view != null)
            {
                var viewSo = new SerializedObject(view);
                viewSo.FindProperty("itemIcon").objectReferenceValue = itemIcon;
                viewSo.ApplyModifiedPropertiesWithoutUndo();
            }

            StyleButtons(root);
            StyleText(root);
            AddShadow(root.gameObject, 2f);
        }

        private static void ConfigureLayoutElement(Transform target, float preferredWidth, float preferredHeight,
            float flexibleWidth = 0f)
        {
            if (target == null)
                return;
            var element = target.GetComponent<LayoutElement>() ?? target.gameObject.AddComponent<LayoutElement>();
            element.minWidth = preferredWidth;
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = 0f;
        }

        private static void StylePanel(Transform root, string backgroundSprite)
        {
            SetImage(root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>(), backgroundSprite, Color.white, true);
            AddFrame(root, "SoftKittyFrame", "frame1", Steel);
            AddAccent(root);
            AddShadow(root.gameObject, 10f);
            StyleButtons(root);
            StyleText(root);
        }

        private static void StyleButtons(Transform root)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
                if (image == null)
                    continue;
                SetImage(image, "button", Color.white, true);
                button.targetGraphic = image;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.88f, 0.58f, 1f);
                colors.pressedColor = new Color(0.65f, 0.5f, 0.28f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.35f, 0.34f, 0.32f, 0.65f);
                button.colors = colors;
            }
        }

        private static void StyleText(Transform root)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.raycastTarget = false;
                text.color = Parchment;
                var name = text.name.ToLowerInvariant();
                if (name.Contains("title") || name.Contains("header"))
                {
                    text.color = Gold;
                    text.fontStyle = FontStyles.Bold;
                }
            }
        }

        private static void AssignCurrencySprites(Transform root)
        {
            var currency = AssetDatabase.LoadAssetAtPath<Sprite>(CurrencyPath);
            if (currency == null)
                return;
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                var name = image.name.ToLowerInvariant();
                if (!name.Contains("coin") && !name.Contains("currency") && !name.Contains("goldicon"))
                    continue;
                image.sprite = currency;
                image.preserveAspect = true;
                image.color = Color.white;
                image.raycastTarget = false;
            }
        }

        private static Image EnsureImage(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing == null)
            {
                var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imageObject.transform.SetParent(parent, false);
                existing = imageObject.transform;
            }

            return existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
        }

        private static Button EnsureButton(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing == null)
            {
                var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                buttonObject.transform.SetParent(parent, false);
                existing = buttonObject.transform;
            }

            var image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            var button = existing.GetComponent<Button>() ?? existing.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static void ClearPersistentListeners(Button button)
        {
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            for (var i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            if (rect == null)
                return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static Image EnsureGhost(Transform parent, string name, float size)
        {
            var existing = parent.Find(name);
            if (existing == null)
            {
                var ghost = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                ghost.transform.SetParent(parent, false);
                existing = ghost.transform;
            }
            ((RectTransform)existing).sizeDelta = new Vector2(size, size);
            var image = existing.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            existing.GetComponent<CanvasGroup>().blocksRaycasts = false;
            existing.SetAsLastSibling();
            return image;
        }

        private static T FindOrCreateChild<T>(Transform parent, string name, T prefab) where T : Component
        {
            var existing = parent.Find(name)?.GetComponent<T>();
            if (existing != null)
                return existing;
            var created = PrefabUtility.InstantiatePrefab(prefab.gameObject, parent) as GameObject;
            if (created == null)
                return null;
            created.name = name;
            return created.GetComponent<T>();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;
            foreach (Transform child in root)
            {
                var match = FindDeep(child, name);
                if (match != null)
                    return match;
            }
            return null;
        }

        private static void SetImage(Image image, string spriteName, Color color, bool sliced)
        {
            if (image == null)
                return;
            var sprite = Skin(spriteName);
            if (sprite != null)
                image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
        }

        private static void AddFrame(Transform root, string name, string spriteName, Color color)
        {
            var frame = root.Find(name);
            if (frame == null)
            {
                var frameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                frameObject.transform.SetParent(root, false);
                frame = frameObject.transform;
            }
            var rect = (RectTransform)frame;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = frame.GetComponent<Image>();
            SetImage(image, spriteName, color, true);
            image.raycastTarget = false;
            frame.SetAsLastSibling();
        }

        private static void AddAccent(Transform root)
        {
            var accent = root.Find("SoftKittyAccent");
            if (accent == null)
            {
                var accentObject = new GameObject("SoftKittyAccent", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                accentObject.transform.SetParent(root, false);
                accent = accentObject.transform;
            }
            var rect = (RectTransform)accent;
            rect.anchorMin = new Vector2(0f, 0.1f);
            rect.anchorMax = new Vector2(0f, 0.9f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(8f, 0f);
            rect.anchoredPosition = new Vector2(10f, 0f);
            var image = accent.GetComponent<Image>();
            SetImage(image, "line1", Gold, true);
            image.raycastTarget = false;
            accent.SetAsLastSibling();
        }

        private static void AddShadow(GameObject target, float distance)
        {
            var shadow = target.GetComponent<Shadow>() ?? target.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(distance, -distance);
            shadow.useGraphicAlpha = true;
        }
    }
}
#endif
