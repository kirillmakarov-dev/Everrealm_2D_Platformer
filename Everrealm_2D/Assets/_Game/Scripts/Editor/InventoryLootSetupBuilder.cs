#if UNITY_EDITOR
using System.IO;
using LetterHunter.Characters;
using LetterHunter.Debugging;
using LetterHunter.Economy;
using LetterHunter.Items;
using LetterHunter.Loot;
using LetterHunter.UI.Inventory;
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
    public static class InventoryLootSetupBuilder
    {
        private const string ItemFolder = "Assets/_Game/Data/Items";
        private const string LootFolder = "Assets/_Game/Data/LootTables";
        private const string IconFolder = "Assets/_Game/Data/UI/Inventory";
        private const string PrefabFolder = "Assets/_Game/Prefabs/Loot";
        private const string UiPrefabFolder = "Assets/_Game/Prefabs/UI";
        private const string CoinPickupPath = PrefabFolder + "/CoinPickup.prefab";
        private const string ItemPickupPath = PrefabFolder + "/ItemPickup.prefab";
        private const string InventorySlotPrefabPath = UiPrefabFolder + "/InventorySlot.prefab";

        public static void SetupCurrentScene()
        {
            SetupScene(SceneManager.GetActiveScene());
        }

        public static void SetupInventoryUiOnly()
        {
            var scene = SceneManager.GetActiveScene();
            EnsureFolders();
            var coinSprite = EnsureIcon("CoinIcon", new Color(1f, 0.78f, 0.2f), new Color(0.65f, 0.36f, 0.05f), IconShape.Circle);
            var canvas = EnsureCanvas(scene);
            EnsureCurrencyHud(canvas, scene, coinSprite);
            EnsureInventoryWindow(canvas, scene, coinSprite);
            DisableLegacyInventoryList(canvas);
            EnsureEventSystem(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Inventory UI setup complete. Existing inventory window layout is preserved if one is already present.");
        }

        public static void BuildPlaytestScene()
        {
            const string sourceScenePath = "Assets/_Game/Scenes/CharacterFrameworkDebug.unity";
            const string targetScenePath = "Assets/_Game/Scenes/InventoryLootDebug.unity";

            if (!File.Exists(sourceScenePath))
            {
                Debug.LogError($"Cannot build inventory loot playtest scene. Missing source scene: {sourceScenePath}");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Single);
            SetupScene(scene);
            EditorSceneManager.SaveScene(scene, targetScenePath);
            Debug.Log($"Inventory loot playtest scene created at {targetScenePath}.");
        }

        public static void SetupScene(Scene scene)
        {
            EnsureFolders();
            var coinSprite = EnsureIcon("CoinIcon", new Color(1f, 0.78f, 0.2f), new Color(0.65f, 0.36f, 0.05f), IconShape.Circle);
            var shardSprite = EnsureIcon("TrainingShardIcon", new Color(0.35f, 0.95f, 1f), new Color(0.02f, 0.28f, 0.45f), IconShape.Diamond);
            var swordSprite = EnsureIcon("RustySwordIcon", new Color(0.78f, 0.82f, 0.88f), new Color(0.35f, 0.22f, 0.12f), IconShape.Sword);

            var shard = EnsureItem("TrainingShard", "training_shard", "Training Shard",
                "A simple material used for early upgrades.", ItemType.Material, ItemRarity.Common, 20, 4, shardSprite);
            var sword = EnsureItem("RustySword", "rusty_sword", "Rusty Sword",
                "A rough practice weapon that can be sold.", ItemType.Weapon, ItemRarity.Uncommon, 1, 18, swordSprite);
            var lootTable = EnsureDummyLootTable(shard, sword);

            var coinPickup = EnsurePickupPrefab(CoinPickupPath, "CoinPickup", coinSprite, LootPickupKind.Coins);
            var itemPickup = EnsurePickupPrefab(ItemPickupPath, "ItemPickup", shardSprite, LootPickupKind.ItemStack);

            var dropService = EnsureDropService(scene, coinPickup, itemPickup);
            SetupPlayers(scene);
            SetupEnemies(scene, lootTable, dropService);
            SetupSnailPrefab(lootTable);
            SetupHud(scene, coinSprite);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Inventory, loot, and economy setup complete. Press Play, defeat an enemy, then walk over the drops.");
        }

        private static ItemDefinition EnsureItem(string assetName, string id, string displayName, string description,
            ItemType type, ItemRarity rarity, int maxStack, int sellPrice, Sprite icon)
        {
            var item = LoadOrCreate<ItemDefinition>($"{ItemFolder}/{assetName}.asset");
            var so = new SerializedObject(item);
            Set(so, "itemId", id);
            Set(so, "displayName", displayName);
            Set(so, "description", description);
            Set(so, "itemType", (int)type);
            Set(so, "rarity", (int)rarity);
            Set(so, "maxStack", maxStack);
            Set(so, "sellPrice", sellPrice);
            Set(so, "canSell", true);
            so.FindProperty("icon").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        private static LootTable EnsureDummyLootTable(ItemDefinition shard, ItemDefinition sword)
        {
            var table = LoadOrCreate<LootTable>($"{LootFolder}/DummyEnemyLootTable.asset");
            var so = new SerializedObject(table);
            Set(so, "lootTableId", "dummy_enemy_loot");
            Set(so, "coinDropChance", 1f);
            Set(so, "coinMin", 3);
            Set(so, "coinMax", 8);

            var drops = so.FindProperty("drops");
            drops.arraySize = 2;
            ConfigureDrop(drops.GetArrayElementAtIndex(0), shard, 0.65f, 1, 2);
            ConfigureDrop(drops.GetArrayElementAtIndex(1), sword, 0.15f, 1, 1);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(table);
            return table;
        }

        private static LootPickup2D EnsurePickupPrefab(string path, string name, Sprite sprite, LootPickupKind kind)
        {
            var existing = AssetDatabase.LoadAssetAtPath<LootPickup2D>(path);
            if (existing != null)
                return existing;

            var root = new GameObject(name);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.28f;

            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.simulated = true;

            var pickup = root.AddComponent<LootPickup2D>();
            var so = new SerializedObject(pickup);
            Set(so, "pickupKind", (int)kind);
            Set(so, "destroyAfterCollect", true);
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LootPickup2D>();
        }

        private static LootDropService EnsureDropService(Scene scene, LootPickup2D coinPickup, LootPickup2D itemPickup)
        {
            var service = FindInScene<LootDropService>(scene);
            if (service == null)
            {
                var go = new GameObject("LootDropService");
                if (scene.IsValid())
                    SceneManager.MoveGameObjectToScene(go, scene);
                service = go.AddComponent<LootDropService>();
            }

            var so = new SerializedObject(service);
            so.FindProperty("coinPickupPrefab").objectReferenceValue = coinPickup;
            so.FindProperty("itemPickupPrefab").objectReferenceValue = itemPickup;
            Set(so, "scatterRadius", 0.55f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(service);
            return service;
        }

        private static void SetupPlayers(Scene scene)
        {
            foreach (var player in FindAllInScene<PlayerClassController>(scene))
            {
                if (player.GetComponent<PlayerInventory>() == null)
                    player.gameObject.AddComponent<PlayerInventory>();
                if (player.GetComponent<CurrencyWallet>() == null)
                    player.gameObject.AddComponent<CurrencyWallet>();
                EditorUtility.SetDirty(player.gameObject);
            }
        }

        private static void SetupEnemies(Scene scene, LootTable lootTable, LootDropService dropService)
        {
            foreach (var enemy in FindAllInScene<DummyEnemy2D>(scene))
                AssignMonsterLoot(enemy.gameObject, lootTable, dropService);
        }

        private static void SetupSnailPrefab(LootTable lootTable)
        {
            const string path = "Assets/_Game/Prefabs/Snail.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return;

            var prefab = PrefabUtility.LoadPrefabContents(path);
            var monsterLoot = prefab.GetComponent<MonsterLoot>();
            if (monsterLoot == null)
                monsterLoot = prefab.AddComponent<MonsterLoot>();

            var so = new SerializedObject(monsterLoot);
            so.FindProperty("lootTable").objectReferenceValue = lootTable;
            Set(so, "logDrops", true);
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        private static void AssignMonsterLoot(GameObject enemy, LootTable lootTable, LootDropService dropService)
        {
            var monsterLoot = enemy.GetComponent<MonsterLoot>();
            if (monsterLoot == null)
                monsterLoot = enemy.AddComponent<MonsterLoot>();

            var so = new SerializedObject(monsterLoot);
            so.FindProperty("lootTable").objectReferenceValue = lootTable;
            so.FindProperty("dropService").objectReferenceValue = dropService;
            so.FindProperty("dropPoint").objectReferenceValue = enemy.transform;
            Set(so, "logDrops", true);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(monsterLoot);
        }

        private static void SetupHud(Scene scene, Sprite coinSprite)
        {
            var canvas = EnsureCanvas(scene);
            EnsureCurrencyHud(canvas, scene, coinSprite);
            EnsureInventoryWindow(canvas, scene, coinSprite);
            DisableLegacyInventoryList(canvas);
            EnsureEventSystem(scene);
        }

        private static Canvas EnsureCanvas(Scene scene)
        {
            var existing = FindInScene<Canvas>(scene);
            if (existing != null)
                return existing;

            var canvasObject = new GameObject("InventoryLootCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (scene.IsValid())
                SceneManager.MoveGameObjectToScene(canvasObject, scene);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void EnsureCurrencyHud(Canvas canvas, Scene scene, Sprite coinSprite)
        {
            var existing = canvas.GetComponentInChildren<CurrencyHudPresenter>(true);
            if (existing != null)
            {
                AssignCurrencyHud(existing, scene, coinSprite);
                return;
            }

            var root = CreateUIObject("CurrencyHud", new Vector2(180f, 56f));
            root.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -24f);

            var background = root.AddComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.07f, 0.82f);

            var icon = CreateChildImage(root.transform, "CoinIcon", new Vector2(34f, 34f), new Vector2(28f, 0f));
            icon.sprite = coinSprite;
            icon.preserveAspect = true;

            var text = CreateChildText(root.transform, "GoldText", new Vector2(110f, 40f), new Vector2(82f, 0f), "0", 24f);
            text.alignment = TextAlignmentOptions.Left;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 0.9f, 0.45f);

            var presenter = root.AddComponent<CurrencyHudPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("goldText").objectReferenceValue = text;
            so.FindProperty("coinIcon").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssignCurrencyHud(presenter, scene, coinSprite);
        }

        private static void AssignCurrencyHud(CurrencyHudPresenter presenter, Scene scene, Sprite coinSprite)
        {
            var so = new SerializedObject(presenter);
            so.FindProperty("wallet").objectReferenceValue = FindInScene<CurrencyWallet>(scene);
            if (coinSprite != null && so.FindProperty("coinIcon").objectReferenceValue is Image icon)
                icon.sprite = coinSprite;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }

        private static void EnsureInventoryHud(Canvas canvas, Scene scene)
        {
            var existing = canvas.GetComponentInChildren<InventoryHudPresenter>(true);
            if (existing != null)
            {
                AssignInventoryHud(existing, scene);
                return;
            }

            var root = CreateUIObject("InventoryHud", new Vector2(280f, 260f));
            root.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -24f);

            var background = root.AddComponent<Image>();
            background.color = new Color(0.04f, 0.05f, 0.07f, 0.78f);

            var title = CreateChildText(root.transform, "InventoryTitle", new Vector2(240f, 32f), new Vector2(0f, 100f), "Inventory", 22f);
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            var list = CreateChildText(root.transform, "InventoryList", new Vector2(240f, 180f), new Vector2(0f, -10f), "Empty", 17f);
            list.alignment = TextAlignmentOptions.TopLeft;
            list.color = new Color(0.92f, 0.96f, 1f);

            var presenter = root.AddComponent<InventoryHudPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("inventoryText").objectReferenceValue = list;
            Set(so, "maxVisibleSlots", 8);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssignInventoryHud(presenter, scene);
        }

        private static InventorySlotView EnsureInventorySlotPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<InventorySlotView>(InventorySlotPrefabPath);
            if (existing != null)
                return existing;

            var root = CreateUIObject("InventorySlot", new Vector2(68f, 68f));
            var background = root.AddComponent<Image>();
            background.color = new Color(0.08f, 0.09f, 0.11f, 0.94f);

            var icon = CreateChildImage(root.transform, "Icon", new Vector2(50f, 50f), Vector2.zero);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var amount = CreateChildText(root.transform, "AmountText", new Vector2(42f, 20f), new Vector2(18f, -22f), string.Empty, 15f);
            amount.alignment = TextAlignmentOptions.BottomRight;
            amount.fontStyle = FontStyles.Bold;
            amount.color = Color.white;

            var highlight = CreateChildImage(root.transform, "Highlight", new Vector2(68f, 68f), Vector2.zero);
            highlight.color = new Color(1f, 0.85f, 0.35f, 0.25f);
            highlight.raycastTarget = false;
            highlight.enabled = false;

            var view = root.AddComponent<InventorySlotView>();
            var so = new SerializedObject(view);
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("icon").objectReferenceValue = icon;
            so.FindProperty("amountText").objectReferenceValue = amount;
            so.FindProperty("highlight").objectReferenceValue = highlight;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, InventorySlotPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<InventorySlotView>();
        }

        private static void EnsureInventoryWindow(Canvas canvas, Scene scene, Sprite coinSprite)
        {
            var existing = canvas.GetComponentInChildren<InventoryWindowPresenter>(true);
            var slotPrefab = EnsureInventorySlotPrefab();
            if (existing != null)
            {
                AssignInventoryWindow(existing, scene, slotPrefab);
                return;
            }

            var window = CreateUIObject("InventoryWindow", new Vector2(430f, 560f));
            window.transform.SetParent(canvas.transform, false);
            var windowRect = (RectTransform)window.transform;
            windowRect.anchorMin = new Vector2(0f, 0.5f);
            windowRect.anchorMax = new Vector2(0f, 0.5f);
            windowRect.pivot = new Vector2(0f, 0.5f);
            windowRect.anchoredPosition = new Vector2(24f, 0f);

            var background = window.AddComponent<Image>();
            background.color = new Color(0.035f, 0.04f, 0.052f, 0.94f);
            var group = window.AddComponent<CanvasGroup>();

            var title = CreateChildText(window.transform, "Title", new Vector2(360f, 38f), new Vector2(0f, 234f), "Inventory", 28f);
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            var hint = CreateChildText(window.transform, "Hint", new Vector2(370f, 28f), new Vector2(0f, -238f), "Drag to move / merge. Right click to split.", 14f);
            hint.alignment = TextAlignmentOptions.Center;
            hint.color = new Color(0.68f, 0.75f, 0.82f);

            var goldRow = CreateUIObject("GoldRow", new Vector2(350f, 42f));
            goldRow.transform.SetParent(window.transform, false);
            ((RectTransform)goldRow.transform).anchoredPosition = new Vector2(0f, 192f);
            var goldBack = goldRow.AddComponent<Image>();
            goldBack.color = new Color(0.09f, 0.095f, 0.11f, 0.88f);
            var coin = CreateChildImage(goldRow.transform, "CoinIcon", new Vector2(28f, 28f), new Vector2(-135f, 0f));
            coin.sprite = coinSprite;
            coin.preserveAspect = true;
            var goldText = CreateChildText(goldRow.transform, "GoldText", new Vector2(160f, 32f), new Vector2(-30f, 0f), "0", 21f);
            goldText.alignment = TextAlignmentOptions.Left;
            goldText.fontStyle = FontStyles.Bold;
            goldText.color = new Color(1f, 0.9f, 0.45f);
            var capacityText = CreateChildText(goldRow.transform, "CapacityText", new Vector2(80f, 32f), new Vector2(125f, 0f), "0/24", 17f);
            capacityText.alignment = TextAlignmentOptions.Right;
            capacityText.color = new Color(0.85f, 0.9f, 0.95f);

            var grid = CreateUIObject("SlotGrid", new Vector2(350f, 350f));
            grid.transform.SetParent(window.transform, false);
            ((RectTransform)grid.transform).anchoredPosition = new Vector2(0f, -20f);
            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(62f, 62f);
            layout.spacing = new Vector2(8f, 8f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 5;
            layout.childAlignment = TextAnchor.UpperCenter;

            var presenter = window.AddComponent<InventoryWindowPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("windowGroup").objectReferenceValue = group;
            so.FindProperty("slotRoot").objectReferenceValue = grid.transform;
            so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
            so.FindProperty("goldText").objectReferenceValue = goldText;
            so.FindProperty("capacityText").objectReferenceValue = capacityText;
            Set(so, "startVisible", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssignInventoryWindow(presenter, scene, slotPrefab);
        }

        private static void AssignInventoryWindow(InventoryWindowPresenter presenter, Scene scene, InventorySlotView slotPrefab)
        {
            var so = new SerializedObject(presenter);
            so.FindProperty("inventory").objectReferenceValue = FindInScene<PlayerInventory>(scene);
            so.FindProperty("wallet").objectReferenceValue = FindInScene<CurrencyWallet>(scene);
            so.FindProperty("slotPrefab").objectReferenceValue = slotPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }

        private static void DisableLegacyInventoryList(Canvas canvas)
        {
            var legacy = canvas.GetComponentInChildren<InventoryHudPresenter>(true);
            if (legacy == null)
                return;

            Undo.DestroyObjectImmediate(legacy.gameObject);
        }

        private static void AssignInventoryHud(InventoryHudPresenter presenter, Scene scene)
        {
            var so = new SerializedObject(presenter);
            so.FindProperty("inventory").objectReferenceValue = FindInScene<PlayerInventory>(scene);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }

        private static Sprite EnsureIcon(string name, Color primary, Color secondary, IconShape shape)
        {
            var path = $"{IconFolder}/{name}.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (var y = 0; y < texture.height; y++)
                for (var x = 0; x < texture.width; x++)
                    texture.SetPixel(x, y, PixelForIcon(x, y, primary, secondary, shape));

                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 64f;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color PixelForIcon(int x, int y, Color primary, Color secondary, IconShape shape)
        {
            var p = new Vector2((x + 0.5f) / 64f, (y + 0.5f) / 64f);
            var centered = p - new Vector2(0.5f, 0.5f);
            var distance = centered.magnitude;
            var clear = new Color(0f, 0f, 0f, 0f);

            return shape switch
            {
                IconShape.Circle => distance <= 0.43f ? Color.Lerp(primary, secondary, distance / 0.43f) : clear,
                IconShape.Diamond => Mathf.Abs(centered.x) + Mathf.Abs(centered.y) <= 0.46f
                    ? Color.Lerp(primary, secondary, Mathf.Abs(centered.y) * 2f)
                    : clear,
                IconShape.Sword => SwordPixel(centered, primary, secondary),
                _ => primary
            };
        }

        private static Color SwordPixel(Vector2 centered, Color primary, Color secondary)
        {
            var blade = Mathf.Abs(centered.x) < 0.055f && centered.y > -0.24f && centered.y < 0.38f;
            var point = Mathf.Abs(centered.x) < 0.16f * (0.48f - centered.y) && centered.y >= 0.28f && centered.y < 0.48f;
            var guard = Mathf.Abs(centered.x) < 0.28f && centered.y > -0.28f && centered.y < -0.18f;
            var handle = Mathf.Abs(centered.x) < 0.07f && centered.y > -0.48f && centered.y <= -0.24f;
            if (blade || point)
                return primary;
            if (guard || handle)
                return secondary;
            return new Color(0f, 0f, 0f, 0f);
        }

        private static void ConfigureDrop(SerializedProperty property, ItemDefinition item, float chance, int min, int max)
        {
            property.FindPropertyRelative("item").objectReferenceValue = item;
            property.FindPropertyRelative("dropChance").floatValue = chance;
            property.FindPropertyRelative("minAmount").intValue = min;
            property.FindPropertyRelative("maxAmount").intValue = max;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            EnsureAssetFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolders()
        {
            EnsureAssetFolder(ItemFolder);
            EnsureAssetFolder(LootFolder);
            EnsureAssetFolder(IconFolder);
            EnsureAssetFolder(PrefabFolder);
            EnsureAssetFolder(UiPrefabFolder);
        }

        private static void EnsureAssetFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void EnsureEventSystem(Scene scene)
        {
            if (FindInScene<EventSystem>(scene) != null)
                return;

            var eventObject = new GameObject("EventSystem", typeof(EventSystem));
            if (scene.IsValid())
                SceneManager.MoveGameObjectToScene(eventObject, scene);

#if ENABLE_INPUT_SYSTEM
            eventObject.AddComponent<InputSystemUIInputModule>();
#else
            eventObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private static GameObject CreateUIObject(string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = size;
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
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        private static T FindInScene<T>(Scene scene) where T : Object
        {
            foreach (var item in FindAllInScene<T>(scene))
                return item;
            return null;
        }

        private static T[] FindAllInScene<T>(Scene scene) where T : Object
        {
            var all = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            if (!scene.IsValid())
                return all;

            var list = new System.Collections.Generic.List<T>();
            foreach (var item in all)
            {
                if (item is Component component && component.gameObject.scene == scene)
                    list.Add(item);
                else if (item is GameObject go && go.scene == scene)
                    list.Add(item);
            }

            return list.ToArray();
        }

        private static void Set(SerializedObject so, string name, bool value) => so.FindProperty(name).boolValue = value;
        private static void Set(SerializedObject so, string name, int value) => so.FindProperty(name).intValue = value;
        private static void Set(SerializedObject so, string name, float value) => so.FindProperty(name).floatValue = value;
        private static void Set(SerializedObject so, string name, string value) => so.FindProperty(name).stringValue = value;

        private enum IconShape { Circle, Diamond, Sword }
    }
}
#endif
