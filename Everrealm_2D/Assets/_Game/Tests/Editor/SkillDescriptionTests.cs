using LetterHunter.Effects;
using LetterHunter.Skills;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Tests.EditMode
{
    public sealed class SkillDescriptionTests
    {
        [Test]
        public void DescriptionReadsCurrentEffectValues()
        {
            var skill = ScriptableObject.CreateInstance<SkillDefinition>();
            var effect = ScriptableObject.CreateInstance<BuffEffectDefinition>();
            try
            {
                var data = new SerializedObject(skill);
                data.FindProperty("effect").objectReferenceValue = effect;
                data.ApplyModifiedPropertiesWithoutUndo();
                var values = new SerializedObject(effect);
                values.FindProperty("amount").floatValue = 17;
                values.ApplyModifiedPropertiesWithoutUndo();
                StringAssert.Contains("+17", SkillDescription.Build(skill));
                values.FindProperty("amount").floatValue = 23;
                values.ApplyModifiedPropertiesWithoutUndo();
                StringAssert.Contains("+23", SkillDescription.Build(skill));
                StringAssert.DoesNotContain("+17", SkillDescription.Build(skill));
            }
            finally { Object.DestroyImmediate(skill); Object.DestroyImmediate(effect); }
        }
    }
}
