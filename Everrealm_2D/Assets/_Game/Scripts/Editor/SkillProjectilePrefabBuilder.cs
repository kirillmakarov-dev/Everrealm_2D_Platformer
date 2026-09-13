#if UNITY_EDITOR
using System;
using System.Linq;
using LetterHunter.Core;
using LetterHunter.Feedback;
using LetterHunter.Skills;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Editor
{
    public static class SkillProjectilePrefabBuilder
    {
        private const string ProjectileFolder = "Assets/_Game/Prefabs/Projectiles/Skills";
        private const string ImpactFolder = "Assets/_Game/Prefabs/Effects/SkillImpacts";
        private const string ProfileFolder = "Assets/_Game/Data/Feedback/SkillImpactProfiles";
        private const string DefaultIconPath = "Assets/_Game/Data/UI/SkillIcons/Icon_DefaultSkill.png";

        private static readonly Color[] Palette =
        {
            new(1f, .35f, .12f), new(.25f, .72f, 1f), new(1f, .78f, .18f),
            new(.72f, .35f, 1f), new(.25f, 1f, .55f), new(1f, .35f, .72f),
            new(.35f, 1f, 1f), new(1f, .55f, .18f), new(.55f, .72f, 1f),
            new(.85f, 1f, .25f), new(1f, .25f, .25f), new(.65f, .45f, 1f)
        };

        [MenuItem("Tools/Everrealm/Combat/Build Individual Skill Projectiles")]
        public static void Build()
        {
            EnsureFolder(ProjectileFolder);
            EnsureFolder(ImpactFolder);
            EnsureFolder(ProfileFolder);

            var defaultIcon = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultIconPath);
            var skillPaths = AssetDatabase.FindAssets("t:SkillDefinition", new[] { "Assets/_Game/Data/Skills" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (var i = 0; i < skillPaths.Length; i++)
            {
                var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(skillPaths[i]);
                if (skill == null) continue;

                var safeName = Sanitize(skill.SkillId);
                var color = Palette[i % Palette.Length];
                var icon = skill.Icon != null ? skill.Icon : defaultIcon;
                var impactPrefab = BuildImpactPrefab(safeName, icon, color, i);
                var profile = BuildImpactProfile(safeName, skill.DisplayName, impactPrefab, color);
                var projectile = BuildProjectilePrefab(safeName, icon, color, i);
                ConfigureSkill(skill, projectile, profile, i);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Built {skillPaths.Length} individual skill projectile prefabs and hit effects.");
        }

        private static GameObject BuildProjectilePrefab(string id, Sprite icon, Color color, int index)
        {
            var root = new GameObject($"Projectile_{id}", typeof(SkillProjectile2D));
            CreateSpriteChild(root.transform, "Glow", icon, new Color(color.r, color.g, color.b, .28f),
                new Vector3(.72f + index % 3 * .08f, .72f + index % 3 * .08f, 1f), 98);
            CreateSpriteChild(root.transform, "Core", icon, color,
                new Vector3(.42f + index % 4 * .04f, .42f + index % 4 * .04f, 1f), 100);

            var projectile = root.GetComponent<SkillProjectile2D>();
            var so = new SerializedObject(projectile);
            so.FindProperty("hitRadius").floatValue = .16f + index % 3 * .025f;
            so.FindProperty("hitLayers").intValue = ~0;
            so.FindProperty("createFallbackVisual").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            var path = $"{ProjectileFolder}/Projectile_{id}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject BuildImpactPrefab(string id, Sprite icon, Color color, int index)
        {
            var root = new GameObject($"Impact_{id}", typeof(ImpactEffectInstance), typeof(SkillImpactBurstVisual));
            var renderers = new SpriteRenderer[3];
            for (var i = 0; i < renderers.Length; i++)
            {
                var tint = Color.Lerp(color, Color.white, i * .18f);
                tint.a = 1f - i * .18f;
                renderers[i] = CreateSpriteChild(root.transform, $"Burst_{i + 1}", icon, tint,
                    Vector3.one * (.28f + i * .12f), 110 + i);
                renderers[i].transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f + index * 13f);
            }

            var visual = root.GetComponent<SkillImpactBurstVisual>();
            var so = new SerializedObject(visual);
            var serializedRenderers = so.FindProperty("renderers");
            serializedRenderers.arraySize = renderers.Length;
            for (var i = 0; i < renderers.Length; i++)
                serializedRenderers.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            so.FindProperty("duration").floatValue = .48f + index % 3 * .07f;
            so.FindProperty("startScale").floatValue = .3f;
            so.FindProperty("endScale").floatValue = 1.2f + index % 4 * .12f;
            so.FindProperty("rotationSpeed").floatValue = 180f + index % 5 * 45f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var path = $"{ImpactFolder}/Impact_{id}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static AttackImpactProfile BuildImpactProfile(string id, string displayName,
            GameObject impactPrefab, Color color)
        {
            var path = $"{ProfileFolder}/{id}_Impact.asset";
            var profile = AssetDatabase.LoadAssetAtPath<AttackImpactProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<AttackImpactProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var so = new SerializedObject(profile);
            so.FindProperty("impactId").stringValue = $"{id}_impact";
            so.FindProperty("displayName").stringValue = $"{displayName} Impact";
            so.FindProperty("floatingTextType").enumValueIndex = ResolveTextType(color);
            so.FindProperty("criticalFloatingTextType").enumValueIndex = (int)FloatingDamageTextType.Critical;
            so.FindProperty("hitEffectPrefab").objectReferenceValue = impactPrefab;
            so.FindProperty("hitEffectOffset").vector2Value = new Vector2(0f, .8f);
            so.FindProperty("flipEffectByAttackerDirection").boolValue = true;
            so.FindProperty("effectLifetime").floatValue = .72f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ConfigureSkill(SkillDefinition skill, GameObject projectile,
            AttackImpactProfile profile, int index)
        {
            var so = new SerializedObject(skill);
            so.FindProperty("projectilePrefab").objectReferenceValue = projectile;
            so.FindProperty("impactProfile").objectReferenceValue = profile;
            so.FindProperty("projectileSpeed").floatValue = 10.5f + index % 5 * .85f;
            so.FindProperty("projectileLifetime").floatValue = 2.4f + index % 3 * .2f;

            var tags = DamageTag.Skill | DamageTag.Ranged;
            if (skill.SkillId.Contains("fire", StringComparison.OrdinalIgnoreCase)) tags |= DamageTag.Fire;
            if (skill.SkillId.Contains("throw", StringComparison.OrdinalIgnoreCase) ||
                skill.SkillId.Contains("star", StringComparison.OrdinalIgnoreCase)) tags |= DamageTag.Throw;
            so.FindProperty("projectileDamageTags").intValue = (int)tags;

            var damage = so.FindProperty("baseDamageMultiplier");
            if (damage.floatValue <= 0f)
                damage.floatValue = skill.SkillType == SkillType.Empower ? .85f : .65f;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
        }

        private static SpriteRenderer CreateSpriteChild(Transform parent, string name, Sprite sprite,
            Color color, Vector3 scale, int sortingOrder)
        {
            var child = new GameObject(name, typeof(SpriteRenderer));
            child.transform.SetParent(parent, false);
            child.transform.localScale = scale;
            var renderer = child.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static int ResolveTextType(Color color)
        {
            if (color.r > .8f && color.g < .55f) return (int)FloatingDamageTextType.Fire;
            if (color.g > .8f && color.r < .7f) return (int)FloatingDamageTextType.Poison;
            if (color.b > .8f && color.r < .7f) return (int)FloatingDamageTextType.Ice;
            return (int)FloatingDamageTextType.Skill;
        }

        private static string Sanitize(string value)
        {
            foreach (var invalid in System.IO.Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "skill" : value;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrWhiteSpace(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
#endif
