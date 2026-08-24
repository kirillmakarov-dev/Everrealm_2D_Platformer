#if UNITY_EDITOR
using System.Collections.Generic;
using LetterHunter.Items;
using LetterHunter.UI.Hud;
using LetterHunter.UI.Inventory;
using LetterHunter.UI.SkillTree;
using LetterHunter.UI.Skills;
using LetterHunter.UI.Shop;
using SoftKitty;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace LetterHunter.EditorTools
{
    /// <summary>
    /// Builds the authored UI layer once in the editor. Runtime presenters only render
    /// into these serialized objects; they never create controls or visual objects.
    /// The palette and framing follow the local Soft Kitty UI package.
    /// </summary>
    public static class SoftKittyUiPolishBuilder
    {
        private static readonly Color Ink = new(0.035f, 0.045f, 0.075f, 0.97f);
        private static readonly Color Panel = new(0.075f, 0.095f, 0.145f, 0.98f);
        private static readonly Color PanelSoft = new(0.11f, 0.135f, 0.2f, 0.96f);
        private static readonly Color Gold = new(0.95f, 0.72f, 0.28f, 1f);
        private static readonly Color Text = new(0.92f, 0.95f, 1f, 1f);

        [MenuItem("Letter Hunter/Polish UI With Soft Kitty")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid())
                return;

            EnsureSoftKittySettings();
            RemoveLegacyInventoryHud();
            foreach (var vitals in Object.FindObjectsByType<PlayerVitalsHudPresenter>(FindObjectsSortMode.None))
                PolishVitals(vitals.transform);
            foreach (var stats in Object.FindObjectsByType<PlayerStatsPanelPresenter>(FindObjectsSortMode.None))
                PolishStats(stats.transform);
            foreach (var inventory in Object.FindObjectsByType<InventoryWindowPresenter>(FindObjectsSortMode.None))
                BuildInventory(inventory);
            foreach (var skillTree in Object.FindObjectsByType<SkillTreeWindowPresenter>(FindObjectsSortMode.None))
                BuildSkillTree(skillTree);
            foreach (var skillBar in Object.FindObjectsByType<SkillBarPresenter>(FindObjectsSortMode.None))
                BuildSkillBar(skillBar);
            foreach (var shop in Resources.FindObjectsOfTypeAll<ShopWindowPresenter>())
            {
                if (shop != null && shop.gameObject.scene.path == scene.path)
                    BuildShop(shop);
            }
            var authoredShop = GameObject.Find("ShopWindow")?.GetComponent<ShopWindowPresenter>();
            if (authoredShop != null)
                BuildShop(authoredShop);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Soft Kitty UI polish complete. All runtime UI references are scene-authored.");
        }

        private static void EnsureSoftKittySettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SGD_Settings>(
                "Assets/SoftKitty/Data/SGD_Settings.asset");
            if (settings == null)
                return;

            EditorBuildSettings.AddConfigObject(SGD_Settings.CONFIG_NAME, settings, true);
            Debug.Log("Soft Kitty UI: registered SGD_Settings in Editor Build Settings.");
        }

        private static void RemoveLegacyInventoryHud()
        {
            foreach (var legacy in Resources.FindObjectsOfTypeAll<InventoryHudPresenter>())
            {
                if (legacy == null || !legacy.gameObject.scene.IsValid())
                    continue;
                Debug.Log($"Soft Kitty UI: removing legacy runtime inventory HUD '{legacy.name}'.");
                Undo.DestroyObjectImmediate(legacy.gameObject);
            }
        }

        private static void PolishVitals(Transform root)
        {
            foreach (var orb in root.GetComponentsInChildren<VitalsOrbView>(true))
            {
                var rect = orb.transform as RectTransform;
                if (rect != null)
                    rect.sizeDelta = new Vector2(128f, 128f);
                var image = orb.GetComponent<Image>();
                if (image != null)
                    image.color = Panel;
                AddShadow(orb.gameObject, new Color(0f, 0f, 0f, 0.55f), 10f);
            }

            StyleHierarchy(root, true);
        }

        private static void PolishStats(Transform root)
        {
            EnsurePanelChrome(root, "StatsPanelAccent", new Vector2(5f, 250f), new Vector2(10f, 0f));
            StyleHierarchy(root, true);
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
            {
                var inventorySo = new SerializedObject(inventory);
                capacity = Mathf.Max(1, inventorySo.FindProperty("capacity").intValue);
            }

            var slotPrefab = AssetDatabase.LoadAssetAtPath<InventorySlotView>(
                "Assets/_Game/Prefabs/UI/InventorySlot.prefab");
            if (slotPrefab == null)
                return;

            var grid = slotRoot.GetComponent<GridLayoutGroup>() ?? slotRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(74f, 74f);
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(capacity)), 4, 8);

            var slots = so.FindProperty("slotViews");
            slots.arraySize = capacity;
            for (var i = 0; i < capacity; i++)
            {
                var slot = FindOrCreateChild(slotRoot, $"Slot_{i + 1:00}", slotPrefab);
                slot.gameObject.SetActive(true);
                slots.GetArrayElementAtIndex(i).objectReferenceValue = slot;
                PolishSlot(slot.transform);
            }

            var dragGhost = presenter.transform.Find("DraggedItemIcon");
            if (dragGhost == null)
            {
                var ghost = new GameObject("DraggedItemIcon", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(CanvasGroup));
                ghost.transform.SetParent(presenter.transform, false);
                dragGhost = ghost.transform;
                var rect = (RectTransform)dragGhost;
                rect.sizeDelta = new Vector2(72f, 72f);
                var image = ghost.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                ghost.GetComponent<CanvasGroup>().blocksRaycasts = false;
                ghost.SetActive(true);
            }
            dragGhost.SetAsLastSibling();
            dragGhost.gameObject.SetActive(true);
            so.FindProperty("dragGhost").objectReferenceValue = dragGhost.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();
            EnsurePanelChrome(presenter.transform, "InventoryAccent", new Vector2(5f, 360f), new Vector2(10f, 0f));
            StyleHierarchy(presenter.transform, true);
        }

        private static void BuildSkillTree(SkillTreeWindowPresenter presenter)
        {
            var so = new SerializedObject(presenter);
            var nodeRoot = so.FindProperty("nodeRoot").objectReferenceValue as Transform;
            var connectionRoot = so.FindProperty("connectionRoot").objectReferenceValue as Transform;
            var controller = so.FindProperty("controller").objectReferenceValue as LetterHunter.SkillTree.PlayerSkillTreeController;
            var tree = controller != null ? controller.SkillTree : null;
            var nodePrefab = AssetDatabase.LoadAssetAtPath<SkillTreeNodeView>(
                "Assets/_Game/Prefabs/UI/SkillTreeNode.prefab");
            var connectionPrefab = AssetDatabase.LoadAssetAtPath<SkillTreeConnectionView>(
                "Assets/_Game/Prefabs/UI/SkillTreeConnection.prefab");
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
                view.gameObject.SetActive(true);
                var viewSo = new SerializedObject(view);
                viewSo.FindProperty("configuredNodeId").stringValue = node.NodeId;
                viewSo.ApplyModifiedPropertiesWithoutUndo();
                nodeViews.GetArrayElementAtIndex(i).objectReferenceValue = view;
                StyleHierarchy(view.transform, false);
            }

            var edgeCount = 0;
            foreach (var node in tree.Nodes)
                if (node != null)
                    edgeCount += node.PrerequisiteNodeIds.Count;

            var connectionViews = so.FindProperty("connectionViews");
            connectionViews.arraySize = edgeCount;
            for (var i = 0; i < edgeCount; i++)
            {
                var connection = FindOrCreateChild(connectionRoot, $"AuthoredConnection_{i + 1:00}", connectionPrefab);
                connection.gameObject.SetActive(true);
                connectionViews.GetArrayElementAtIndex(i).objectReferenceValue = connection;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EnsurePanelChrome(presenter.transform, "SkillTreeAccent", new Vector2(5f, 420f), new Vector2(10f, 0f));
            StyleHierarchy(presenter.transform, true);
        }

        private static void BuildSkillBar(SkillBarPresenter presenter)
        {
            var so = new SerializedObject(presenter);
            var slotRoot = so.FindProperty("slotRoot").objectReferenceValue as Transform ?? presenter.transform;
            var prefab = AssetDatabase.LoadAssetAtPath<SkillSlotView>(
                "Assets/_Game/Prefabs/UI/SkillSlot.prefab");
            if (prefab == null)
                return;

            var maxSlots = Mathf.Max(1, so.FindProperty("maxSlots").intValue);
            var authored = so.FindProperty("authoredSlots");
            authored.arraySize = maxSlots;
            for (var i = 0; i < maxSlots; i++)
            {
                var view = FindOrCreateChild(slotRoot, $"SkillSlot_{i + 1}", prefab);
                authored.GetArrayElementAtIndex(i).objectReferenceValue = view;
                view.gameObject.SetActive(true);
                StyleHierarchy(view.transform, false);
            }

            var ghost = presenter.transform.Find("DraggedSkillIcon");
            if (ghost == null)
            {
                var ghostObject = new GameObject("DraggedSkillIcon", typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image), typeof(CanvasGroup));
                ghostObject.transform.SetParent(presenter.transform, false);
                ghost = ghostObject.transform;
                ((RectTransform)ghost).sizeDelta = new Vector2(64f, 64f);
                ghostObject.GetComponent<Image>().raycastTarget = false;
                ghostObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
            }
            so.FindProperty("assignmentGhost").objectReferenceValue = ghost.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();
            StyleHierarchy(presenter.transform, false);
        }

        private static void BuildShop(ShopWindowPresenter presenter)
        {
            var so = new SerializedObject(presenter);
            var rowRoot = so.FindProperty("rowRoot").objectReferenceValue as Transform;
            var prefab = AssetDatabase.LoadAssetAtPath<ShopItemRowView>(
                "Assets/_Game/Prefabs/UI/ShopItemRow.prefab");
            if (rowRoot == null || prefab == null)
                return;

            var authored = so.FindProperty("authoredRows");
            const int authoredRowCount = 12;
            authored.arraySize = authoredRowCount;
            for (var i = 0; i < authoredRowCount; i++)
            {
                var row = FindOrCreateChild(rowRoot, $"AuthoredShopRow_{i + 1:00}", prefab);
                row.gameObject.SetActive(false);
                authored.GetArrayElementAtIndex(i).objectReferenceValue = row;
                StyleHierarchy(row.transform, false);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            StyleHierarchy(presenter.transform, true);
        }

        private static T FindOrCreateChild<T>(Transform parent, string name, T prefab) where T : Component
        {
            var existing = parent.Find(name)?.GetComponent<T>();
            if (existing != null)
                return existing;

            var created = PrefabUtility.InstantiatePrefab(prefab, parent) as T;
            if (created == null)
                return null;
            created.name = name;
            return created;
        }

        private static void PolishSlot(Transform slot)
        {
            var image = slot.GetComponent<Image>();
            if (image != null)
                image.color = PanelSoft;
            AddShadow(slot.gameObject, new Color(0f, 0f, 0f, 0.45f), 5f);
        }

        private static void EnsurePanelChrome(Transform root, string accentName, Vector2 size, Vector2 position)
        {
            var image = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
            image.color = Panel;
            var outline = root.GetComponent<Outline>() ?? root.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.5f);
            outline.effectDistance = new Vector2(2f, -2f);
            AddShadow(root.gameObject, new Color(0f, 0f, 0f, 0.6f), 14f);

            if (root.Find(accentName) != null)
                return;
            var accent = new GameObject(accentName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accent.transform.SetParent(root, false);
            var rect = (RectTransform)accent.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            accent.GetComponent<Image>().color = Gold;
        }

        private static void StyleHierarchy(Transform root, bool includeRoot)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name.Contains("Fill"))
                    image.color = image.gameObject.name.ToLowerInvariant().Contains("mana")
                        ? new Color(0.25f, 0.55f, 1f, 1f)
                        : new Color(0.95f, 0.25f, 0.28f, 1f);
                else if (image.gameObject.name.Contains("Background") || image.gameObject.name.Contains("Panel"))
                    image.color = PanelSoft;
            }

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.color = Text;
                text.raycastTarget = false;
                if (text.name.Contains("Title") || text.name.Contains("Header"))
                {
                    text.color = Gold;
                    text.fontStyle = FontStyles.Bold;
                }
            }

            if (includeRoot)
                EnsurePanelChrome(root, "SoftKittyAccent", new Vector2(4f, 150f), new Vector2(8f, 0f));
        }

        private static void AddShadow(GameObject target, Color color, float distance)
        {
            var shadow = target.GetComponent<Shadow>() ?? target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = new Vector2(distance, -distance);
            shadow.useGraphicAlpha = true;
        }
    }
}
#endif
