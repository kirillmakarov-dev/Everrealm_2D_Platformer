using LetterHunter.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Tests.EditMode
{
    public sealed class InventoryTests
    {
        [Test]
        public void TryAdd_MergesExistingStacksBeforeUsingEmptySlots()
        {
            var item = CreateItem("test_material", maxStack: 5);
            var inventory = new Inventory(3);

            inventory.TryAdd(new ItemStack(item, 3));
            var result = inventory.TryAdd(new ItemStack(item, 4));

            Assert.That(result.Status, Is.EqualTo(InventoryAddStatus.AddedAll));
            Assert.That(inventory.Slots[0].Amount, Is.EqualTo(5));
            Assert.That(inventory.Slots[1].Amount, Is.EqualTo(2));
            Assert.That(inventory.Count(item), Is.EqualTo(7));
        }

        [Test]
        public void TryAdd_ReturnsPartialWhenInventoryCannotFitFullStack()
        {
            var item = CreateItem("test_material", maxStack: 5);
            var inventory = new Inventory(1);

            var result = inventory.TryAdd(new ItemStack(item, 8));

            Assert.That(result.Status, Is.EqualTo(InventoryAddStatus.AddedPartial));
            Assert.That(result.AddedAmount, Is.EqualTo(5));
            Assert.That(result.RemainingAmount, Is.EqualTo(3));
            Assert.That(inventory.Count(item), Is.EqualTo(5));
        }

        [Test]
        public void TryRemove_RemovesRequestedAmountAcrossStacks()
        {
            var item = CreateItem("test_material", maxStack: 5);
            var inventory = new Inventory(2);
            inventory.TryAdd(new ItemStack(item, 8));

            var removed = inventory.TryRemove(item, 6);

            Assert.That(removed, Is.True);
            Assert.That(inventory.Count(item), Is.EqualTo(2));
        }

        [Test]
        public void TryMoveSlot_MovesStackIntoEmptySlot()
        {
            var item = CreateItem("test_material", maxStack: 5);
            var inventory = new Inventory(2);
            inventory.TryAdd(new ItemStack(item, 3));

            var moved = inventory.TryMoveSlot(0, 1);

            Assert.That(moved, Is.True);
            Assert.That(inventory.Slots[0].IsEmpty, Is.True);
            Assert.That(inventory.Slots[1].Item, Is.EqualTo(item));
            Assert.That(inventory.Slots[1].Amount, Is.EqualTo(3));
        }

        [Test]
        public void TryMoveSlot_SwapsDifferentItems()
        {
            var shard = CreateItem("shard", maxStack: 5);
            var sword = CreateItem("sword", maxStack: 1);
            var inventory = new Inventory(2);
            inventory.TryAdd(new ItemStack(shard, 3));
            inventory.TryAdd(new ItemStack(sword, 1));

            var moved = inventory.TryMoveSlot(0, 1);

            Assert.That(moved, Is.True);
            Assert.That(inventory.Slots[0].Item, Is.EqualTo(sword));
            Assert.That(inventory.Slots[1].Item, Is.EqualTo(shard));
            Assert.That(inventory.Slots[1].Amount, Is.EqualTo(3));
        }

        [Test]
        public void TryMergeSlots_MergesMatchingStacks()
        {
            var item = CreateItem("test_material", maxStack: 5);
            var inventory = new Inventory(2);
            inventory.TryAdd(new ItemStack(item, 5));
            inventory.Slots[0].Set(item, 3);
            inventory.Slots[1].Set(item, 1);

            var merged = inventory.TryMergeSlots(1, 0);

            Assert.That(merged, Is.True);
            Assert.That(inventory.Slots[0].Amount, Is.EqualTo(4));
            Assert.That(inventory.Slots[1].IsEmpty, Is.True);
        }

        [Test]
        public void TryMoveSlot_PartialMergeLeavesRemainderInSource()
        {
            var item = CreateItem("test_material", maxStack: 5);
            var inventory = new Inventory(2);
            inventory.Slots[0].Set(item, 4);
            inventory.Slots[1].Set(item, 3);

            var moved = inventory.TryMoveSlot(1, 0);

            Assert.That(moved, Is.True);
            Assert.That(inventory.Slots[0].Amount, Is.EqualTo(5));
            Assert.That(inventory.Slots[1].Amount, Is.EqualTo(2));
        }

        [Test]
        public void TrySplitStack_CreatesNewStackInEmptySlot()
        {
            var item = CreateItem("test_material", maxStack: 10);
            var inventory = new Inventory(2);
            inventory.TryAdd(new ItemStack(item, 8));

            var split = inventory.TrySplitStack(0, 1, 3);

            Assert.That(split, Is.True);
            Assert.That(inventory.Slots[0].Amount, Is.EqualTo(5));
            Assert.That(inventory.Slots[1].Item, Is.EqualTo(item));
            Assert.That(inventory.Slots[1].Amount, Is.EqualTo(3));
        }

        [Test]
        public void TrySplitStack_RejectsInvalidAmount()
        {
            var item = CreateItem("test_material", maxStack: 10);
            var inventory = new Inventory(2);
            inventory.TryAdd(new ItemStack(item, 3));

            Assert.That(inventory.TrySplitStack(0, 1, 0), Is.False);
            Assert.That(inventory.TrySplitStack(0, 1, 3), Is.False);
            Assert.That(inventory.Slots[0].Amount, Is.EqualTo(3));
            Assert.That(inventory.Slots[1].IsEmpty, Is.True);
        }

        private static ItemDefinition CreateItem(string itemId, int maxStack, int sellPrice = 1)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            SetSerialized(item, "itemId", itemId);
            SetSerialized(item, "displayName", itemId);
            SetSerialized(item, "maxStack", maxStack);
            SetSerialized(item, "sellPrice", sellPrice);
            return item;
        }

        private static void SetSerialized(Object target, string fieldName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(fieldName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerialized(Object target, string fieldName, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(fieldName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
