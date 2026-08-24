#if UNITY_EDITOR
using System.IO;
using LetterHunter.Characters;
using LetterHunter.Combat;
using LetterHunter.Core;
using LetterHunter.Economy;
using LetterHunter.Effects;
using LetterHunter.Items;
using LetterHunter.SkillTree;
using LetterHunter.Skills;
using LetterHunter.UI.SkillTree;
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
    public static class SkillTreeSetupBuilder
    {
        private const string TreeFolder = "Assets/_Game/Data/SkillTrees";
        private const string SkillFolder = "Assets/_Game/Data/Skills/SkillTree";
        private const string EffectFolder = "Assets/_Game/Data/Effects/SkillTree";
        private const string UiPrefabFolder = "Assets/_Game/Prefabs/UI";
        private const string NodePrefabPath = UiPrefabFolder + "/SkillTreeNode.prefab";
        private const string ConnectionPrefabPath = UiPrefabFolder + "/SkillTreeConnection.prefab";

        private readonly struct RankEffectSpec
        {
            public RankEffectSpec(SkillTreeRankEffectType type, float amountPerRank,
                SkillDefinition targetSkill = null, int firstAppliedRank = 1)
            {
                Type = type;
                AmountPerRank = amountPerRank;
                TargetSkill = targetSkill;
                FirstAppliedRank = firstAppliedRank;
            }

            public SkillTreeRankEffectType Type { get; }
            public float AmountPerRank { get; }
            public SkillDefinition TargetSkill { get; }
            public int FirstAppliedRank { get; }
        }

        public static void SetupCurrentScene()
        {
            SetupScene(SceneManager.GetActiveScene());
        }

        public static void RebuildDefaultUiPrefabs()
        {
            EnsureFolders();
            BuildNodePrefab();
            BuildConnectionPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Default skill tree UI prefabs rebuilt. Run Letter Hunter/Setup Skill Tree to wire them into the scene.");
        }

        public static void ValidateSkillTrees()
        {
            var errorCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:SkillTreeDefinition"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var tree = AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(path);
                if (tree == null)
                    continue;

                foreach (var error in tree.ValidateDefinition())
                {
                    errorCount++;
                    Debug.LogError($"[Skill Tree Validation] {path}: {error}", tree);
                }
            }

            if (errorCount == 0)
                Debug.Log("Skill tree validation passed. No errors found.");
            else
                Debug.LogError($"Skill tree validation found {errorCount} error(s). See previous console entries.");
        }

        public static void RebuildDemoWarriorSkillTreeData()
        {
            EnsureFolders();
            var shard = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/TrainingShard.asset");
            var demoSkill = EnsureTreeSkill();
            EnsureSkillTree(shard, demoSkill, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Demo Warrior Skill Tree rebuilt with seven validated nodes.");
        }

        public static void BuildPlaytestScene()
        {
            const string sourceScenePath = "Assets/_Game/Scenes/InventoryLootDebug.unity";
            const string targetScenePath = "Assets/_Game/Scenes/SkillTreeDebug.unity";

            if (!File.Exists(sourceScenePath))
            {
                Debug.LogError($"Cannot build skill tree playtest scene. Missing source scene: {sourceScenePath}. Run Letter Hunter/Build Inventory Loot Playtest Scene first.");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(sourceScenePath, OpenSceneMode.Single);
            SetupScene(scene);
            EditorSceneManager.SaveScene(scene, targetScenePath);
            Debug.Log($"Skill tree playtest scene created at {targetScenePath}.");
        }

        public static void SetupScene(Scene scene)
        {
            EnsureFolders();

            var shard = AssetDatabase.LoadAssetAtPath<ItemDefinition>("Assets/_Game/Data/Items/TrainingShard.asset");
            var demoSkill = EnsureTreeSkill();
            var tree = EnsureSkillTree(shard, demoSkill, false);

            SetupPlayers(scene, tree);
            EnsureSkillTreeWindow(scene, tree);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Skill tree setup complete. Press Play and use K to open the skill tree.");
        }

        private static SkillDefinition EnsureTreeSkill()
        {
            var effect = LoadOrCreate<DamageSkillEffectDefinition>($"{EffectFolder}/Warrior_TreeFocusSlash.asset");
            var effectSo = new SerializedObject(effect);
            SetShape(effectSo, "attackShape", AttackShapeType.DirectionalBox, new Vector2(2.6f, 1.2f), 0f, 1.1f);
            Set(effectSo, "damageTags", (int)(DamageTag.Skill | DamageTag.Melee));
            effectSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effect);

            var skill = LoadOrCreate<SkillDefinition>($"{SkillFolder}/TreeFocusSlash.asset");
            var so = new SerializedObject(skill);
            Set(so, "skillId", "tree_focus_slash");
            Set(so, "displayName", "Focus Slash");
            Set(so, "shortName", "Focus");
            Set(so, "inputLabel", "Tree");
            Set(so, "description", "A skill unlocked from the first test skill tree.");
            Set(so, "classType", (int)CharacterClassType.Warrior);
            Set(so, "skillType", (int)SkillType.Active);
            Set(so, "manaCost", 8f);
            Set(so, "cooldown", 4f);
            Set(so, "duration", 0f);
            Set(so, "baseDamageMultiplier", 1.25f);
            Set(so, "damageLines", 2);
            Set(so, "maxTargets", 2);
            Set(so, "rank", 1);
            so.FindProperty("effect").objectReferenceValue = effect;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        private static SkillTreeDefinition EnsureSkillTree(ItemDefinition shard, SkillDefinition demoSkill, bool overwriteNodes)
        {
            var tree = LoadOrCreate<SkillTreeDefinition>($"{TreeFolder}/WarriorSkillTree.asset");
            var so = new SerializedObject(tree);
            Set(so, "treeId", "warrior_skill_tree");
            Set(so, "displayName", "Warrior Skill Tree");

            var focusIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/UI/SkillIcons/Icon_Focus.png");
            var ironIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/UI/SkillIcons/Icon_Iron.png");
            var speedIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/UI/SkillIcons/Icon_Speed.png");
            var starIcon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/UI/SkillIcons/Icon_Star.png");

            var nodes = so.FindProperty("nodes");
            if (!overwriteNodes && nodes.arraySize > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tree);
                return tree;
            }

            so.FindProperty("goldRefundRate").floatValue = 0.75f;
            so.FindProperty("refundMaterials").boolValue = false;
            nodes.arraySize = 11;
            ConfigureNode(nodes.GetArrayElementAtIndex(0), "training_roots", "Attack Basics",
                "Basic attack training and the start of the offensive branch.", 10, null, null,
                SkillTreeUnlockAction.None, null, SkillTreeNodeType.Passive, starIcon, 3,
                new RankEffectSpec(SkillTreeRankEffectType.AttackPower, 2f));
            ConfigureNode(nodes.GetArrayElementAtIndex(1), "defense_basics", "Defense Basics",
                "Basic armor training and the start of the defensive branch.", 10, null, null,
                SkillTreeUnlockAction.None, null, SkillTreeNodeType.Passive, ironIcon, 3,
                new RankEffectSpec(SkillTreeRankEffectType.Defense, 1f));
            ConfigureNode(nodes.GetArrayElementAtIndex(2), "speed_basics", "Attack Speed Basics",
                "Basic speed training and the start of the rapid attack branch.", 10, null, null,
                SkillTreeUnlockAction.None, null, SkillTreeNodeType.Passive, speedIcon, 3,
                new RankEffectSpec(SkillTreeRankEffectType.AttackSpeed, 0.05f));
            ConfigureNode(nodes.GetArrayElementAtIndex(3), "focus_slash", "Focus Slash",
                "Unlocks a new active skill. Requires Training Roots and one Training Shard.", 15,
                shard != null ? new[] { new SkillTreeMaterialCost(shard, 1) } : null,
                new[] { "training_roots" }, SkillTreeUnlockAction.UnlockSkill, demoSkill,
                SkillTreeNodeType.Skill, focusIcon, 2,
                new RankEffectSpec(SkillTreeRankEffectType.SkillDamagePercent, 0.15f, demoSkill, 2));
            ConfigureNode(nodes.GetArrayElementAtIndex(4), "iron_discipline", "Iron Discipline",
                "A defensive branch for surviving stronger monsters.", 12, null,
                new[] { "defense_basics" }, SkillTreeUnlockAction.None, null,
                SkillTreeNodeType.Passive, ironIcon, 3,
                new RankEffectSpec(SkillTreeRankEffectType.Defense, 1.5f));
            ConfigureNode(nodes.GetArrayElementAtIndex(5), "swift_training", "Swift Training",
                "A speed branch for faster basic attacks.", 12, null,
                new[] { "speed_basics" }, SkillTreeUnlockAction.None, null,
                SkillTreeNodeType.Passive, speedIcon, 3,
                new RankEffectSpec(SkillTreeRankEffectType.AttackSpeed, 0.08f),
                new RankEffectSpec(SkillTreeRankEffectType.MoveSpeed, 0.3f));
            ConfigureNode(nodes.GetArrayElementAtIndex(6), "sharpen_focus", "Sharpen Focus",
                "Increase Focus Slash damage with every rank.", 18, null,
                new[] { "focus_slash" }, SkillTreeUnlockAction.None, null,
                SkillTreeNodeType.Upgrade, focusIcon, 3,
                new RankEffectSpec(SkillTreeRankEffectType.SkillDamagePercent, 0.1f, demoSkill));
            ConfigureNode(nodes.GetArrayElementAtIndex(7), "focused_flow", "Focused Flow",
                "Reduce both the cooldown and mana cost of Focus Slash.", 18, null,
                new[] { "focus_slash" }, SkillTreeUnlockAction.None, null,
                SkillTreeNodeType.Upgrade, focusIcon, 3,
                new RankEffectSpec(SkillTreeRankEffectType.SkillCooldownReductionPercent, 0.08f, demoSkill),
                new RankEffectSpec(SkillTreeRankEffectType.SkillManaCostReductionPercent, 0.08f, demoSkill));
            ConfigureNode(nodes.GetArrayElementAtIndex(8), "defense_mastery", "Fortress Training",
                "Advance the defensive branch with stronger armor training.", 18, null,
                new[] { "iron_discipline" }, SkillTreeUnlockAction.None, null,
                SkillTreeNodeType.Upgrade, ironIcon, 3,
                new RankEffectSpec(SkillTreeRankEffectType.Defense, 2.5f));
            ConfigureNode(nodes.GetArrayElementAtIndex(9), "rapid_assault", "Rapid Assault",
                "Advance the speed branch with increasingly rapid attacks.", 18, null,
                new[] { "swift_training" }, SkillTreeUnlockAction.None, null,
                SkillTreeNodeType.Upgrade, speedIcon, 3,
                new RankEffectSpec(SkillTreeRankEffectType.AttackSpeed, 0.12f),
                new RankEffectSpec(SkillTreeRankEffectType.JumpHeight, 0.15f));
            ConfigureNode(nodes.GetArrayElementAtIndex(10), "battle_mastery", "Battle Mastery",
                "A capstone unlocked after completing all three branches.", 30, null,
                new[] { "sharpen_focus", "focused_flow", "defense_mastery", "rapid_assault" },
                SkillTreeUnlockAction.None, null, SkillTreeNodeType.Passive, starIcon, 1,
                new RankEffectSpec(SkillTreeRankEffectType.AttackPower, 5f),
                new RankEffectSpec(SkillTreeRankEffectType.Defense, 3f));

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tree);
            return tree;
        }

        private static SkillTreeNodeView EnsureNodePrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SkillTreeNodeView>(NodePrefabPath);
            if (existing != null)
                return existing;

            return BuildNodePrefab();
        }

        private static SkillTreeNodeView BuildNodePrefab()
        {
            var root = CreateUIObject("SkillTreeNode", new Vector2(350f, 160f));
            var background = root.AddComponent<Image>();
            background.color = new Color(0.12f, 0.13f, 0.16f, 0.95f);
            background.raycastTarget = false;

            var iconObject = CreateUIObject("Icon", new Vector2(42f, 42f));
            iconObject.transform.SetParent(root.transform, false);
            ((RectTransform)iconObject.transform).anchoredPosition = new Vector2(-145f, 52f);
            var icon = iconObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var title = CreateChildText(root.transform, "Title", new Vector2(190f, 28f), new Vector2(-20f, 60f), "Node", 20f);
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            var rank = CreateChildText(root.transform, "Rank", new Vector2(90f, 24f), new Vector2(120f, 60f), "Rank 0/1", 14f);
            rank.alignment = TextAlignmentOptions.Right;
            rank.fontStyle = FontStyles.Bold;
            rank.color = new Color(0.65f, 0.85f, 1f);

            var type = CreateChildText(root.transform, "Type", new Vector2(110f, 20f), new Vector2(-72f, 39f), "PASSIVE", 11f);
            type.alignment = TextAlignmentOptions.Left;
            type.fontStyle = FontStyles.Bold;
            type.color = new Color(0.55f, 0.7f, 0.9f);

            var description = CreateChildText(root.transform, "Description", new Vector2(240f, 24f), new Vector2(-18f, 20f), "Description", 13f);
            description.alignment = TextAlignmentOptions.Center;
            description.color = new Color(0.82f, 0.88f, 0.96f);

            var effect = CreateChildText(root.transform, "Effect", new Vector2(220f, 24f),
                new Vector2(-28f, -6f), "Upgrade effect", 13f);
            effect.alignment = TextAlignmentOptions.Center;
            effect.fontStyle = FontStyles.Bold;
            effect.color = new Color(0.45f, 0.9f, 0.55f);

            var cost = CreateChildText(root.transform, "Cost", new Vector2(160f, 22f), new Vector2(-80f, -28f), "Cost", 14f);
            cost.alignment = TextAlignmentOptions.Left;
            cost.color = new Color(1f, 0.88f, 0.42f);

            var state = CreateChildText(root.transform, "State", new Vector2(90f, 22f), new Vector2(34f, -28f), "Locked", 14f);
            state.alignment = TextAlignmentOptions.Right;
            state.fontStyle = FontStyles.Bold;
            state.color = Color.white;

            var selectButton = CreateButton(root.transform, "SelectButton", new Vector2(82f, 28f),
                new Vector2(124f, -28f), "Select");
            var selectText = selectButton.GetComponentInChildren<TextMeshProUGUI>();
            var upgradeButton = CreateButton(root.transform, "UpgradeButton", new Vector2(82f, 28f),
                new Vector2(124f, 8f), "Unlock");
            var upgradeText = upgradeButton.GetComponentInChildren<TextMeshProUGUI>();
            var refundButton = CreateButton(root.transform, "RefundButton", new Vector2(82f, 24f),
                new Vector2(124f, -61f), "Refund");
            var refundText = refundButton.GetComponentInChildren<TextMeshProUGUI>();

            var view = root.AddComponent<SkillTreeNodeView>();
            var so = new SerializedObject(view);
            so.FindProperty("upgradeButton").objectReferenceValue = upgradeButton;
            so.FindProperty("background").objectReferenceValue = background;
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("typeText").objectReferenceValue = type;
            so.FindProperty("descriptionText").objectReferenceValue = description;
            so.FindProperty("effectText").objectReferenceValue = effect;
            so.FindProperty("costText").objectReferenceValue = cost;
            so.FindProperty("stateText").objectReferenceValue = state;
            so.FindProperty("rankText").objectReferenceValue = rank;
            so.FindProperty("upgradeButtonText").objectReferenceValue = upgradeText;
            so.FindProperty("selectButton").objectReferenceValue = selectButton;
            so.FindProperty("refundButton").objectReferenceValue = refundButton;
            so.FindProperty("selectButtonText").objectReferenceValue = selectText;
            so.FindProperty("refundButtonText").objectReferenceValue = refundText;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, NodePrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<SkillTreeNodeView>();
        }

        private static SkillTreeConnectionView EnsureConnectionPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SkillTreeConnectionView>(ConnectionPrefabPath);
            return existing != null ? existing : BuildConnectionPrefab();
        }

        private static SkillTreeConnectionView BuildConnectionPrefab()
        {
            var root = CreateUIObject("SkillTreeConnection", new Vector2(100f, 6f));
            var image = root.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = new Color(0.25f, 0.27f, 0.32f, 0.9f);
            var view = root.AddComponent<SkillTreeConnectionView>();
            var so = new SerializedObject(view);
            so.FindProperty("line").objectReferenceValue = root.transform;
            so.FindProperty("image").objectReferenceValue = image;
            so.ApplyModifiedPropertiesWithoutUndo();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ConnectionPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<SkillTreeConnectionView>();
        }

        private static void SetupPlayers(Scene scene, SkillTreeDefinition tree)
        {
            foreach (var player in FindAllInScene<PlayerClassController>(scene))
            {
                var controller = player.GetComponent<PlayerSkillTreeController>();
                if (controller == null)
                    controller = player.gameObject.AddComponent<PlayerSkillTreeController>();

                var so = new SerializedObject(controller);
                so.FindProperty("skillTree").objectReferenceValue = tree;
                so.FindProperty("player").objectReferenceValue = player;
                so.FindProperty("inventory").objectReferenceValue = player.GetComponent<PlayerInventory>();
                so.FindProperty("wallet").objectReferenceValue = player.GetComponent<CurrencyWallet>();
                so.FindProperty("skillBar").objectReferenceValue = FindInScene<LetterHunter.UI.Skills.SkillBarPresenter>(scene);
                so.FindProperty("skillLoadout").objectReferenceValue =
                    player.GetComponent<LetterHunter.UI.Skills.PlayerSkillLoadout>();
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
            }
        }

        private static void EnsureSkillTreeWindow(Scene scene, SkillTreeDefinition tree)
        {
            var canvas = EnsureCanvas(scene);
            var nodePrefab = EnsureNodePrefab();
            var connectionPrefab = EnsureConnectionPrefab();
            var existing = FindInScene<SkillTreeWindowPresenter>(scene);
            if (existing != null)
            {
                if (existing.transform.parent != canvas.transform)
                    existing.transform.SetParent(canvas.transform, false);
                AssignWindow(existing, scene, nodePrefab, connectionPrefab);
                return;
            }

            var window = CreateUIObject("SkillTreeWindow", new Vector2(440f, 430f));
            window.transform.SetParent(canvas.transform, false);
            var rect = (RectTransform)window.transform;
            ConfigureSkillTreeWindowRect(rect);

            var background = window.AddComponent<Image>();
            background.color = new Color(0.035f, 0.04f, 0.055f, 0.95f);
            var group = window.AddComponent<CanvasGroup>();

            var title = CreateChildText(window.transform, "Title", new Vector2(390f, 42f), new Vector2(0f, 170f), tree.DisplayName, 28f);
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;

            Transform nodeRoot = null;
            Transform connectionRoot = null;
            EnsureScrollableTree(window.transform, ref nodeRoot, ref connectionRoot);

            var feedback = CreateChildText(window.transform, "Feedback", new Vector2(380f, 28f), new Vector2(0f, -175f), "Press K to close.", 15f);
            feedback.alignment = TextAlignmentOptions.Center;
            feedback.color = new Color(0.78f, 0.84f, 0.92f);
            var tooltipRoot = EnsureTooltip(window.transform, out var tooltipText);

            var presenter = window.AddComponent<SkillTreeWindowPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("windowGroup").objectReferenceValue = group;
            so.FindProperty("nodeRoot").objectReferenceValue = nodeRoot.transform;
            so.FindProperty("connectionRoot").objectReferenceValue = connectionRoot.transform;
            so.FindProperty("scrollContent").objectReferenceValue = nodeRoot.parent;
            so.FindProperty("nodePrefab").objectReferenceValue = nodePrefab;
            so.FindProperty("connectionPrefab").objectReferenceValue = connectionPrefab;
            so.FindProperty("skillBar").objectReferenceValue = FindInScene<LetterHunter.UI.Skills.SkillBarPresenter>(scene);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("feedbackText").objectReferenceValue = feedback;
            so.FindProperty("tooltipRoot").objectReferenceValue = tooltipRoot;
            so.FindProperty("tooltipText").objectReferenceValue = tooltipText;
            so.FindProperty("startVisible").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssignWindow(presenter, scene, nodePrefab, connectionPrefab);
        }

        private static void AssignWindow(SkillTreeWindowPresenter presenter, Scene scene, SkillTreeNodeView nodePrefab,
            SkillTreeConnectionView connectionPrefab)
        {
            ConfigureSkillTreeWindowRect((RectTransform)presenter.transform);
            var so = new SerializedObject(presenter);
            var nodeRoot = so.FindProperty("nodeRoot").objectReferenceValue as Transform;
            if (nodeRoot == null)
                nodeRoot = presenter.transform.Find("Nodes") ?? presenter.transform.Find("NodeList");
            if (nodeRoot != null)
            {
                nodeRoot.name = "Nodes";
            }

            var connectionRoot = so.FindProperty("connectionRoot").objectReferenceValue as Transform;
            if (connectionRoot == null)
                connectionRoot = presenter.transform.Find("Connections");

            EnsureScrollableTree(presenter.transform, ref nodeRoot, ref connectionRoot);
            var tooltipRoot = EnsureTooltip(presenter.transform, out var tooltipText);
            ConfigureWindowChrome(presenter.transform);

            if (nodeRoot != null)
            {
                connectionRoot.SetAsFirstSibling();
                nodeRoot.SetAsLastSibling();
            }

            so.FindProperty("controller").objectReferenceValue = FindInScene<PlayerSkillTreeController>(scene);
            so.FindProperty("nodeRoot").objectReferenceValue = nodeRoot;
            so.FindProperty("connectionRoot").objectReferenceValue = connectionRoot;
            so.FindProperty("scrollContent").objectReferenceValue = nodeRoot != null ? nodeRoot.parent : null;
            so.FindProperty("nodePrefab").objectReferenceValue = nodePrefab;
            so.FindProperty("connectionPrefab").objectReferenceValue = connectionPrefab;
            so.FindProperty("tooltipRoot").objectReferenceValue = tooltipRoot;
            so.FindProperty("tooltipText").objectReferenceValue = tooltipText;
            so.FindProperty("skillBar").objectReferenceValue = FindInScene<LetterHunter.UI.Skills.SkillBarPresenter>(scene);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(presenter);
        }

        private static GameObject EnsureTooltip(Transform window, out TMP_Text tooltipText)
        {
            var existing = window.Find("Tooltip");
            GameObject root;
            if (existing == null)
            {
                root = CreateUIObject("Tooltip", new Vector2(260f, 210f));
                root.transform.SetParent(window, false);
                ((RectTransform)root.transform).anchoredPosition = new Vector2(370f, 10f);
                var image = root.AddComponent<Image>();
                image.color = new Color(0.055f, 0.065f, 0.085f, 0.98f);
            }

            else
            {
                root = existing.gameObject;
            }

            var tooltipRect = (RectTransform)root.transform;
            tooltipRect.anchorMin = Vector2.one;
            tooltipRect.anchorMax = Vector2.one;
            tooltipRect.pivot = Vector2.one;
            tooltipRect.anchoredPosition = new Vector2(-20f, -78f);

            tooltipText = root.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tooltipText == null)
            {
                tooltipText = CreateChildText(root.transform, "Text", new Vector2(224f, 178f), Vector2.zero,
                    "Skill tree node details", 14f);
                tooltipText.alignment = TextAlignmentOptions.TopLeft;
                tooltipText.textWrappingMode = TextWrappingModes.Normal;
                tooltipText.overflowMode = TextOverflowModes.Ellipsis;
                tooltipText.color = new Color(0.9f, 0.93f, 0.98f);
            }

            root.SetActive(false);
            return root;
        }

        private static void EnsureScrollableTree(Transform window, ref Transform nodeRoot, ref Transform connectionRoot)
        {
            var scrollTransform = window.Find("TreeScroll");
            GameObject scrollObject;
            if (scrollTransform == null)
            {
                scrollObject = CreateUIObject("TreeScroll", new Vector2(390f, 270f));
                scrollObject.transform.SetParent(window, false);
                ((RectTransform)scrollObject.transform).anchoredPosition = new Vector2(0f, 10f);
            }

            else
            {
                scrollObject = scrollTransform.gameObject;
            }

            var scrollObjectRect = (RectTransform)scrollObject.transform;
            scrollObjectRect.anchorMin = Vector2.zero;
            scrollObjectRect.anchorMax = Vector2.one;
            scrollObjectRect.offsetMin = new Vector2(20f, 56f);
            scrollObjectRect.offsetMax = new Vector2(-20f, -72f);

            var scrollRect = scrollObject.GetComponent<ScrollRect>();
            if (scrollRect == null)
                scrollRect = scrollObject.AddComponent<ScrollRect>();

            var viewport = scrollObject.transform.Find("Viewport") as RectTransform;
            if (viewport == null)
            {
                var viewportObject = CreateUIObject("Viewport", Vector2.zero);
                viewportObject.transform.SetParent(scrollObject.transform, false);
                viewport = (RectTransform)viewportObject.transform;
                StretchToParent(viewport);
                var viewportImage = viewportObject.AddComponent<Image>();
                viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
                var mask = viewportObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;
            }

            var content = viewport.Find("Content") as RectTransform;
            if (content == null)
            {
                var contentObject = CreateUIObject("Content", new Vector2(900f, 500f));
                contentObject.transform.SetParent(viewport, false);
                content = (RectTransform)contentObject.transform;
                content.anchorMin = new Vector2(0.5f, 0.5f);
                content.anchorMax = new Vector2(0.5f, 0.5f);
                content.pivot = new Vector2(0.5f, 0.5f);
            }

            if (connectionRoot == null)
                connectionRoot = CreateUIObject("Connections", content.sizeDelta).transform;
            if (nodeRoot == null)
                nodeRoot = CreateUIObject("Nodes", content.sizeDelta).transform;

            ConfigureTreeLayer(connectionRoot, content);
            ConfigureTreeLayer(nodeRoot, content);
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(nodeRoot.gameObject);
            var oldLayout = nodeRoot.GetComponent<VerticalLayoutGroup>();
            if (oldLayout != null)
                Object.DestroyImmediate(oldLayout);
            var layout = nodeRoot.GetComponent<SkillTreeGraphLayoutGroup>();
            if (layout == null)
            {
                layout = nodeRoot.gameObject.AddComponent<SkillTreeGraphLayoutGroup>();
                layout.padding = new RectOffset(40, 40, 40, 40);
            }
            connectionRoot.SetAsFirstSibling();
            nodeRoot.SetAsLastSibling();

            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.horizontalNormalizedPosition = 0f;
        }

        private static void ConfigureSkillTreeWindowRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.03f, 0.08f);
            rect.anchorMax = new Vector2(0.55f, 0.92f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ConfigureWindowChrome(Transform window)
        {
            if (window.Find("Title") is RectTransform title)
            {
                title.anchorMin = new Vector2(0f, 1f);
                title.anchorMax = new Vector2(1f, 1f);
                title.pivot = new Vector2(0.5f, 1f);
                title.offsetMin = new Vector2(20f, -54f);
                title.offsetMax = new Vector2(-20f, -10f);
            }

            if (window.Find("Feedback") is RectTransform feedback)
            {
                feedback.anchorMin = new Vector2(0f, 0f);
                feedback.anchorMax = new Vector2(1f, 0f);
                feedback.pivot = new Vector2(0.5f, 0f);
                feedback.offsetMin = new Vector2(20f, 14f);
                feedback.offsetMax = new Vector2(-20f, 46f);
            }
        }

        private static void ConfigureTreeLayer(Transform layer, RectTransform content)
        {
            layer.SetParent(content, false);
            if (layer is not RectTransform rect)
                return;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = content.sizeDelta;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Canvas EnsureCanvas(Scene scene)
        {
            Canvas canvas = null;
            foreach (var candidate in FindAllInScene<Canvas>(scene))
            {
                if (candidate.name == "SkillTreeCanvas")
                {
                    canvas = candidate;
                    break;
                }
            }

            if (canvas != null)
            {
                EnsureEventSystem(scene);
                return canvas;
            }

            var canvasObject = new GameObject("SkillTreeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (scene.IsValid())
                SceneManager.MoveGameObjectToScene(canvasObject, scene);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            EnsureEventSystem(scene);
            return canvas;
        }

        private static void EnsureFolders()
        {
            EnsureAssetFolder(TreeFolder);
            EnsureAssetFolder(SkillFolder);
            EnsureAssetFolder(EffectFolder);
            EnsureAssetFolder(UiPrefabFolder);
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

        private static void ConfigureNode(SerializedProperty property, string id, string title, string description,
            int goldCost, SkillTreeMaterialCost[] materials, string[] prerequisites,
            SkillTreeUnlockAction action, SkillDefinition skill, SkillTreeNodeType nodeType, Sprite icon,
            int maxRank, params RankEffectSpec[] effects)
        {
            property.FindPropertyRelative("nodeId").stringValue = id;
            property.FindPropertyRelative("displayName").stringValue = title;
            property.FindPropertyRelative("nodeType").intValue = (int)nodeType;
            property.FindPropertyRelative("icon").objectReferenceValue = icon;
            property.FindPropertyRelative("description").stringValue = description;
            property.FindPropertyRelative("goldCost").intValue = goldCost;
            property.FindPropertyRelative("unlockAction").intValue = (int)action;
            property.FindPropertyRelative("skillToUnlock").objectReferenceValue = skill;
            property.FindPropertyRelative("maxRank").intValue = maxRank;
            property.FindPropertyRelative("uiPosition").vector2Value = Vector2.zero;

            var rankEffects = property.FindPropertyRelative("rankEffects");
            rankEffects.arraySize = effects?.Length ?? 0;
            for (var i = 0; i < rankEffects.arraySize; i++)
            {
                var rankEffect = rankEffects.GetArrayElementAtIndex(i);
                rankEffect.FindPropertyRelative("effectType").intValue = (int)effects[i].Type;
                rankEffect.FindPropertyRelative("targetSkill").objectReferenceValue = effects[i].TargetSkill;
                rankEffect.FindPropertyRelative("amountPerRank").floatValue = effects[i].AmountPerRank;
                rankEffect.FindPropertyRelative("firstAppliedRank").intValue = effects[i].FirstAppliedRank;
            }

            var materialCosts = property.FindPropertyRelative("materialCosts");
            materialCosts.arraySize = materials != null ? materials.Length : 0;
            for (var i = 0; i < materialCosts.arraySize; i++)
            {
                materialCosts.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue = materials[i].Item;
                materialCosts.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue = materials[i].Amount;
            }

            var prereq = property.FindPropertyRelative("prerequisiteNodeIds");
            prereq.arraySize = prerequisites != null ? prerequisites.Length : 0;
            for (var i = 0; i < prereq.arraySize; i++)
                prereq.GetArrayElementAtIndex(i).stringValue = prerequisites[i];
        }

        private static void SetShape(SerializedObject so, string name, AttackShapeType type, Vector2 size, float radius, float offset)
        {
            var p = so.FindProperty(name);
            p.FindPropertyRelative("type").intValue = (int)type;
            p.FindPropertyRelative("size").vector2Value = size;
            p.FindPropertyRelative("radius").floatValue = radius;
            p.FindPropertyRelative("forwardOffset").floatValue = offset;
        }

        private static void EnsureAssetFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static GameObject CreateUIObject(string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            ((RectTransform)go.transform).sizeDelta = size;
            return go;
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

        private static Button CreateButton(Transform parent, string name, Vector2 size, Vector2 position, string text)
        {
            var go = CreateUIObject(name, size);
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).anchoredPosition = position;
            var image = go.AddComponent<Image>();
            image.color = new Color(0.95f, 0.78f, 0.25f, 0.95f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var label = CreateChildText(go.transform, "Text", size, Vector2.zero, text, 13f);
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.black;
            return button;
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
                if (item is Component component && component.gameObject.scene == scene)
                    list.Add(item);
            return list.ToArray();
        }

        private static void Set(SerializedObject so, string name, int value) => so.FindProperty(name).intValue = value;
        private static void Set(SerializedObject so, string name, float value) => so.FindProperty(name).floatValue = value;
        private static void Set(SerializedObject so, string name, string value) => so.FindProperty(name).stringValue = value;
    }
}
#endif
