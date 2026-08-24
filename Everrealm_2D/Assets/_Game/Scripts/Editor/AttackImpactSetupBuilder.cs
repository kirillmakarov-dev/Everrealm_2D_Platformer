#if UNITY_EDITOR
using System.Collections.Generic;
using LetterHunter.Characters;
using LetterHunter.Combat;
using LetterHunter.Debugging;
using LetterHunter.Feedback;
using LetterHunter.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LetterHunter.Editor
{
    public static class AttackImpactSetupBuilder
    {
        private const string ProfileFolder = "Assets/_Game/Data/Feedback/ImpactProfiles";
        private const string EnemyAttackFolder = "Assets/_Game/Data/Enemies/Attacks";

        [MenuItem("Letter Hunter/Setup Attack Impact Feedback")]
        public static void SetupCurrentSceneAndAssets()
        {
            EnsureFolder(ProfileFolder);
            EnsureFolder(EnemyAttackFolder);
            var profiles = EnsureDefaultProfiles();
            var enemyAttack = EnsureDefaultEnemyAttack(profiles);

            FloatingDamageTextSetupBuilder.SetupCurrentScene();
            EnsureImpactPool(SceneManager.GetActiveScene());
            AssignProfilesToSkillAssets(profiles);
            AssignProfilesToPlayers(profiles);
            AttachEnemyAttackControllers(enemyAttack);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Attack Impact Feedback setup complete. Assign or replace VFX prefabs in Assets/_Game/Data/Feedback/ImpactProfiles.");
        }

        public static Dictionary<string, AttackImpactProfile> EnsureDefaultProfiles()
        {
            return new Dictionary<string, AttackImpactProfile>
            {
                ["NormalSlash"] = EnsureProfile("NormalSlash", "normal_slash", "Normal Slash",
                    FloatingDamageTextType.Normal, FloatingDamageTextType.Critical,
                    "Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/00_Prefab/2_Impact_1.1.0/VFX_ImpactClassic01_1.1.0.prefab",
                    new Vector2(0f, 0.8f), 0.9f),
                ["HeavySlash"] = EnsureProfile("HeavySlash", "heavy_slash", "Heavy Slash",
                    FloatingDamageTextType.Skill, FloatingDamageTextType.Critical,
                    "Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/00_Prefab/2_Impact_1.1.0/VFX_ImpactCross_1.1.0.prefab",
                    new Vector2(0f, 0.85f), 1.1f),
                ["FireHit"] = EnsureProfile("FireHit", "fire_hit", "Fire Hit",
                    FloatingDamageTextType.Fire, FloatingDamageTextType.Critical,
                    "Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/00_Prefab/0_Classic/VFX_Arcade_01.prefab",
                    new Vector2(0f, 0.8f), 1.1f),
                ["PoisonHit"] = EnsureProfile("PoisonHit", "poison_hit", "Poison Hit",
                    FloatingDamageTextType.Poison, FloatingDamageTextType.Critical,
                    "Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/00_Prefab/0_Classic/VFX_Poison_01.prefab",
                    new Vector2(0f, 0.8f), 1.25f),
                ["ThrowHit"] = EnsureProfile("ThrowHit", "throw_hit", "Throw Hit",
                    FloatingDamageTextType.Skill, FloatingDamageTextType.Critical,
                    "Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/00_Prefab/2_Impact_1.1.0/VFX_ImpactToon_1.1.0.prefab",
                    new Vector2(0f, 0.8f), 0.9f),
                ["EnemyHit"] = EnsureProfile("EnemyHit", "enemy_hit", "Enemy Hit",
                    FloatingDamageTextType.Normal, FloatingDamageTextType.Critical,
                    "Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/00_Prefab/0_Classic/VFX_Classic_01.prefab",
                    new Vector2(0f, 0.8f), 0.9f)
            };
        }

        public static ImpactEffectPool EnsureImpactPool(Scene scene)
        {
            var pool = FindImpactPoolInScene(scene);
            if (pool == null)
            {
                var go = new GameObject("ImpactEffectPool");
                if (scene.IsValid())
                    SceneManager.MoveGameObjectToScene(go, scene);
                pool = go.AddComponent<ImpactEffectPool>();
                Undo.RegisterCreatedObjectUndo(go, "Create Impact Effect Pool");
            }

            EditorSceneManager.MarkSceneDirty(pool.gameObject.scene);
            return pool;
        }

        public static EnemyAttackDefinition EnsureDefaultEnemyAttack(IReadOnlyDictionary<string, AttackImpactProfile> profiles)
        {
            var path = $"{EnemyAttackFolder}/EnemyBasicHit.asset";
            var definition = AssetDatabase.LoadAssetAtPath<EnemyAttackDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyAttackDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            profiles.TryGetValue("EnemyHit", out var enemyHit);
            var so = new SerializedObject(definition);
            so.FindProperty("attackId").stringValue = "enemy_basic_hit";
            so.FindProperty("displayName").stringValue = "Enemy Basic Hit";
            so.FindProperty("baseDamage").floatValue = 8f;
            so.FindProperty("damageMultiplier").floatValue = 1f;
            so.FindProperty("damageLines").intValue = 1;
            so.FindProperty("cooldown").floatValue = 1.25f;
            so.FindProperty("impactProfile").objectReferenceValue = enemyHit;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        public static void AssignProfilesToPlayers(IReadOnlyDictionary<string, AttackImpactProfile> profiles)
        {
            if (!profiles.TryGetValue("NormalSlash", out var normalSlash)) return;

            foreach (var player in Object.FindObjectsByType<PlayerClassController>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(player);
                var property = so.FindProperty("defaultAutoAttackImpact");
                if (property != null && property.objectReferenceValue == null)
                {
                    property.objectReferenceValue = normalSlash;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(player);
                }
            }
        }

        public static void AssignProfilesToSkillAssets(IReadOnlyDictionary<string, AttackImpactProfile> profiles)
        {
            var guids = AssetDatabase.FindAssets("t:SkillDefinition", new[] { "Assets/_Game/Data/Skills" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path);
                if (skill == null) continue;

                var profile = SelectProfileForSkill(skill, profiles);
                if (profile == null) continue;

                var so = new SerializedObject(skill);
                var property = so.FindProperty("impactProfile");
                if (property != null && property.objectReferenceValue == null)
                {
                    property.objectReferenceValue = profile;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(skill);
                }
            }
        }

        public static void AttachEnemyAttackControllers(EnemyAttackDefinition defaultAttack)
        {
            foreach (var enemy in Object.FindObjectsByType<DummyEnemy2D>(FindObjectsSortMode.None))
            {
                var controller = enemy.GetComponent<EnemyAttackController2D>();
                if (controller == null)
                    controller = Undo.AddComponent<EnemyAttackController2D>(enemy.gameObject);

                var patrol = enemy.GetComponent<EnemyPatrolAI2D>();
                if (patrol == null)
                    patrol = Undo.AddComponent<EnemyPatrolAI2D>(enemy.gameObject);

                var body = enemy.GetComponent<Rigidbody2D>();
                if (body == null)
                    body = Undo.AddComponent<Rigidbody2D>(enemy.gameObject);
                body.freezeRotation = true;

                var so = new SerializedObject(controller);
                var attacker = so.FindProperty("attackerComponent");
                var attack = so.FindProperty("attackDefinition");
                var autoAttack = so.FindProperty("autoAttack");

                if (attacker != null && attacker.objectReferenceValue == null)
                    attacker.objectReferenceValue = enemy;
                if (attack != null && attack.objectReferenceValue == null)
                    attack.objectReferenceValue = defaultAttack;
                if (autoAttack != null)
                    autoAttack.boolValue = false;
                var targetLayers = so.FindProperty("targetLayers");
                if (targetLayers != null)
                    targetLayers.intValue = PhysicsLayerSetupBuilder.PlayerMask.value;

                so.ApplyModifiedPropertiesWithoutUndo();

                var patrolSo = new SerializedObject(patrol);
                var patrolTargetLayers = patrolSo.FindProperty("targetLayers");
                if (patrolTargetLayers != null)
                    patrolTargetLayers.intValue = PhysicsLayerSetupBuilder.PlayerMask.value;
                patrolSo.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(patrol);
                EditorUtility.SetDirty(body);
            }
        }

        private static AttackImpactProfile SelectProfileForSkill(SkillDefinition skill, IReadOnlyDictionary<string, AttackImpactProfile> profiles)
        {
            var id = $"{skill.SkillId} {skill.DisplayName} {skill.name}".ToLowerInvariant();

            if (id.Contains("fire") && profiles.TryGetValue("FireHit", out var fire)) return fire;
            if ((id.Contains("iron") || id.Contains("strength") || id.Contains("heavy")) &&
                profiles.TryGetValue("HeavySlash", out var heavy)) return heavy;
            if ((id.Contains("ground") || id.Contains("slam")) &&
                profiles.TryGetValue("HeavySlash", out var slam)) return slam;
            if ((id.Contains("star") || id.Contains("throw")) &&
                profiles.TryGetValue("ThrowHit", out var thrown)) return thrown;
            if (id.Contains("poison") && profiles.TryGetValue("PoisonHit", out var poison)) return poison;
            if (profiles.TryGetValue("NormalSlash", out var normal)) return normal;

            return null;
        }

        private static AttackImpactProfile EnsureProfile(string assetName, string impactId, string displayName,
            FloatingDamageTextType textType, FloatingDamageTextType criticalTextType, string prefabPath,
            Vector2 effectOffset, float lifetime)
        {
            var path = $"{ProfileFolder}/{assetName}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<AttackImpactProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<AttackImpactProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var so = new SerializedObject(profile);
            so.FindProperty("impactId").stringValue = impactId;
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("floatingTextType").enumValueIndex = (int)textType;
            so.FindProperty("criticalFloatingTextType").enumValueIndex = (int)criticalTextType;
            so.FindProperty("hitEffectPrefab").objectReferenceValue = prefab;
            so.FindProperty("hitEffectOffset").vector2Value = effectOffset;
            so.FindProperty("effectLifetime").floatValue = lifetime;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);

            return profile;
        }

        private static ImpactEffectPool FindImpactPoolInScene(Scene scene)
        {
            if (!scene.IsValid()) return Object.FindFirstObjectByType<ImpactEffectPool>();

            foreach (var root in scene.GetRootGameObjects())
            {
                var pool = root.GetComponentInChildren<ImpactEffectPool>(true);
                if (pool != null) return pool;
            }

            return null;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
#endif
