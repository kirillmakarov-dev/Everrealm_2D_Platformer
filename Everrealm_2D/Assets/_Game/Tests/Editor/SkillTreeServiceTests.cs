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
