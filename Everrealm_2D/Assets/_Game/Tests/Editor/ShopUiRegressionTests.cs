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
        [Test]
        public void ShopRowPrefab_HasClickableSellButtons()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/ShopItemRow.prefab");
            Assert.That(prefab, Is.Not.Null);

            AssertClickable(prefab.transform.Find("SellOneButton")?.GetComponent<Button>());
            AssertClickable(prefab.transform.Find("SellStackButton")?.GetComponent<Button>());
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
    }
}
