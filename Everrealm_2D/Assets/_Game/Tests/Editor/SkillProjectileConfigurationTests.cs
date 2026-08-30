using System.Collections.Generic;
using LetterHunter.Core;
using LetterHunter.Skills;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Tests.EditMode
{
    public sealed class SkillProjectileConfigurationTests
    {
        [Test]
        public void EverySkill_HasAnEditableUniqueProjectileAndImpactPrefab()
        {
            var projectilePaths = new HashSet<string>();
            var skillGuids = AssetDatabase.FindAssets("t:SkillDefinition", new[] { "Assets/_Game/Data/Skills" });
            Assert.That(skillGuids.Length, Is.GreaterThan(0));

            foreach (var guid in skillGuids)
            {
                var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(skill.ProjectilePrefab, Is.Not.Null, skill.name);
                var projectilePath = AssetDatabase.GetAssetPath(skill.ProjectilePrefab);
                Assert.That(projectilePath, Does.StartWith("Assets/_Game/Prefabs/Projectiles/Skills/"), skill.name);
                Assert.That(projectilePaths.Add(projectilePath), Is.True, $"Projectile is shared by more than one skill: {projectilePath}");
                Assert.That(skill.ProjectilePrefab.GetComponent<SkillProjectile2D>(), Is.Not.Null, skill.name);
                Assert.That(skill.ProjectilePrefab.GetComponentInChildren<SpriteRenderer>(true), Is.Not.Null, skill.name);

                Assert.That(skill.ImpactProfile, Is.Not.Null, skill.name);
                Assert.That(skill.ImpactProfile.HitEffectPrefab, Is.Not.Null, skill.name);
                Assert.That(AssetDatabase.GetAssetPath(skill.ImpactProfile.HitEffectPrefab),
                    Does.StartWith("Assets/_Game/Prefabs/Effects/SkillImpacts/"), skill.name);

                Assert.That(skill.BaseDamageMultiplier, Is.GreaterThan(0f), skill.name);
            }
        }
    }
}
