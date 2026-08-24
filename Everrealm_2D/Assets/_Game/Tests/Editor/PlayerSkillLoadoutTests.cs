using LetterHunter.Skills;
using LetterHunter.UI.Skills;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
