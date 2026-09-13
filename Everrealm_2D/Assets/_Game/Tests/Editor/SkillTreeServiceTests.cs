using System.Collections.Generic;
using LetterHunter.Economy;
using LetterHunter.SkillTree;
using LetterHunter.Skills;
using LetterHunter.UI.SkillTree;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.Tests.EditMode
{
    public sealed class SkillTreeServiceTests
    {
        [Test]
        public void State_TracksLevelParentsFundsAndPurchase()
        {
            var root = CreateNode("root", 1, 0);
            var child = CreateNode("child", 3, 15, root);
            var profession = CreateProfession(root, child);
            var wallet = CreateWallet(10);
            var progress = new SkillTreeProgress();
            var level = 1;
            var service = new SkillTreeService(progress, wallet, () => level, null, null);
            service.SetActiveProfession(profession);

            Assert.That(service.GetState(root), Is.EqualTo(SkillTreeNodeState.Available));
            Assert.That(service.GetState(child), Is.EqualTo(SkillTreeNodeState.Locked));
            Assert.That(service.TryPurchase(root).Success, Is.True);
            level = 3;
            Assert.That(service.GetState(child), Is.EqualTo(SkillTreeNodeState.UnavailableByFunds));
            wallet.AddGold(5);
            Assert.That(service.GetState(child), Is.EqualTo(SkillTreeNodeState.Available));
            Assert.That(service.TryPurchase(child).Success, Is.True);
            Assert.That(service.GetState(child), Is.EqualTo(SkillTreeNodeState.Purchased));
            Assert.That(wallet.Gold, Is.EqualTo(0));

            Destroy(profession, root, child, wallet.gameObject);
        }

        [Test]
        public void Purchase_UsesExistingAbilityContractWithoutDuplicates()
        {
            var ability = CreateSkill("focus_slash");
            var node = CreateNode("focus_slash", 1, 15, null, ability);
            var profession = CreateProfession(node);
            var wallet = CreateWallet(30);
            var progress = new SkillTreeProgress();
            var ownsAbility = false;
            var grantCount = 0;
            var service = new SkillTreeService(progress, wallet, () => 1, _ => ownsAbility,
                _ => { grantCount++; ownsAbility = true; });
            service.SetActiveProfession(profession);

            Assert.That(service.TryPurchase(node).Success, Is.True);
            Assert.That(progress.IsPurchased("warrior", "focus_slash"), Is.True);
            Assert.That(grantCount, Is.EqualTo(1));
            Assert.That(service.TryPurchase(node).Failure, Is.EqualTo(SkillTreePurchaseFailure.AlreadyPurchased));
            Assert.That(grantCount, Is.EqualTo(1));

            Destroy(profession, node, ability, wallet.gameObject);
        }

        [Test]
        public void Purchase_RollsBackCurrencyAndProgressWhenGrantFails()
        {
            var ability = CreateSkill("unstable_skill");
            var node = CreateNode("unstable", 1, 20, null, ability);
            var profession = CreateProfession(node);
            var wallet = CreateWallet(20);
            var progress = new SkillTreeProgress();
            var service = new SkillTreeService(progress, wallet, () => 1, _ => false,
                _ => throw new System.InvalidOperationException("grant failed"));
            service.SetActiveProfession(profession);

            var result = service.TryPurchase(node);

            Assert.That(result.Failure, Is.EqualTo(SkillTreePurchaseFailure.GrantFailed));
            Assert.That(wallet.Gold, Is.EqualTo(20));
            Assert.That(progress.IsPurchased("warrior", "unstable"), Is.False);

            Destroy(profession, node, ability, wallet.gameObject);
        }

        [Test]
        public void GraphLayout_UsesInspectorAuthoredNodePositions()
        {
            var rootNode = CreateNode("root", 1, 0, null, null, new Vector2(25f, 40f));
            var childNode = CreateNode("child", 1, 0, rootNode, null, new Vector2(420f, 180f));
            var profession = CreateProfession(rootNode, childNode);
            var root = new GameObject("Graph", typeof(RectTransform));
            var layout = root.AddComponent<SkillTreeGraphLayoutGroup>();
            var rootView = CreateView(root.transform, "Root");
            var childView = CreateView(root.transform, "Child");
            var views = new Dictionary<string, SkillTreeNodeView>
            {
                ["root"] = rootView,
                ["child"] = childView
            };

            var size = layout.Arrange(profession, views);

            Assert.That(((RectTransform)rootView.transform).anchoredPosition,
                Is.EqualTo(new Vector2(135f, -130f)));
            Assert.That(((RectTransform)childView.transform).anchoredPosition,
                Is.EqualTo(new Vector2(530f, -270f)));
            Assert.That(size.x, Is.GreaterThan(childNode.UiPosition.x));

            Destroy(root, profession, rootNode, childNode);
        }

        [Test]
        public void StartingSkills_RespectTreePurchasesAndOldLoadoutAfterRestoreAndReset()
        {
            var skill = CreateSkill("gated");
            var free = CreateSkill("innate");
            var node = CreateNode("gated_node", 1, 0, ability: skill);
            var profession = CreateProfession(node);
            var definition = ScriptableObject.CreateInstance<LetterHunter.Classes.ClassDefinition>();
            var playerObject = new GameObject("GatedPlayer");
            playerObject.SetActive(false);
            try
            {
                var classData = new SerializedObject(definition);
                var starting = classData.FindProperty("startingSkills");
                starting.arraySize = 2;
                starting.GetArrayElementAtIndex(0).objectReferenceValue = skill;
                starting.GetArrayElementAtIndex(1).objectReferenceValue = free;
                classData.ApplyModifiedPropertiesWithoutUndo();
                var player = playerObject.AddComponent<LetterHunter.Characters.PlayerClassController>();
                var targets = playerObject.AddComponent<LetterHunter.Infrastructure.Physics2DTargetProvider>();
                var playerData = new SerializedObject(player);
                playerData.FindProperty("classDefinition").objectReferenceValue = definition;
                playerData.FindProperty("targetProviderComponent").objectReferenceValue = targets;
                playerData.ApplyModifiedPropertiesWithoutUndo();
                var tree = playerObject.AddComponent<PlayerSkillTreeController>();
                var treeData = new SerializedObject(tree);
                treeData.FindProperty("startingProfession").objectReferenceValue = profession;
                treeData.FindProperty("player").objectReferenceValue = player;
                treeData.ApplyModifiedPropertiesWithoutUndo();
                var loadout = playerObject.AddComponent<LetterHunter.UI.Skills.PlayerSkillLoadout>();
                player.ResetLearnedSkills();
                Assert.That(player.IsSkillAvailable(skill), Is.False);
                Assert.That(player.IsSkillAvailable(free), Is.True);
                Assert.That(loadout.TryAssignSkill(0, skill, out _), Is.False);
                loadout.SetSlots(new[] { new LetterHunter.UI.Skills.SkillSlotBinding(0, skill, "1") });
                Assert.That(loadout.ResolveSkill(player, 0, out _), Is.Null);
                Assert.That(player.UseSkill(skill.SkillId).Failure, Is.EqualTo(SkillUseFailure.NotRegistered));

                Assert.That(tree.TryGrantNodeForDebug(node, out _), Is.True);
                Assert.That(player.IsSkillAvailable(skill), Is.True);
                Assert.That(loadout.ResolveSkill(player, 0, out _), Is.SameAs(skill));
                Assert.That(loadout.ResolveSkill(player, 1, out _), Is.Null,
                    "A learned skill must not appear in an unassigned skill bar slot.");
                tree.RestoreProgress(profession.ProfessionId, System.Array.Empty<PurchasedSkillNode>());
                Assert.That(player.IsSkillAvailable(skill), Is.False);
                Assert.That(loadout.ResolveSkill(player, 0, out _), Is.Null);
                tree.TryGrantNodeForDebug(node, out _);
                tree.ResetProgress();
                Assert.That(player.IsSkillAvailable(skill), Is.False);
                Assert.That(player.IsSkillAvailable(free), Is.True);
            }
            finally { Destroy(playerObject, definition, profession, node, skill, free); }
        }

        [Test]
        public void DebugGrant_IncludesParentsWithoutSpendingAndRejectsCycles()
        {
            var root = CreateNode("root", 10, 500);
            var child = CreateNode("child", 20, 900, root);
            var profession = CreateProfession(root, child);
            var go = new GameObject("DebugGrant");
            go.SetActive(false);
            try
            {
                var tree = go.AddComponent<PlayerSkillTreeController>();
                var data = new SerializedObject(tree);
                data.FindProperty("startingProfession").objectReferenceValue = profession;
                data.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(tree.TryGrantNodeForDebug(child, out _), Is.True);
                Assert.That(tree.Progress.IsPurchased("warrior", "root"), Is.True);
                Assert.That(tree.Progress.IsPurchased("warrior", "child"), Is.True);
                Assert.That(tree.CurrentCoins, Is.Zero);
                tree.ResetProgress();
                var rootData = new SerializedObject(root);
                var parents = rootData.FindProperty("parentNodes");
                parents.arraySize = 1;
                parents.GetArrayElementAtIndex(0).objectReferenceValue = child;
                rootData.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(tree.TryGrantNodeForDebug(child, out _), Is.False);
                Assert.That(tree.Progress.IsPurchased("warrior", "root"), Is.False);
            }
            finally { Destroy(go, profession, root, child); }
        }

        [Test]
        public void Purchase_OnlyPublishesCommitAfterWalletAndGrantComplete()
        {
            var skill = CreateSkill("paid");
            var node = CreateNode("paid", 1, 10, ability: skill);
            var profession = CreateProfession(node);
            var wallet = CreateWallet(20);
            try
            {
                bool granted = false;
                var service = new SkillTreeService(new SkillTreeProgress(), wallet, () => 1, _ => false, _ => granted = true);
                service.SetActiveProfession(profession);
                wallet.GoldChanged += _ => Assert.That(service.IsPurchasing, Is.True);
                int commits = 0;
                service.NodePurchased += (_, _) =>
                {
                    Assert.That(service.IsPurchasing, Is.False);
                    Assert.That(granted, Is.True);
                    Assert.That(wallet.Gold, Is.EqualTo(10));
                    commits++;
                };
                Assert.That(service.TryPurchase(node).Success, Is.True);
                Assert.That(commits, Is.EqualTo(1));
            }
            finally { Destroy(profession, node, skill, wallet.gameObject); }
        }

        [Test]
        public void SkillTreeWindowPrefab_HasTwoAxisScrollingAndConnectionSetup()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/UI/SkillTree/SkillTreeWindow.prefab");

            Assert.That(prefab, Is.Not.Null);
            var scroll = prefab.GetComponentInChildren<ScrollRect>(true);
            Assert.That(scroll, Is.Not.Null);
            Assert.That(scroll.horizontal, Is.True);
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.horizontalScrollbar, Is.Not.Null);
            Assert.That(scroll.verticalScrollbar, Is.Not.Null);

            var visual = prefab.GetComponent<EverrealmSkillTreeVisual>();
            Assert.That(visual, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<RectTransform>(true),
                Has.Some.Matches<RectTransform>(rect => rect.name == "ConnectionLayer"));

            var connectionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Resources/EnglishKingdomSkillTree/SkillTreeConnection.prefab");
            Assert.That(connectionPrefab, Is.Not.Null);
            Assert.That(connectionPrefab.GetComponent<EverrealmSkillTreeConnectionVisual>(), Is.Not.Null);
        }

        [TestCase(300f, 180f)]
        [TestCase(2400f, 1600f)]
        public void Visual_ZoomedOutGraphCentersInViewport(float width, float height)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Prefabs/UI/SkillTree/SkillTreeWindow.prefab");
            var canvas = new GameObject("Preview", typeof(RectTransform), typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var instance = Object.Instantiate(prefab, canvas.transform);
            var first = CreateNode("left", 1, 0, position: new Vector2(-.3f, .2f));
            var second = CreateNode("right", 1, 0, position: new Vector2(.8f, 1.2f));
            var profession = CreateProfession(first, second);
            try
            {
                var visual = instance.GetComponent<EverrealmSkillTreeVisual>();
                var scroll = instance.GetComponentInChildren<ScrollRect>(true);
                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance;
                typeof(EverrealmSkillTreeVisual).GetMethod("ConfigureTreeScroll", flags).Invoke(visual, null);
                var metrics = new EverrealmSkillTreeLayout.Metrics(-.3f, .2f, 100f, 100f, new Vector2(width, height));
                var positions = (Dictionary<SkillNodeDefinitionSO, Vector2>)typeof(EverrealmSkillTreeVisual)
                    .GetMethod("BuildLayout", flags).Invoke(visual, new object[] { profession, metrics });
                scroll.content.sizeDelta = metrics.ContentSize;
                typeof(EverrealmSkillTreeVisual).GetField("_zoom", flags).SetValue(visual, .75f);
                scroll.velocity = new Vector2(500f, -200f);
                typeof(EverrealmSkillTreeVisual).GetMethod("CenterTreeScroll", flags).Invoke(visual, null);
                Canvas.ForceUpdateCanvases();

                var graphCenter = (positions[first] + positions[second]) * .5f;
                var worldCenter = scroll.content.TransformPoint(scroll.content.rect.min + graphCenter);
                var viewportCenter = scroll.viewport.InverseTransformPoint(worldCenter);
                Assert.That(Vector2.Distance(viewportCenter, scroll.viewport.rect.center), Is.LessThan(.1f));
                Assert.That(scroll.content.localScale.x, Is.EqualTo(.75f));
                Assert.That(scroll.velocity, Is.EqualTo(Vector2.zero));
                Assert.That(first.UiPosition, Is.EqualTo(new Vector2(-.3f, .2f)));
                Assert.That(second.UiPosition, Is.EqualTo(new Vector2(.8f, 1.2f)));
            }
            finally { Destroy(canvas, profession, first, second); }
        }

        private static SkillTreeNodeView CreateView(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(SkillTreeNodeView));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = new Vector2(220f, 120f);
            return go.GetComponent<SkillTreeNodeView>();
        }

        private static SkillNodeDefinitionSO CreateNode(string id, int level, int price,
            SkillNodeDefinitionSO parent = null, SkillDefinition ability = null, Vector2? position = null)
        {
            var node = ScriptableObject.CreateInstance<SkillNodeDefinitionSO>();
            var so = new SerializedObject(node);
            so.FindProperty("nodeId").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.FindProperty("requiredLevel").intValue = level;
            so.FindProperty("price").intValue = price;
            so.FindProperty("abilityToGrant").objectReferenceValue = ability;
            so.FindProperty("uiPosition").vector2Value = position ?? Vector2.zero;
            var parents = so.FindProperty("parentNodes");
            parents.arraySize = parent != null ? 1 : 0;
            if (parent != null) parents.GetArrayElementAtIndex(0).objectReferenceValue = parent;
            so.ApplyModifiedPropertiesWithoutUndo();
            return node;
        }

        private static ProfessionDefinitionSO CreateProfession(params SkillNodeDefinitionSO[] nodes)
        {
            var profession = ScriptableObject.CreateInstance<ProfessionDefinitionSO>();
            var so = new SerializedObject(profession);
            so.FindProperty("professionId").stringValue = "warrior";
            so.FindProperty("displayName").stringValue = "Warrior";
            var nodeList = so.FindProperty("skillNodes");
            nodeList.arraySize = nodes.Length;
            for (var i = 0; i < nodes.Length; i++) nodeList.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return profession;
        }

        private static SkillDefinition CreateSkill(string id)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var so = new SerializedObject(skill);
            so.FindProperty("skillId").stringValue = id;
            so.FindProperty("displayName").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return skill;
        }

        private static CurrencyWallet CreateWallet(int gold)
        {
            var go = new GameObject("Wallet");
            var wallet = go.AddComponent<CurrencyWallet>();
            wallet.SetGold(gold);
            return wallet;
        }

        private static void Destroy(params Object[] objects)
        {
            foreach (var item in objects) if (item != null) Object.DestroyImmediate(item);
        }
    }
}
