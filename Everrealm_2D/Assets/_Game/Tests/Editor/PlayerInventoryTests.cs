using LetterHunter.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Tests.EditMode
{
    public sealed class PlayerInventoryTests
    {
        [Test]
        public void RuntimeInventory_AddsConfiguredStartingItems()
        {
            var item = CreateItem("training_shard", maxStack: 20);
            var go = new GameObject("PlayerInventory");
            var playerInventory = go.AddComponent<PlayerInventory>();
            var so = new SerializedObject(playerInventory);
            so.FindProperty("capacity").intValue = 4;
            var startingItems = so.FindProperty("startingItems");
            startingItems.arraySize = 1;
            var entry = startingItems.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("item").objectReferenceValue = item;
            entry.FindPropertyRelative("amount").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();

            var inventory = playerInventory.RuntimeInventory;

            Assert.That(inventory.Count(item), Is.EqualTo(3));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(item);
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
    }
}
