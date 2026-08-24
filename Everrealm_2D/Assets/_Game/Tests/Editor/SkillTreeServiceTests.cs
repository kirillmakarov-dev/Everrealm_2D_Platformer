using LetterHunter.Economy;
using LetterHunter.Items;
using LetterHunter.SkillTree;
using LetterHunter.Skills;
using LetterHunter.UI.SkillTree;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.Tests.EditMode
{
    public sealed class SkillTreeServiceTests
    {
        [Test]
        public void TryUnlock_FailsWhenPrerequisiteIsMissing()
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var tree = CreateTree(null, skill);
            var wallet = CreateWallet(100);
            var inventory = new Inventory(2);
            var service = new SkillTreeService(tree, new SkillTreeProgress(), wallet, inventory, null);

            var result = service.TryUnlock("focus_slash");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Failure, Is.EqualTo(SkillTreeUnlockFailure.MissingPrerequisite));
            Object.DestroyImmediate(wallet.gameObject);
        }

        [Test]
        public void TryUnlock_FailsWhenMaterialsAreMissing()
        {
            var shard = CreateItem("training_shard", 20);
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var tree = CreateTree(shard, skill);
            var wallet = CreateWallet(100);
            var progress = new SkillTreeProgress();
            progress.MarkUnlocked("training_roots");
            var service = new SkillTreeService(tree, progress, wallet, new Inventory(2), null);

            var result = service.TryUnlock("focus_slash");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Failure, Is.EqualTo(SkillTreeUnlockFailure.NotEnoughMaterials));
            Object.DestroyImmediate(wallet.gameObject);
        }

        [Test]
        public void TryUnlock_SpendsCostsAndInvokesSkillUnlock()
        {
            var shard = CreateItem("training_shard", 20);
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var tree = CreateTree(shard, skill);
            var wallet = CreateWallet(100);
            var inventory = new Inventory(2);
            inventory.TryAdd(new ItemStack(shard, 2));
            var progress = new SkillTreeProgress();
            progress.MarkUnlocked("training_roots");
            SkillDefinition unlockedSkill = null;
            var service = new SkillTreeService(tree, progress, wallet, inventory, s => unlockedSkill = s);

            var result = service.TryUnlock("focus_slash");

            Assert.That(result.Success, Is.True);
            Assert.That(progress.IsUnlocked("focus_slash"), Is.True);
            Assert.That(wallet.Gold, Is.EqualTo(85));
            Assert.That(inventory.Count(shard), Is.EqualTo(1));
            Assert.That(unlockedSkill, Is.EqualTo(skill));
            Assert.That(service.TryUnlock("focus_slash").Failure, Is.EqualTo(SkillTreeUnlockFailure.AlreadyUnlocked));
            Object.DestroyImmediate(wallet.gameObject);
        }

        [Test]
        public void TryUnlock_UpgradesUntilMaxRankAndOnlyGrantsSkillOnce()
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var tree = CreateTree(null, skill, 2);
            var wallet = CreateWallet(100);
            var progress = new SkillTreeProgress();
            progress.MarkUnlocked("training_roots");
            var grantCount = 0;
            var purchasedRanks = new List<int>();
            var service = new SkillTreeService(tree, progress, wallet, new Inventory(2), _ => grantCount++,
                (_, rank) => purchasedRanks.Add(rank));

            Assert.That(service.TryUnlock("focus_slash").Success, Is.True);
            Assert.That(service.TryUnlock("focus_slash").Success, Is.True);
            Assert.That(progress.GetRank("focus_slash"), Is.EqualTo(2));
            Assert.That(grantCount, Is.EqualTo(1));
            Assert.That(purchasedRanks, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(service.TryUnlock("focus_slash").Failure, Is.EqualTo(SkillTreeUnlockFailure.MaxRankReached));
            Assert.That(wallet.Gold, Is.EqualTo(70));

            Object.DestroyImmediate(wallet.gameObject);
            Object.DestroyImmediate(tree);
            Object.DestroyImmediate(skill);
        }

        [Test]
        public void NodeView_TransientFailureKeepsUpgradeClickableAndUnlockedSkillEnablesSelect()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/SkillTreeNode.prefab");
            var instance = Object.Instantiate(prefab);
            var view = instance.GetComponent<SkillTreeNodeView>();
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var tree = CreateTree(null, skill, 2);
            Assert.That(tree.TryGetNode("focus_slash", out var node), Is.True);
            var progress = new SkillTreeProgress();

            view.Bind(node.NodeId, _ => { }, _ => { });
            view.Render(node, progress, SkillTreeUnlockFailure.NotEnoughMaterials);

            Assert.That(instance.transform.Find("UpgradeButton").GetComponent<Button>().interactable, Is.True);
            Assert.That(instance.transform.Find("SelectButton").gameObject.activeSelf, Is.False);

            progress.SetRank(node.NodeId, 1);
            view.Render(node, progress, SkillTreeUnlockFailure.None);

            Assert.That(instance.transform.Find("UpgradeButton").GetComponent<Button>().interactable, Is.True);
            Assert.That(instance.transform.Find("SelectButton").gameObject.activeSelf, Is.True);
            Assert.That(instance.transform.Find("SelectButton").GetComponent<Button>().interactable, Is.True);

            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(tree);
            Object.DestroyImmediate(skill);
        }

        [Test]
        public void ValidateDefinition_FindsIdsPrerequisitesCyclesAndInvalidRankEffects()
        {
            var tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
            var so = new SerializedObject(tree);
            var nodes = so.FindProperty("nodes");
            nodes.arraySize = 3;
            ConfigureValidationNode(nodes.GetArrayElementAtIndex(0), "node_a", new[] { "node_b", "missing" });
            ConfigureValidationNode(nodes.GetArrayElementAtIndex(1), "node_b", new[] { "node_a" });
            ConfigureValidationNode(nodes.GetArrayElementAtIndex(2), "node_a", null);

            var first = nodes.GetArrayElementAtIndex(0);
            first.FindPropertyRelative("unlockAction").intValue = (int)SkillTreeUnlockAction.UnlockSkill;
            first.FindPropertyRelative("skillToUnlock").objectReferenceValue = null;
            first.FindPropertyRelative("maxRank").intValue = 2;
            var effects = first.FindPropertyRelative("rankEffects");
            effects.arraySize = 1;
            var effect = effects.GetArrayElementAtIndex(0);
            effect.FindPropertyRelative("effectType").intValue = (int)SkillTreeRankEffectType.SkillDamagePercent;
            effect.FindPropertyRelative("targetSkill").objectReferenceValue = null;
            effect.FindPropertyRelative("amountPerRank").floatValue = 0.1f;
            effect.FindPropertyRelative("firstAppliedRank").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            var errors = tree.ValidateDefinition();

            Assert.That(errors.Any(message => message.Contains("Duplicate node id")), Is.True);
            Assert.That(errors.Any(message => message.Contains("missing node")), Is.True);
            Assert.That(errors.Any(message => message.Contains("cycle")), Is.True);
            Assert.That(errors.Any(message => message.Contains("no SkillDefinition")), Is.True);
            Assert.That(errors.Any(message => message.Contains("without a target skill")), Is.True);
            Assert.That(errors.Any(message => message.Contains("above max rank")), Is.True);
            Object.DestroyImmediate(tree);
        }

        [Test]
        public void TryRefundRank_BlocksLastRankWhenUnlockedNodeDependsOnIt()
        {
            var tree = CreateTree(null, ScriptableObject.CreateInstance<SkillDefinition>());
            var wallet = CreateWallet(0);
            var progress = new SkillTreeProgress();
            progress.SetRank("training_roots", 1);
            progress.SetRank("focus_slash", 1);
            var service = new SkillTreeService(tree, progress, wallet, new Inventory(2), null);

            var result = service.TryRefundRank("training_roots");

            Assert.That(result.Success, Is.False);
            Assert.That(result.Failure, Is.EqualTo(SkillTreeRespecFailure.HasUnlockedDependents));
            Assert.That(progress.GetRank("training_roots"), Is.EqualTo(1));
            Object.DestroyImmediate(wallet.gameObject);
            Object.DestroyImmediate(tree);
        }

        [Test]
        public void TryRefundRank_RefundsConfiguredGoldMaterialsAndPublishesNewRank()
        {
            var shard = CreateItem("training_shard", 20);
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var tree = CreateTree(shard, skill);
            var treeSo = new SerializedObject(tree);
            treeSo.FindProperty("goldRefundRate").floatValue = 0.75f;
            treeSo.FindProperty("refundMaterials").boolValue = true;
            treeSo.ApplyModifiedPropertiesWithoutUndo();
            var wallet = CreateWallet(0);
            var inventory = new Inventory(2);
            var progress = new SkillTreeProgress();
            progress.SetRank("training_roots", 1);
            progress.SetRank("focus_slash", 1);
            var changedRank = -1;
            var service = new SkillTreeService(tree, progress, wallet, inventory, null,
                (_, rank) => changedRank = rank);

            var result = service.TryRefundRank("focus_slash");

            Assert.That(result.Success, Is.True);
            Assert.That(result.RefundedGold, Is.EqualTo(11));
            Assert.That(wallet.Gold, Is.EqualTo(11));
            Assert.That(inventory.Count(shard), Is.EqualTo(1));
            Assert.That(progress.GetRank("focus_slash"), Is.EqualTo(0));
            Assert.That(changedRank, Is.EqualTo(0));
            Object.DestroyImmediate(wallet.gameObject);
            Object.DestroyImmediate(tree);
            Object.DestroyImmediate(shard);
            Object.DestroyImmediate(skill);
        }

        [Test]
        public void GraphLayout_PlacesPrerequisiteLeftOfDependentAndConnectionPrefabExists()
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var tree = CreateTree(null, skill);
            var root = new GameObject("GraphLayout", typeof(RectTransform));
            var layout = root.AddComponent<SkillTreeGraphLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            var rootNodeObject = new GameObject("RootNode", typeof(RectTransform), typeof(SkillTreeNodeView));
            rootNodeObject.transform.SetParent(root.transform, false);
            var focusNodeObject = new GameObject("FocusNode", typeof(RectTransform), typeof(SkillTreeNodeView));
            focusNodeObject.transform.SetParent(root.transform, false);
            var views = new Dictionary<string, SkillTreeNodeView>
            {
                ["training_roots"] = rootNodeObject.GetComponent<SkillTreeNodeView>(),
                ["focus_slash"] = focusNodeObject.GetComponent<SkillTreeNodeView>()
            };

            var size = layout.Arrange(tree, views);

            Assert.That(((RectTransform)rootNodeObject.transform).anchoredPosition.x,
                Is.LessThan(((RectTransform)focusNodeObject.transform).anchoredPosition.x));
            Assert.That(size.x, Is.GreaterThan(700f));
            Assert.That(AssetDatabase.LoadAssetAtPath<SkillTreeConnectionView>(
                "Assets/_Game/Prefabs/UI/SkillTreeConnection.prefab"), Is.Not.Null);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(tree);
            Object.DestroyImmediate(skill);
        }

        private static void ConfigureValidationNode(SerializedProperty property, string nodeId, string[] prerequisites)
        {
            property.FindPropertyRelative("nodeId").stringValue = nodeId;
            property.FindPropertyRelative("displayName").stringValue = nodeId;
            property.FindPropertyRelative("maxRank").intValue = 1;
            var prereq = property.FindPropertyRelative("prerequisiteNodeIds");
            prereq.arraySize = prerequisites?.Length ?? 0;
            for (var i = 0; i < prereq.arraySize; i++)
                prereq.GetArrayElementAtIndex(i).stringValue = prerequisites[i];
        }

        private static CurrencyWallet CreateWallet(int gold)
        {
            var go = new GameObject("Wallet");
            var wallet = go.AddComponent<CurrencyWallet>();
            wallet.AddGold(gold);
            return wallet;
        }

        private static ItemDefinition CreateItem(string itemId, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            var so = new SerializedObject(item);
            so.FindProperty("itemId").stringValue = itemId;
            so.FindProperty("displayName").stringValue = itemId;
            so.FindProperty("maxStack").intValue = maxStack;
            so.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static SkillTreeDefinition CreateTree(ItemDefinition shard, SkillDefinition skill, int focusSlashMaxRank = 1)
        {
            var tree = ScriptableObject.CreateInstance<SkillTreeDefinition>();
            var so = new SerializedObject(tree);
            so.FindProperty("treeId").stringValue = "test_tree";
            so.FindProperty("displayName").stringValue = "Test Tree";
            var nodes = so.FindProperty("nodes");
            nodes.arraySize = 2;

            ConfigureNode(nodes.GetArrayElementAtIndex(0), "training_roots", 10, null, null, SkillTreeUnlockAction.None, null);
            ConfigureNode(nodes.GetArrayElementAtIndex(1), "focus_slash", 15,
                shard != null ? new[] { new SkillTreeMaterialCost(shard, 1) } : null,
                new[] { "training_roots" }, SkillTreeUnlockAction.UnlockSkill, skill);
            nodes.GetArrayElementAtIndex(1).FindPropertyRelative("maxRank").intValue = focusSlashMaxRank;

            so.ApplyModifiedPropertiesWithoutUndo();
            return tree;
        }

        private static void ConfigureNode(SerializedProperty property, string nodeId, int goldCost,
            SkillTreeMaterialCost[] costs, string[] prerequisites, SkillTreeUnlockAction action, SkillDefinition skill)
        {
            property.FindPropertyRelative("nodeId").stringValue = nodeId;
            property.FindPropertyRelative("displayName").stringValue = nodeId;
            property.FindPropertyRelative("goldCost").intValue = goldCost;
            property.FindPropertyRelative("unlockAction").intValue = (int)action;
            property.FindPropertyRelative("skillToUnlock").objectReferenceValue = skill;

            var materialCosts = property.FindPropertyRelative("materialCosts");
            materialCosts.arraySize = costs != null ? costs.Length : 0;
            for (var i = 0; i < materialCosts.arraySize; i++)
            {
                materialCosts.GetArrayElementAtIndex(i).FindPropertyRelative("item").objectReferenceValue = costs[i].Item;
                materialCosts.GetArrayElementAtIndex(i).FindPropertyRelative("amount").intValue = costs[i].Amount;
            }

            var prereq = property.FindPropertyRelative("prerequisiteNodeIds");
            prereq.arraySize = prerequisites != null ? prerequisites.Length : 0;
            for (var i = 0; i < prereq.arraySize; i++)
                prereq.GetArrayElementAtIndex(i).stringValue = prerequisites[i];
        }
    }
}
