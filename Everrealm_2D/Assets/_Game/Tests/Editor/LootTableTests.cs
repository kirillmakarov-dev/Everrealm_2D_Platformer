using LetterHunter.Effects;
using LetterHunter.Items;
using LetterHunter.Loot;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Tests.EditMode
{
    public sealed class LootTableTests
    {
        [Test]
        public void Roll_ReturnsCoinsWithinConfiguredRange()
        {
            var table = ScriptableObject.CreateInstance<LootTable>();
            SetSerialized(table, "coinDropChance", 1f);
            SetSerialized(table, "coinMin", 3);
            SetSerialized(table, "coinMax", 7);

            var roll = table.Roll(new SequenceRandom(0f, 0.5f));

            Assert.That(roll.Coins, Is.InRange(3, 7));
        }

        [Test]
        public void Roll_DoesNotReturnCoinsWhenChanceFails()
        {
            var table = ScriptableObject.CreateInstance<LootTable>();
            SetSerialized(table, "coinDropChance", 0.25f);
            SetSerialized(table, "coinMin", 3);
            SetSerialized(table, "coinMax", 7);

            var roll = table.Roll(new SequenceRandom(0.75f));

            Assert.That(roll.Coins, Is.EqualTo(0));
        }

        [Test]
        public void Roll_ReturnsItemStackWhenEntryChanceSucceeds()
        {
            var item = CreateItem("test_drop", maxStack: 10);
            var table = ScriptableObject.CreateInstance<LootTable>();
            SetSerialized(table, "coinDropChance", 0f);
            SetSerialized(table, "drops", new[] { CreateEntry(item, 1f, 2, 4) });

            var roll = table.Roll(new SequenceRandom(1f, 0f, 0.5f));

            Assert.That(roll.Items.Count, Is.EqualTo(1));
            Assert.That(roll.Items[0].Item, Is.EqualTo(item));
            Assert.That(roll.Items[0].Amount, Is.InRange(2, 4));
        }

        private static ItemDefinition CreateItem(string itemId, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            SetSerialized(item, "itemId", itemId);
            SetSerialized(item, "displayName", itemId);
            SetSerialized(item, "maxStack", maxStack);
            return item;
        }

        private static LootEntry CreateEntry(ItemDefinition item, float chance, int min, int max)
        {
            var entry = new LootEntry();
            var boxed = (object)entry;
            SetField(boxed, "item", item);
            SetField(boxed, "dropChance", chance);
            SetField(boxed, "minAmount", min);
            SetField(boxed, "maxAmount", max);
            return (LootEntry)boxed;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = typeof(LootEntry).GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field.SetValue(target, value);
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

        private static void SetSerialized(Object target, string fieldName, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(fieldName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerialized(Object target, string fieldName, LootEntry[] value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(fieldName);
            property.arraySize = value.Length;

            for (var i = 0; i < value.Length; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = value[i].Item;
                element.FindPropertyRelative("dropChance").floatValue = value[i].DropChance;
                element.FindPropertyRelative("minAmount").intValue = value[i].MinAmount;
                element.FindPropertyRelative("maxAmount").intValue = value[i].MaxAmount;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private sealed class SequenceRandom : IRandomSource
        {
            private readonly float[] _values;
            private int _index;

            public SequenceRandom(params float[] values)
            {
                _values = values;
            }

            public float Next01()
            {
                if (_values.Length == 0)
                    return 0f;

                var value = _values[Mathf.Min(_index, _values.Length - 1)];
                _index++;
                return value;
            }
        }
    }
}
