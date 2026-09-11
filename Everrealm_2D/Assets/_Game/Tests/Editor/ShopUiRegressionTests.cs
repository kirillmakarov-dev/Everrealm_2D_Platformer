using LetterHunter.Characters;
using LetterHunter.UI.Shop;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.Tests.EditMode
{
    public sealed class ShopUiRegressionTests
    {
        private const string ActionButtonPrefabPath = "Assets/_Game/Prefabs/UI/ShopActionButton.prefab";
        private const string RowPrefabPath = "Assets/_Game/Prefabs/UI/ShopItemRow.prefab";

        [Test]
        public void ShopRowPrefab_HasClickableSellButtons()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/ShopItemRow.prefab");
            Assert.That(prefab, Is.Not.Null);

            AssertClickable(prefab.transform.Find("SellOneButton")?.GetComponent<Button>());
            AssertClickable(prefab.transform.Find("SellStackButton")?.GetComponent<Button>());
        }

        [Test]
        public void ShopRowButtons_AreInstancesOfSharedActionButtonPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            AssertSharedButtonPrefab(prefab.transform.Find("SellOneButton")?.GetComponent<Button>());
            AssertSharedButtonPrefab(prefab.transform.Find("SellStackButton")?.GetComponent<Button>());
        }

        [Test]
        public void ShopWindow_ReferencesOneRowPrefab_WithoutAuthoredRowCopies()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/ShopWindow.prefab");
            Assert.That(prefab, Is.Not.Null);

            var presenter = prefab.GetComponent<ShopWindowPresenter>();
            Assert.That(presenter, Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<ShopItemRowView>(true), Is.Empty);

            var so = new SerializedObject(presenter);
            var rowPrefab = so.FindProperty("rowPrefab").objectReferenceValue;
            Assert.That(AssetDatabase.GetAssetPath(rowPrefab), Is.EqualTo(RowPrefabPath));
        }

        [Test]
        public void ShopWindow_BlocksCharacterInputWhileVisible()
        {
            var player = new GameObject("TestPlayer");
            var input = player.AddComponent<CharacterInputRouter>();
            var shopObject = new GameObject("TestShop", typeof(CanvasGroup));
            var presenter = shopObject.AddComponent<ShopWindowPresenter>();
            var presenterSo = new SerializedObject(presenter);
            presenterSo.FindProperty("characterInput").objectReferenceValue = input;
            presenterSo.FindProperty("windowGroup").objectReferenceValue = shopObject.GetComponent<CanvasGroup>();
            presenterSo.ApplyModifiedPropertiesWithoutUndo();

            presenter.SetVisible(true);
            Assert.That(input.IsBlocked, Is.True);

            presenter.SetVisible(false);
            Assert.That(input.IsBlocked, Is.False);

            Object.DestroyImmediate(shopObject);
            Object.DestroyImmediate(player);
        }

        private static void AssertClickable(Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(button.interactable, Is.True);
            Assert.That(button.targetGraphic, Is.Not.Null);
            Assert.That(button.targetGraphic.raycastTarget, Is.True);
            var layout = button.GetComponent<LayoutElement>();
            Assert.That(layout, Is.Not.Null);
            Assert.That(layout.preferredWidth, Is.GreaterThan(0f));
            Assert.That(layout.preferredHeight, Is.GreaterThan(0f));
        }

        private static void AssertSharedButtonPrefab(Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(button.gameObject),
                Is.EqualTo(ActionButtonPrefabPath));
        }
    }
}
