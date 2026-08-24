using LetterHunter.Economy;
using LetterHunter.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Tests.EditMode
{
    public sealed class ShopServiceTests
    {
        [Test]
        public void TrySell_RemovesItemsAndAddsGold()
        {
            var item = CreateItem("training_shard", maxStack: 10, sellPrice: 4);
            var inventory = new Inventory(2);
            inventory.TryAdd(new ItemStack(item, 3));

            var walletObject = new GameObject("Wallet");
            var wallet = walletObject.AddComponent<CurrencyWallet>();
            var shop = new ShopService(inventory, wallet);

            var result = shop.TrySell(item, 2);

            Assert.That(result.Success, Is.True);
            Assert.That(result.GoldEarned, Is.EqualTo(8));
            Assert.That(wallet.Gold, Is.EqualTo(8));
            Assert.That(inventory.Count(item), Is.EqualTo(1));

            Object.DestroyImmediate(walletObject);
        }

        private static ItemDefinition CreateItem(string itemId, int maxStack, int sellPrice)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            var serialized = new SerializedObject(item);
            serialized.FindProperty("itemId").stringValue = itemId;
            serialized.FindProperty("displayName").stringValue = itemId;
            serialized.FindProperty("maxStack").intValue = maxStack;
            serialized.FindProperty("sellPrice").intValue = sellPrice;
            serialized.FindProperty("canSell").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }
    }
}
