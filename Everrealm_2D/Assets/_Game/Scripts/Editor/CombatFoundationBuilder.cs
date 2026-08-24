#if UNITY_EDITOR
using System.Collections.Generic;
using LetterHunter.Characters;
using LetterHunter.Classes;
using LetterHunter.Core;
using LetterHunter.Debugging;
using LetterHunter.Effects;
using LetterHunter.Infrastructure;
using LetterHunter.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LetterHunter.Editor
{
    public static class CombatFoundationBuilder
    {
        private const string DataRoot = "Assets/_Game/Data";

        [InitializeOnLoadMethod]
        private static void BuildSampleOnce()
        {
            if (!System.IO.File.Exists("Assets/_Game/Scenes/CombatFoundationDebug.unity"))
                EditorApplication.delayCall += Build;
        }

        public static void Build()
        {
            PhysicsLayerSetupBuilder.ConfigureCollisionMatrix();
            EnsureFolders();
            var warrior = BuildWarrior();
            BuildNinja();
            BuildScene(warrior);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Letter Hunter combat sample assets and scene created.");
        }

        private static ClassDefinition BuildWarrior()
        {
            var iron = CreateEffect<EmpowerEffectDefinition>("Effects/Warrior_IronStrength", so =>
            { Set(so, "damageMultiplier", 1.6f); Set(so, "damageLines", 3); Set(so, "maxTargets", 1); SetShape(so, "attackShape", AttackShapeType.DirectionalBox, new Vector2(1.8f, 1.2f), 0f, .9f); });
            var fire = CreateEffect<EmpowerEffectDefinition>("Effects/Warrior_FireSpin", so =>
            { Set(so, "damageMultiplier", .8f); Set(so, "damageLines", 1); Set(so, "maxTargets", 5); SetShape(so, "attackShape", AttackShapeType.Circle, Vector2.zero, 2.2f, 0f); Set(so, "additionalTags", (int)DamageTag.Fire); });
            var slam = CreateEffect<DamageSkillEffectDefinition>("Effects/Warrior_GroundSlam", so =>
            { SetShape(so, "attackShape", AttackShapeType.Circle, Vector2.zero, 2.2f, 0f); Set(so, "damageTags", (int)(DamageTag.Skill | DamageTag.Melee)); });
            var focus = CreateEffect<BuffEffectDefinition>("Effects/Warrior_ExtremeFocus", so => { Set(so, "stat", 0); Set(so, "amount", 8f); });
            var training = CreateEffect<PeriodicHealPassiveDefinition>("Effects/Warrior_SpecialTraining", so => { Set(so, "interval", 5f); Set(so, "healAmount", 5f); });
            var combo = CreateEffect<AutoAttackUpgradeEffectDefinition>("Effects/Warrior_ComboMaster", so =>
            { Set(so, "damageMultiplier", .5f); Set(so, "damageLines", 3); Set(so, "maxTargets", 1); SetShape(so, "shape", AttackShapeType.DirectionalBox, new Vector2(1.8f, 1.2f), 0f, .9f); Set(so, "tags", (int)(DamageTag.AutoAttack | DamageTag.Melee)); });

            var skills = new List<SkillDefinition>
            {
                CreateSkill("Warrior/IronStrength", "warrior_iron_strength", "Iron Strength", SkillType.Empower, 8, 4, 0, 1, 3, 1, iron),
                CreateSkill("Warrior/FireSpin", "warrior_fire_spin", "Fire Spin", SkillType.Empower, 12, 6, 0, .8f, 1, 5, fire),
                CreateSkill("Warrior/GroundSlam", "warrior_ground_slam", "Ground Slam", SkillType.Active, 10, 6, 0, 1, 3, 3, slam),
                CreateSkill("Warrior/ExtremeFocus", "warrior_extreme_focus", "Extreme Focus", SkillType.Buff, 10, 10, 6, 0, 1, 1, focus),
                CreateSkill("Warrior/SpecialTraining", "warrior_special_training", "Special Training", SkillType.Passive, 0, 0, 0, 0, 1, 1, training),
                CreateSkill("Warrior/ComboMaster", "warrior_combo_master", "Combo Master", SkillType.AutoAttackUpgrade, 0, 0, 0, .5f, 3, 1, combo)
            };
            return CreateClass("Warrior", CharacterClassType.Warrior, "Warrior", 140, 60, 18, 5, 1, 5, skills);
        }

        private static ClassDefinition BuildNinja()
        {
            var star = CreateEffect<DamageSkillEffectDefinition>("Effects/Ninja_BouncingStar", so =>
            { SetShape(so, "attackShape", AttackShapeType.DirectionalBox, new Vector2(6f, 1.5f), 0f, 3f); Set(so, "damageTags", (int)(DamageTag.Skill | DamageTag.Ranged | DamageTag.Throw)); });
            var speed = CreateEffect<AutoAttackUpgradeEffectDefinition>("Effects/Ninja_SpeedThrow", so =>
            { Set(so, "damageMultiplier", .45f); Set(so, "damageLines", 3); Set(so, "maxTargets", 1); SetShape(so, "shape", AttackShapeType.DirectionalBox, new Vector2(4f, 1.2f), 0f, 2f); Set(so, "tags", (int)(DamageTag.AutoAttack | DamageTag.Ranged | DamageTag.Throw)); });
            var sense = CreateEffect<BuffEffectDefinition>("Effects/Ninja_SixthSense", so => { Set(so, "stat", 2); Set(so, "amount", .75f); });
            var shadow = CreateEffect<ExtraDamageLinePassiveDefinition>("Effects/Ninja_ShadowTraining", so => { Set(so, "chance", .25f); Set(so, "extraLines", 1); });
            var armor = CreateEffect<DefensePassiveDefinition>("Effects/Ninja_ArmorMastery", so => Set(so, "defenseBonus", 5f));

            var skills = new List<SkillDefinition>
            {
                CreateSkill("Ninja/BouncingStar", "ninja_bouncing_star", "Bouncing Star", SkillType.Active, 10, 3, 0, .8f, 1, 3, star),
                CreateSkill("Ninja/SpeedThrow", "ninja_speed_throw", "Speed Throw", SkillType.AutoAttackUpgrade, 0, 0, 0, .45f, 3, 1, speed),
                CreateSkill("Ninja/SixthSense", "ninja_sixth_sense", "Sixth Sense", SkillType.Buff, 8, 9, 6, 0, 1, 1, sense),
                CreateSkill("Ninja/ShadowTraining", "ninja_shadow_training", "Shadow Training", SkillType.Passive, 0, 0, 0, 0, 1, 1, shadow),
                CreateSkill("Ninja/ArmorMastery", "ninja_armor_mastery", "Armor Mastery", SkillType.Passive, 0, 0, 0, 0, 1, 1, armor)
            };
            return CreateClass("Ninja", CharacterClassType.Ninja, "Ninja", 100, 90, 15, 3, 1.5f, 6.5f, skills);
        }

        private static void BuildScene(ClassDefinition warrior)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var cameraObject = new GameObject("Main Camera");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<Camera>().orthographic = true;
            var player = new GameObject("Player_Warrior");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(player, scene);
            PhysicsLayerSetupBuilder.AssignLayer(player, PhysicsLayerSetupBuilder.PlayerLayer);
            player.AddComponent<BoxCollider2D>();
            var provider = player.AddComponent<Physics2DTargetProvider>();
            var controller = player.AddComponent<PlayerClassController>();
            player.AddComponent<PlayerDebugInput2D>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("classDefinition").objectReferenceValue = warrior;
            serialized.FindProperty("targetProviderComponent").objectReferenceValue = provider;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var providerSerialized = new SerializedObject(provider);
            providerSerialized.FindProperty("targetLayers").intValue = PhysicsLayerSetupBuilder.EnemyTargetMask.value;
            providerSerialized.ApplyModifiedPropertiesWithoutUndo();

            for (var i = 0; i < 4; i++)
            {
                var dummy = new GameObject($"Dummy_{i + 1}");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(dummy, scene);
                PhysicsLayerSetupBuilder.AssignLayer(dummy, PhysicsLayerSetupBuilder.EnemyBodyLayer);
                dummy.transform.position = new Vector3(1.5f + i * 1.25f, i % 2 == 0 ? 0f : 1f, 0f);
                dummy.AddComponent<BoxCollider2D>();
                dummy.AddComponent<DummyEnemy2D>();
            }
            EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/CombatFoundationDebug.unity");
            EditorSceneManager.CloseScene(scene, true);
        }

        private static SkillDefinition CreateSkill(string path, string id, string name, SkillType type, float mana,
            float cooldown, float duration, float multiplier, int lines, int targets, SkillEffectDefinition effect)
        {
            var asset = LoadOrCreate<SkillDefinition>($"Skills/{path}");
            var so = new SerializedObject(asset);
            Set(so, "skillId", id); Set(so, "displayName", name); Set(so, "description", $"Foundation implementation of {name}.");
            Set(so, "classType", path.StartsWith("Warrior") ? 0 : 1); Set(so, "skillType", (int)type);
            Set(so, "manaCost", mana); Set(so, "cooldown", cooldown); Set(so, "duration", duration);
            Set(so, "baseDamageMultiplier", multiplier); Set(so, "damageLines", lines); Set(so, "maxTargets", targets);
            so.FindProperty("effect").objectReferenceValue = effect; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ClassDefinition CreateClass(string path, CharacterClassType type, string name, float hp, float mana,
            float attack, float defense, float attackSpeed, float moveSpeed, List<SkillDefinition> skills)
        {
            var asset = LoadOrCreate<ClassDefinition>($"Classes/{path}"); var so = new SerializedObject(asset);
            Set(so, "classType", (int)type); Set(so, "displayName", name);
            var stats = so.FindProperty("baseStats"); stats.FindPropertyRelative("maxHealth").floatValue = hp; stats.FindPropertyRelative("maxMana").floatValue = mana;
            stats.FindPropertyRelative("attackPower").floatValue = attack; stats.FindPropertyRelative("defense").floatValue = defense;
            stats.FindPropertyRelative("attackSpeed").floatValue = attackSpeed; stats.FindPropertyRelative("moveSpeed").floatValue = moveSpeed;
            var list = so.FindProperty("startingSkills"); list.arraySize = skills.Count;
            for (var i = 0; i < skills.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset); return asset;
        }

        private static T CreateEffect<T>(string path, System.Action<SerializedObject> configure) where T : ScriptableObject
        { var asset = LoadOrCreate<T>(path); var so = new SerializedObject(asset); configure(so); so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(asset); return asset; }
        private static T LoadOrCreate<T>(string relative) where T : ScriptableObject
        {
            var path = $"{DataRoot}/{relative}.asset"; EnsureAssetFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'));
            var asset = AssetDatabase.LoadAssetAtPath<T>(path); if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static void Set(SerializedObject so, string name, float value) => so.FindProperty(name).floatValue = value;
        private static void Set(SerializedObject so, string name, int value) => so.FindProperty(name).intValue = value;
        private static void Set(SerializedObject so, string name, string value) => so.FindProperty(name).stringValue = value;
        private static void SetShape(SerializedObject so, string name, AttackShapeType type, Vector2 size, float radius, float offset)
        { var p = so.FindProperty(name); p.FindPropertyRelative("type").intValue = (int)type; p.FindPropertyRelative("size").vector2Value = size; p.FindPropertyRelative("radius").floatValue = radius; p.FindPropertyRelative("forwardOffset").floatValue = offset; }
        private static void EnsureFolders() { EnsureAssetFolder(DataRoot); EnsureAssetFolder($"{DataRoot}/Skills"); EnsureAssetFolder($"{DataRoot}/Effects"); EnsureAssetFolder($"{DataRoot}/Classes"); }
        private static void EnsureAssetFolder(string path)
        { if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return; EnsureAssetFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/')); AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/'), System.IO.Path.GetFileName(path)); }
    }
}
#endif
