#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LetterHunter.Items;
using LetterHunter.UI.Hud;
using LetterHunter.UI.Inventory;
using LetterHunter.UI.SkillTree;
using LetterHunter.UI.Skills;
using LetterHunter.UI.Shop;
using TMPro;
using UnityEditor;
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
            StylePanel(root, "bg2");
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                var name = image.name.ToLowerInvariant();
                if (name.Contains("row") || name.Contains("background"))
                    SetImage(image, "field1", Color.white, true);
            }
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
            grid.cellSize = new Vector2(78f, 78f);
            grid.spacing = new Vector2(8f, 8f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(capacity)), 4, 8);
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
            StylePanel(presenter.transform, "bg2");
            AssignCurrencySprites(presenter.transform);
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
            StylePanel(presenter.transform, "bg2");
            AssignCurrencySprites(presenter.transform);
        }

        private static void SkinInventorySlot(Transform root)
        {
            SetImage(root.GetComponent<Image>(), "item", Color.white, true);
            SetImage(FindDeep(root, "Highlight")?.GetComponent<Image>(), "item_frame", Gold, true);
            AddShadow(root.gameObject, 3f);
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
            SetImage(root.GetComponent<Image>(), "field1", Color.white, true);
            StyleButtons(root);
            StyleText(root);
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
            }
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
