using LetterHunter.Items;
using LetterHunter.Skills;
using LetterHunter.UI.Skills;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LetterHunter.Tests.EditMode
{
    public sealed class PlayerSkillLoadoutTests
    {
        [Test]
        public void RemoveSkill_RemovesExplicitBindingAndPublishesOneChange()
        {
            var go = new GameObject("Loadout");
            var loadout = go.AddComponent<PlayerSkillLoadout>();
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var so = new UnityEditor.SerializedObject(skill);
            so.FindProperty("skillId").stringValue = "refund_skill";
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(loadout.TryAssignSkill(0, skill, out _), Is.True);
            var changes = 0;
            loadout.LoadoutChanged += () => changes++;

            Assert.That(loadout.RemoveSkill(skill.SkillId), Is.True);

            Assert.That(loadout.Slots.Count, Is.EqualTo(0));
            Assert.That(changes, Is.EqualTo(1));
            Object.DestroyImmediate(skill);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryAssignSkill_RejectsDuplicateSkillInAnotherSlot()
        {
            var go = new GameObject("Loadout");
            var loadout = go.AddComponent<PlayerSkillLoadout>();
            var skill = CreateSkill("focus_slash", "Focus Slash");

            Assert.That(loadout.TryAssignSkill(0, skill, out var firstFailure), Is.True);
            Assert.That(firstFailure, Is.Null);

            Assert.That(loadout.TryAssignSkill(1, skill, out var secondFailure), Is.False);
            Assert.That(secondFailure, Does.Contain("already"));
            Assert.That(loadout.Slots.Count, Is.EqualTo(1));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(skill);
        }

        [Test]
        public void TryAssignSkill_ReplacesExistingSkillInSameSlot()
        {
            var go = new GameObject("Loadout");
            var loadout = go.AddComponent<PlayerSkillLoadout>();
            var first = CreateSkill("focus_slash", "Focus Slash");
            var second = CreateSkill("guard_break", "Guard Break");

            Assert.That(loadout.TryAssignSkill(0, first, out _), Is.True);
            Assert.That(loadout.TryAssignSkill(0, second, out var failure), Is.True);

            Assert.That(failure, Is.Null);
            Assert.That(loadout.Slots.Count, Is.EqualTo(1));
            Assert.That(loadout.Slots[0].Skill, Is.EqualTo(second));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void TrySwapSlots_SwapsSkillsAndPublishesOneChange()
        {
            var go = new GameObject("Loadout");
            var loadout = go.AddComponent<PlayerSkillLoadout>();
            var first = CreateSkill("first", "First");
            var second = CreateSkill("second", "Second");
            Assert.That(loadout.TryAssignSkill(0, first, out _), Is.True);
            Assert.That(loadout.TryAssignSkill(1, second, out _), Is.True);
            var changes = 0;
            loadout.LoadoutChanged += () => changes++;

            Assert.That(loadout.TrySwapSlots(0, 1, first, second, out var failure), Is.True);

            Assert.That(failure, Is.Null);
            Assert.That(loadout.Slots[0].Skill, Is.EqualTo(second));
            Assert.That(loadout.Slots[1].Skill, Is.EqualTo(first));
            Assert.That(changes, Is.EqualTo(1));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        [Test]
        public void TrySwapSlots_MovingToEmptySlotKeepsSourceExplicitlyEmpty()
        {
            var go = new GameObject("Loadout");
            var loadout = go.AddComponent<PlayerSkillLoadout>();
            var skill = CreateSkill("movable", "Movable");
            Assert.That(loadout.TryAssignSkill(0, skill, out _), Is.True);

            Assert.That(loadout.TrySwapSlots(0, 3, skill, null, out _), Is.True);

            Assert.That(loadout.ResolveSkill(null, 0, out _), Is.Null);
            Assert.That(loadout.Slots[1].Skill, Is.EqualTo(skill));
            Assert.That(loadout.Slots.Count, Is.EqualTo(2));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(skill);
        }

        [Test]
        public void TrySwapConsumableSlots_SwapsItemsAndPublishesOneChange()
        {
            var go = new GameObject("Skill Bar");
            var bar = go.AddComponent<SkillBarPresenter>();
            var healthPotion = CreateConsumable("health_potion", "Health Potion");
            var energyPotion = CreateConsumable("energy_potion", "Energy Potion");
            Assert.That(bar.SetConsumableSlot(0, healthPotion), Is.True);
            Assert.That(bar.SetConsumableSlot(1, energyPotion), Is.True);
            var changes = 0;
            bar.ConsumableSlotsChanged += () => changes++;

            Assert.That(bar.TrySwapConsumableSlots(0, 1), Is.True);

            Assert.That(bar.GetConsumableSlot(0), Is.SameAs(energyPotion));
            Assert.That(bar.GetConsumableSlot(1), Is.SameAs(healthPotion));
            Assert.That(changes, Is.EqualTo(1));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(healthPotion);
            Object.DestroyImmediate(energyPotion);
        }

        [Test]
        public void ConsumableSlot_BeginsLeftButtonDrag()
        {
            var go = new GameObject("Consumable Slot");
            var view = go.AddComponent<SkillSlotView>();
            var potion = CreateConsumable("health_potion", "Health Potion");
            var draggedIndex = -1;
            view.DragBegan += (index, _) => draggedIndex = index;
            view.RenderConsumable(potion, 2, "1");

            view.OnBeginDrag(new PointerEventData(null)
            {
                button = PointerEventData.InputButton.Left
            });

            Assert.That(draggedIndex, Is.EqualTo(0));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(potion);
        }

        private static ItemDefinition CreateConsumable(string id, string displayName)
        {
            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            var so = new SerializedObject(item);
            so.FindProperty("itemId").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("itemType").enumValueIndex = (int)ItemType.Consumable;
            so.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static SkillDefinition CreateSkill(string id, string displayName)
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var so = new SerializedObject(skill);
            so.FindProperty("skillId").stringValue = id;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("shortName").stringValue = displayName;
            so.ApplyModifiedPropertiesWithoutUndo();
            return skill;
        }
    }
}
