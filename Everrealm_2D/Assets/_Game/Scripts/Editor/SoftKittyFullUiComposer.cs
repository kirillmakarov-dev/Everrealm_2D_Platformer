#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LetterHunter.Items;
using LetterHunter.SkillTree;
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
    /// Composes the playable Letter Hunter UI from complete SoftKitty prefab windows.
    /// It runs only in the editor and saves every generated object and reference into the scene.
    /// </summary>
    public static class SoftKittyFullUiComposer
    {
        private const string ScenePath = "Assets/_Game/Scenes/InventoryLootDebug.unity";
        private const string InventoryTemplate =
            "Assets/SoftKitty/InventoryEngine/Resources/InventoryEngine/UiWindows/Inventory.prefab";
        private const string MerchantTemplate =
            "Assets/SoftKitty/InventoryEngine/Resources/InventoryEngine/UiWindows/Merchant.prefab";
        private const string SkillsTemplate =
            "Assets/SoftKitty/InventoryEngine/Resources/InventoryEngine/UiWindows/Skills.prefab";
        private const string ActionBarTemplate =
            "Assets/SoftKitty/InventoryEngine/Prefabs/Ui/ActionBar.prefab";
        private const string SkillIconFolder =
            "Assets/SoftKitty/InventoryEngine/Textures/SkillIcon";
        private const string AtlasPath =
            "Assets/SoftKitty/InventoryEngine/Textures/Sprites/Main.png";

        private static Dictionary<string, Sprite> atlas;

        [InitializeOnLoadMethod]
        private static void QueueAutomaticComposition()
        {
            EditorApplication.delayCall += ComposeIfNeeded;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += ComposeIfNeeded;
        }

        private static void ComposeIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                return;
            var inventory = UnityEngine.Object.FindFirstObjectByType<InventoryWindowPresenter>(FindObjectsInactive.Include);
            if (inventory != null && inventory.transform.Find("SoftKittyInventoryShell") == null)
                Build();
        }

        [MenuItem("Letter Hunter/Build Full SoftKitty UI", priority = 1)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before composing the SoftKitty UI.");
                return;
            }
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                Debug.LogError($"Open {ScenePath} before composing the SoftKitty UI.");
                return;
            }
            if (!LoadAtlas() || !TemplatesExist())
            {
                Debug.LogError("The local SoftKitty package is missing required UI prefabs or the Main atlas.");
                return;
            }

            RegisterSoftKittySettings();
            SoftKittyUiPolishBuilder.Build();
            PrepareSkillIcons();
            ComposeInventory();
            ComposeMerchant();
            ComposeSkillTree();
            ComposeActionBar();

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Full SoftKitty UI composition saved into InventoryLootDebug. Runtime uses only authored references.");
        }

        public static void BuildFromBatchMode()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Build();
        }

        private static bool TemplatesExist() =>
            AssetDatabase.LoadAssetAtPath<GameObject>(InventoryTemplate) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(MerchantTemplate) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(SkillsTemplate) != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(ActionBarTemplate) != null;

        private static void RegisterSoftKittySettings()
        {
            var settings = AssetDatabase.LoadMainAssetAtPath("Assets/SoftKitty/Data/SGD_Settings.asset");
            if (settings != null)
                EditorBuildSettings.AddConfigObject("com.SoftKitty.settings", settings, true);
        }

        private static bool LoadAtlas()
        {
            atlas = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>()
                .GroupBy(sprite => sprite.name).ToDictionary(group => group.Key, group => group.First());
            return atlas.Count > 0;
        }

        private static Sprite Skin(string name) => atlas.TryGetValue(name, out var sprite) ? sprite : null;

        private static void ComposeInventory()
        {
            var presenter = UnityEngine.Object.FindFirstObjectByType<InventoryWindowPresenter>(FindObjectsInactive.Include);
            if (presenter == null)
                return;
            var host = presenter.transform;
            var shell = ReplaceWithShell(host, InventoryTemplate, "SoftKittyInventoryShell", new Vector2(620f, 720f));
            if (shell == null)
                return;

            SetText(shell.transform, "TitleBar/Title/Title_text", "INVENTORY");
            SetText(shell.transform, "Tabs/Tab/title_text", "ALL ITEMS");
            Disable(shell.transform, "BlockControl");
            BindClose(FindPath(shell.transform, "TitleBar/CloseBt")?.GetComponent<Button>(), presenter.Close);

            var content = FindPath(shell.transform, "Items Scroll View/Viewport/Content");
            if (content == null)
                return;
            ClearChildren(content);
            ConfigureGrid(content, new Vector2(72f, 72f), new Vector2(8f, 8f), 6);

            var so = new SerializedObject(presenter);
            var inventory = so.FindProperty("inventory").objectReferenceValue as PlayerInventory;
            var capacity = inventory != null
                ? Mathf.Max(1, new SerializedObject(inventory).FindProperty("capacity").intValue)
                : 24;
            var slotPrefab = AssetDatabase.LoadAssetAtPath<InventorySlotView>("Assets/_Game/Prefabs/UI/InventorySlot.prefab");
            var slots = so.FindProperty("slotViews");
            slots.arraySize = capacity;
            for (var i = 0; i < capacity; i++)
            {
                var view = InstantiateComponent(slotPrefab, content, $"InventorySlot_{i + 1:00}");
                if (view == null)
                    continue;
                ((RectTransform)view.transform).sizeDelta = new Vector2(72f, 72f);
                slots.GetArrayElementAtIndex(i).objectReferenceValue = view;
            }

            var capacityText = CreateLabel(FindPath(shell.transform, "TitleBar/Title"), "CapacityValue", "0/24", 19f,
                TextAlignmentOptions.MidlineRight, new Vector2(160f, 30f));
            AnchorTopRight(capacityText.rectTransform, new Vector2(-8f, -2f));
            var goldText = CreateLabel(FindPath(shell.transform, "BottomInfo/Currency/CurrencyItem"), "GoldValue", "0", 20f,
                TextAlignmentOptions.MidlineRight, new Vector2(120f, 30f));
            AnchorCenter(goldText.rectTransform);
            var ghost = CreateGhost(host, "DraggedItemIcon", 72f);

            so.FindProperty("slotRoot").objectReferenceValue = content;
            so.FindProperty("capacityText").objectReferenceValue = capacityText;
            so.FindProperty("goldText").objectReferenceValue = goldText;
            so.FindProperty("dragGhost").objectReferenceValue = ghost;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ComposeMerchant()
        {
            var presenter = UnityEngine.Object.FindFirstObjectByType<ShopWindowPresenter>(FindObjectsInactive.Include);
            if (presenter == null)
                return;
            var host = presenter.transform;
            var shell = ReplaceWithShell(host, MerchantTemplate, "SoftKittyMerchantShell", new Vector2(1160f, 690f));
            if (shell == null)
                return;

            SetText(shell.transform, "TitleBar/Title/Title_text", "MERCHANT");
            SetText(shell.transform, "TitleBar/Title/name_text", "SELL & TRADE");
            SetText(shell.transform, "NamTabPlayer/Name", "YOUR INVENTORY");
            SetText(shell.transform, "NamTabNpc/Name", "SHOP PREVIEW");
            Disable(shell.transform, "BlockControl");
            Disable(shell.transform, "TradeButton");
            Disable(shell.transform, "CancelButton");
            BindClose(FindPath(shell.transform, "TitleBar/CloseBt")?.GetComponent<Button>(), presenter.Close);
            BindClose(FindPath(shell.transform, "CloseButton")?.GetComponent<Button>(), presenter.Close);

            var rowRoot = FindPath(shell.transform, "Items Scroll View Player/Viewport/Content") as RectTransform;
            var previewRoot = FindPath(shell.transform, "Items Scroll View Npc/Viewport/Content");
            if (rowRoot == null)
                return;
            ClearChildren(rowRoot);
            if (previewRoot != null)
                ClearChildren(previewRoot);
            ConfigureVertical(rowRoot, 8f, new RectOffset(8, 8, 8, 8));

            var so = new SerializedObject(presenter);
            var rowPrefab = AssetDatabase.LoadAssetAtPath<ShopItemRowView>("Assets/_Game/Prefabs/UI/ShopItemRow.prefab");
            var rows = so.FindProperty("authoredRows");
            const int rowCount = 12;
            rows.arraySize = rowCount;
            for (var i = 0; i < rowCount; i++)
            {
                var row = InstantiateComponent(rowPrefab, rowRoot, $"MerchantRow_{i + 1:00}");
                if (row == null)
                    continue;
                ((RectTransform)row.transform).sizeDelta = new Vector2(430f, 54f);
                row.gameObject.SetActive(false);
                rows.GetArrayElementAtIndex(i).objectReferenceValue = row;
            }

            var goldText = CreateLabel(FindPath(shell.transform, "NamTabPlayer/Currency/CurrencyItem"), "GoldValue", "0", 20f,
                TextAlignmentOptions.MidlineRight, new Vector2(120f, 28f));
            AnchorCenter(goldText.rectTransform);
            var emptyText = CreateLabel(rowRoot, "EmptyState", "No sellable items", 22f,
                TextAlignmentOptions.Center, new Vector2(420f, 70f));
            var feedback = CreateLabel(shell.transform, "TradeFeedback", string.Empty, 21f,
                TextAlignmentOptions.Center, new Vector2(460f, 38f));
            AnchorBottomCenter(feedback.rectTransform, new Vector2(0f, 18f));

            so.FindProperty("rowRoot").objectReferenceValue = rowRoot;
            so.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            so.FindProperty("goldText").objectReferenceValue = goldText;
            so.FindProperty("emptyText").objectReferenceValue = emptyText;
            so.FindProperty("feedbackText").objectReferenceValue = feedback;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ComposeSkillTree()
        {
            var presenter = UnityEngine.Object.FindFirstObjectByType<SkillTreeWindowPresenter>(FindObjectsInactive.Include);
            if (presenter == null)
                return;
            var host = presenter.transform;
            var shell = ReplaceWithShell(host, SkillsTemplate, "SoftKittySkillsShell", new Vector2(1320f, 760f));
            if (shell == null)
                return;

            SetText(shell.transform, "TitleBar/Title/Title_text", "WARRIOR SKILL TREE");
            SetText(shell.transform, "Tabs/Tab/title_text", "PROGRESSION");
            Disable(shell.transform, "BlockControl");
            BindClose(FindPath(shell.transform, "TitleBar/CloseBt")?.GetComponent<Button>(), presenter.Close);

            var content = FindPath(shell.transform, "Items Scroll View/Viewport/Content") as RectTransform;
            if (content == null)
                return;
            ClearChildren(content);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 1f);
            content.sizeDelta = new Vector2(1900f, 680f);

            var connectionRoot = CreateLayer(content, "ConnectionLayer", content.sizeDelta);
            var nodeRoot = CreateLayer(content, "NodeLayer", content.sizeDelta);
            var graph = nodeRoot.gameObject.AddComponent<SkillTreeGraphLayoutGroup>();
            graph.padding = new RectOffset(55, 340, 35, 35);

            var so = new SerializedObject(presenter);
            var controller = so.FindProperty("controller").objectReferenceValue as PlayerSkillTreeController;
            var tree = controller != null ? controller.SkillTree : null;
            var nodePrefab = AssetDatabase.LoadAssetAtPath<SkillTreeNodeView>("Assets/_Game/Prefabs/UI/SkillTreeNode.prefab");
            var connectionPrefab = AssetDatabase.LoadAssetAtPath<SkillTreeConnectionView>("Assets/_Game/Prefabs/UI/SkillTreeConnection.prefab");
            if (tree == null || nodePrefab == null || connectionPrefab == null)
                return;

            var nodes = so.FindProperty("nodeViews");
            nodes.arraySize = tree.Nodes.Count;
            for (var i = 0; i < tree.Nodes.Count; i++)
            {
                var node = tree.Nodes[i];
                if (node == null)
                    continue;
                var view = InstantiateComponent(nodePrefab, nodeRoot, $"SkillNode_{node.NodeId}");
                if (view == null)
                    continue;
                var viewSo = new SerializedObject(view);
                viewSo.FindProperty("configuredNodeId").stringValue = node.NodeId;
                viewSo.ApplyModifiedPropertiesWithoutUndo();
                nodes.GetArrayElementAtIndex(i).objectReferenceValue = view;
            }

            var edgeCount = tree.Nodes.Where(node => node != null).Sum(node => node.PrerequisiteNodeIds.Count);
            var connections = so.FindProperty("connectionViews");
            connections.arraySize = edgeCount;
            for (var i = 0; i < edgeCount; i++)
            {
                var line = InstantiateComponent(connectionPrefab, connectionRoot, $"SkillConnection_{i + 1:00}");
                if (line != null)
                    connections.GetArrayElementAtIndex(i).objectReferenceValue = line;
            }

            var title = CreateLabel(FindPath(shell.transform, "TitleBar/Title"), "RuntimeTitle", "WARRIOR SKILL TREE", 25f,
                TextAlignmentOptions.MidlineLeft, new Vector2(420f, 42f));
            AnchorCenter(title.rectTransform);
            var feedback = CreateLabel(shell.transform, "SkillFeedback", "Select a node to inspect or unlock it.", 20f,
                TextAlignmentOptions.Center, new Vector2(620f, 36f));
            AnchorBottomCenter(feedback.rectTransform, new Vector2(0f, 18f));
            var details = CreateDetailsPanel(content, new Vector2(310f, 580f), new Vector2(1550f, -325f));
            var tooltip = CreateLabel(details, "SkillDetailsText", "Hover a skill node for details.", 20f,
                TextAlignmentOptions.TopLeft, new Vector2(270f, 520f));
            AnchorCenter(tooltip.rectTransform);

            so.FindProperty("nodeRoot").objectReferenceValue = nodeRoot;
            so.FindProperty("connectionRoot").objectReferenceValue = connectionRoot;
            so.FindProperty("scrollContent").objectReferenceValue = content;
            so.FindProperty("nodePrefab").objectReferenceValue = nodePrefab;
            so.FindProperty("connectionPrefab").objectReferenceValue = connectionPrefab;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("feedbackText").objectReferenceValue = feedback;
            so.FindProperty("tooltipRoot").objectReferenceValue = details.gameObject;
            so.FindProperty("tooltipText").objectReferenceValue = tooltip;
            so.FindProperty("minimumContentWidth").floatValue = 1900f;
            so.FindProperty("minimumContentHeight").floatValue = 680f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ComposeActionBar()
        {
            var presenter = UnityEngine.Object.FindFirstObjectByType<SkillBarPresenter>(FindObjectsInactive.Include);
            if (presenter == null)
                return;
            var host = presenter.transform;
            var shell = ReplaceWithShell(host, ActionBarTemplate, "SoftKittyActionBarShell", new Vector2(840f, 130f));
            if (shell == null)
                return;
            Disable(shell.transform, "BlockControl");

            var slotRoot = FindPath(shell.transform, "Slots");
            if (slotRoot == null)
                return;
            var candidates = slotRoot.Cast<Transform>().Where(child => child.name.StartsWith("Item", StringComparison.Ordinal))
                .OrderBy(child => child.GetSiblingIndex()).Take(10).ToArray();
            var views = new List<SkillSlotView>();
            for (var i = 0; i < candidates.Length; i++)
            {
                var slot = candidates[i];
                slot.name = $"ActionSkillSlot_{i + 1}";
                var view = slot.GetComponent<SkillSlotView>() ?? slot.gameObject.AddComponent<SkillSlotView>();
                var button = slot.GetComponent<Button>() ?? slot.gameObject.AddComponent<Button>();
                ResetButton(button);
                var background = slot.GetComponent<Image>();
                var icon = FindPath(slot, "Icon")?.GetComponent<Image>();
                var cooldown = FindPath(slot, "CoolDown")?.GetComponent<Image>();
                var key = CreateLabel(FindPath(slot, "Key") ?? slot, "RuntimeKey", (i + 1).ToString(), 17f,
                    TextAlignmentOptions.Center, new Vector2(30f, 24f));
                AnchorCenter(key.rectTransform);
                var timer = CreateLabel(FindPath(slot, "CoolDown") ?? slot, "RuntimeTimer", string.Empty, 18f,
                    TextAlignmentOptions.Center, new Vector2(50f, 30f));
                AnchorCenter(timer.rectTransform);
                var viewSo = new SerializedObject(view);
                viewSo.FindProperty("button").objectReferenceValue = button;
                viewSo.FindProperty("background").objectReferenceValue = background;
                viewSo.FindProperty("icon").objectReferenceValue = icon;
                viewSo.FindProperty("cooldownOverlay").objectReferenceValue = cooldown;
                viewSo.FindProperty("cooldownText").objectReferenceValue = timer;
                viewSo.FindProperty("keyText").objectReferenceValue = key;
                viewSo.FindProperty("nameText").objectReferenceValue = null;
                viewSo.ApplyModifiedPropertiesWithoutUndo();
                views.Add(view);
            }

            var ghost = CreateGhost(host, "DraggedSkillIcon", 64f);
            var so = new SerializedObject(presenter);
            so.FindProperty("slotRoot").objectReferenceValue = slotRoot;
            so.FindProperty("slotPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SkillSlotView>(
                "Assets/_Game/Prefabs/UI/SkillSlot.prefab");
            so.FindProperty("maxSlots").intValue = views.Count;
            var authored = so.FindProperty("authoredSlots");
            authored.arraySize = views.Count;
            for (var i = 0; i < views.Count; i++)
                authored.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            so.FindProperty("assignmentGhost").objectReferenceValue = ghost;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject ReplaceWithShell(Transform host, string prefabPath, string shellName, Vector2 hostSize)
        {
            ClearChildren(host);
            if (host is RectTransform hostRect)
                hostRect.sizeDelta = hostSize;
            var hostImage = host.GetComponent<Image>();
            if (hostImage != null)
            {
                hostImage.enabled = false;
                hostImage.raycastTarget = false;
            }
            foreach (var effect in host.GetComponents<BaseMeshEffect>())
                effect.enabled = false;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var shell = PrefabUtility.InstantiatePrefab(prefab, host) as GameObject;
            if (shell == null)
                return null;
            shell.name = shellName;
            StripSoftKittyRuntime(shell);
            if (shell.transform is RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }
            return shell;
        }

        private static void StripSoftKittyRuntime(GameObject root)
        {
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                    continue;
                var typeNamespace = behaviour.GetType().Namespace;
                if (!string.IsNullOrEmpty(typeNamespace) && typeNamespace.StartsWith("SoftKitty", StringComparison.Ordinal))
                    UnityEngine.Object.DestroyImmediate(behaviour, true);
            }
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
        }

        private static void PrepareSkillIcons()
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { SkillIconFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => System.IO.Path.GetFileNameWithoutExtension(path).StartsWith("Skill_", StringComparison.Ordinal))
                .OrderBy(NaturalSkillIconIndex).Take(24).ToArray();
            foreach (var path in paths)
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                    continue;
                if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single)
                    continue;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            var icons = paths.Select(AssetDatabase.LoadAssetAtPath<Sprite>).Where(icon => icon != null).ToArray();
            if (icons.Length == 0)
                return;
            var skillAssets = AssetDatabase.FindAssets("t:SkillDefinition", new[] { "Assets/_Game/Data/Skills" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
            for (var i = 0; i < skillAssets.Length; i++)
                SetSpriteProperty(AssetDatabase.LoadMainAssetAtPath(skillAssets[i]), "icon", icons[i % icons.Length]);
            foreach (var treeGuid in AssetDatabase.FindAssets("t:SkillTreeDefinition", new[] { "Assets/_Game/Data/SkillTrees" }))
            {
                var tree = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(treeGuid));
                var so = new SerializedObject(tree);
                var nodes = so.FindProperty("nodes");
                for (var i = 0; nodes != null && i < nodes.arraySize; i++)
                    nodes.GetArrayElementAtIndex(i).FindPropertyRelative("icon").objectReferenceValue = icons[i % icons.Length];
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tree);
            }
        }

        private static int NaturalSkillIconIndex(string path)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(path);
            return int.TryParse(name.Substring("Skill_".Length), out var index) ? index : int.MaxValue;
        }

        private static void SetSpriteProperty(UnityEngine.Object asset, string propertyName, Sprite sprite)
        {
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

        private static T InstantiateComponent<T>(T prefab, Transform parent, string name) where T : Component
        {
            if (prefab == null)
                return null;
            var instance = PrefabUtility.InstantiatePrefab(prefab.gameObject, parent) as GameObject;
            if (instance == null)
                return null;
            instance.name = name;
            return instance.GetComponent<T>();
        }

        private static RectTransform CreateLayer(Transform parent, string name, Vector2 size)
        {
            var layer = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            layer.SetParent(parent, false);
            layer.anchorMin = new Vector2(0f, 1f);
            layer.anchorMax = new Vector2(0f, 1f);
            layer.pivot = new Vector2(0f, 1f);
            layer.sizeDelta = size;
            return layer;
        }

        private static RectTransform CreateDetailsPanel(Transform parent, Vector2 size, Vector2 position)
        {
            var panel = new GameObject("SkillDetailsPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<RectTransform>();
            panel.SetParent(parent, false);
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = size;
            panel.anchoredPosition = position;
            var image = panel.GetComponent<Image>();
            image.sprite = Skin("bg2");
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return panel;
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string value, float fontSize,
            TextAlignmentOptions alignment, Vector2 size)
        {
            var existing = parent != null ? parent.Find(name)?.GetComponent<TextMeshProUGUI>() : null;
            var label = existing ?? new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            if (parent != null && label.transform.parent != parent)
                label.transform.SetParent(parent, false);
            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.92f, 0.88f, 0.78f, 1f);
            label.alignment = alignment;
            label.raycastTarget = false;
            label.rectTransform.sizeDelta = size;
            return label;
        }

        private static Image CreateGhost(Transform parent, string name, float size)
        {
            var ghost = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            ghost.transform.SetParent(parent, false);
            var rect = (RectTransform)ghost.transform;
            rect.sizeDelta = new Vector2(size, size);
            var image = ghost.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            ghost.GetComponent<CanvasGroup>().blocksRaycasts = false;
            ghost.transform.SetAsLastSibling();
            return image;
        }

        private static void ConfigureGrid(Transform root, Vector2 cell, Vector2 spacing, int columns)
        {
            foreach (var other in root.GetComponents<LayoutGroup>())
                if (other is not GridLayoutGroup)
                    UnityEngine.Object.DestroyImmediate(other, true);
            var grid = root.GetComponent<GridLayoutGroup>() ?? root.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cell;
            grid.spacing = spacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.padding = new RectOffset(12, 12, 12, 12);
            var fitter = root.GetComponent<ContentSizeFitter>() ?? root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void ConfigureVertical(Transform root, float spacing, RectOffset padding)
        {
            foreach (var other in root.GetComponents<LayoutGroup>())
                if (other is not VerticalLayoutGroup)
                    UnityEngine.Object.DestroyImmediate(other, true);
            var layout = root.GetComponent<VerticalLayoutGroup>() ?? root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = root.GetComponent<ContentSizeFitter>() ?? root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static void BindClose(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            ResetButton(button);
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static void ResetButton(Button button)
        {
            var so = new SerializedObject(button);
            var calls = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls != null)
                calls.arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetText(Transform root, string path, string value)
        {
            var target = FindPath(root, path);
            if (target == null)
                return;
            var tmp = target.GetComponent<TMP_Text>();
            if (tmp != null)
                tmp.text = value;
            var legacy = target.GetComponent<Text>();
            if (legacy != null)
                legacy.text = value;
        }

        private static Transform FindPath(Transform root, string path)
        {
            if (root == null)
                return null;
            var direct = root.Find(path);
            if (direct != null)
                return direct;
            var leaf = path.Contains('/') ? path.Substring(path.LastIndexOf('/') + 1) : path;
            return FindDeep(root, leaf);
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

        private static void Disable(Transform root, string path)
        {
            var target = FindPath(root, path);
            if (target != null)
                target.gameObject.SetActive(false);
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void AnchorCenter(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void AnchorTopRight(RectTransform rect, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = offset;
        }

        private static void AnchorBottomCenter(RectTransform rect, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = offset;
        }
    }
}
#endif
