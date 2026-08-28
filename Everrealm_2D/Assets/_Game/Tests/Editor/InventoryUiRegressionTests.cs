using LetterHunter.Items;
using LetterHunter.Loot;
using LetterHunter.UI.Inventory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LetterHunter.Tests.EditMode
{
    public sealed class InventoryUiRegressionTests
    {
        [Test]
        public void InventorySlotFrame_DoesNotCoverItemIcon()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/InventorySlot.prefab");
            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<InventorySlotView>(), Is.Not.Null);

            var frame = prefab.transform.Find("InventorySlotFrame")?.GetComponent<Image>();
            Assert.That(frame, Is.Not.Null);
            Assert.That(frame.fillCenter, Is.False);
            Assert.That(frame.raycastTarget, Is.False);

            var highlight = prefab.transform.Find("Highlight")?.GetComponent<Image>();
            Assert.That(highlight, Is.Not.Null);
            Assert.That(highlight.fillCenter, Is.False);
            Assert.That(highlight.raycastTarget, Is.False);

            var icon = prefab.transform.Find("Icon")?.GetComponent<Image>();
            Assert.That(icon, Is.Not.Null);
            Assert.That(icon.rectTransform.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(icon.rectTransform.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(icon.rectTransform.offsetMin, Is.EqualTo(new Vector2(8f, 8f)));
            Assert.That(icon.rectTransform.offsetMax, Is.EqualTo(new Vector2(-8f, -8f)));
            Assert.That(icon.rectTransform.rect.width, Is.GreaterThan(0f));
            Assert.That(icon.rectTransform.rect.height, Is.GreaterThan(0f));
        }

        [Test]
        public void InventoryAndShopWindows_AreEditablePrefabAssets()
        {
            var inventory = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/InventoryWindow.prefab");
            var shop = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/UI/ShopWindow.prefab");

            Assert.That(inventory, Is.Not.Null);
            Assert.That(inventory.GetComponent<InventoryWindowPresenter>(), Is.Not.Null);
            Assert.That(inventory.transform.Find("SlotGrid"), Is.Not.Null);
            Assert.That(inventory.transform.Find("CloseButton"), Is.Not.Null);
            Assert.That(inventory.transform.Find("GoldRow"), Is.Not.Null);

            Assert.That(shop, Is.Not.Null);
            Assert.That(shop.GetComponent<LetterHunter.UI.Shop.ShopWindowPresenter>(), Is.Not.Null);
            Assert.That(shop.transform.Find("Rows"), Is.Not.Null);
            Assert.That(shop.transform.Find("ShopCloseButton"), Is.Not.Null);
        }

        [Test]
        public void ItemPickup_UsesTheSameIconAsItemDefinition()
        {
            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            var itemSo = new SerializedObject(item);
            itemSo.FindProperty("icon").objectReferenceValue = sprite;
            itemSo.ApplyModifiedPropertiesWithoutUndo();

            var pickupObject = new GameObject("Pickup", typeof(SpriteRenderer), typeof(CircleCollider2D));
            var pickup = pickupObject.AddComponent<LootPickup2D>();
            pickup.ConfigureItem(new ItemStack(item, 1));

            Assert.That(pickupObject.GetComponent<SpriteRenderer>().sprite, Is.SameAs(item.Icon));

            Object.DestroyImmediate(pickupObject);
            Object.DestroyImmediate(item);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }
}
