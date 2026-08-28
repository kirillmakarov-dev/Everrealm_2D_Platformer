#if UNITY_EDITOR
using LetterHunter.Characters;
using LetterHunter.Classes;
using LetterHunter.Debugging;
using LetterHunter.Feedback;
using LetterHunter.Infrastructure;
using LetterHunter.UI.Skills;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LetterHunter.Editor
{
    public static class CharacterFrameworkBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/CharacterFrameworkDebug.unity";
        private const string ConfigPath = "Assets/_Game/Data/Characters/DefaultMovement.asset";

        [InitializeOnLoadMethod]
        private static void BuildOnce()
        {
            if (!System.IO.File.Exists(ScenePath)) EditorApplication.delayCall += Build;
        }

        public static void Build()
        {
            PhysicsLayerSetupBuilder.ConfigureCollisionMatrix();
            CombatFoundationBuilder.Build();
            EnsureFolder("Assets/_Game/Data/Characters");
            var config = CreateMovementConfig();
            var warrior = AssetDatabase.LoadAssetAtPath<ClassDefinition>("Assets/_Game/Data/Classes/Warrior.asset");
            if (warrior == null) { Debug.LogError("Build Session 01 sample assets first."); return; }
            BuildScene(config, warrior);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Character Framework sample created.");
        }

        private static CharacterMovementConfig CreateMovementConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CharacterMovementConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<CharacterMovementConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            var so = new SerializedObject(config);
            so.FindProperty("moveSpeed").floatValue = 6f;
            so.FindProperty("acceleration").floatValue = 45f;
            so.FindProperty("deceleration").floatValue = 55f;
            so.FindProperty("jumpForce").floatValue = 12f;
            so.FindProperty("gravityScale").floatValue = 3f;
            so.FindProperty("airControl").floatValue = .65f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void BuildScene(CharacterMovementConfig config, ClassDefinition warrior)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var cameraObject = CreateInScene("Main Camera", scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;

            var ground = CreateInScene("Ground", scene);
            PhysicsLayerSetupBuilder.AssignLayer(ground, PhysicsLayerSetupBuilder.GroundLayer);
            ground.transform.position = new Vector3(0f, -1f);
            ground.AddComponent<BoxCollider2D>().size = new Vector2(24f, 1f);

            var player = CreateInScene("DebugCharacter_Warrior", scene);
            PhysicsLayerSetupBuilder.AssignLayer(player, PhysicsLayerSetupBuilder.PlayerLayer);
            player.transform.position = Vector3.zero;
            player.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.6f);
            var body = player.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            var provider = player.AddComponent<Physics2DTargetProvider>();
            var combat = player.AddComponent<PlayerClassController>();
            var motor = player.AddComponent<CharacterMotor2D>();
            var groundDetector = player.AddComponent<CharacterGroundDetector>();
            var facingView = player.AddComponent<CharacterFacingView2D>();
            var animation = player.AddComponent<CharacterAnimationController>();
            var input = player.AddComponent<CharacterInputRouter>();
            var hitReaction = player.AddComponent<CharacterHitReaction2D>();
            var root = player.AddComponent<CharacterRoot>();
            player.AddComponent<FloatingDamageTextController>();
            player.AddComponent<PlayerSkillLoadout>();
            player.AddComponent<CharacterDebugOverlay>();
            player.AddComponent<CharacterStatsDebug>();

            var checkPoint = new GameObject("GroundCheck").transform;
            checkPoint.SetParent(player.transform, false);
            checkPoint.localPosition = new Vector3(0f, -.86f);

            var combatSo = new SerializedObject(combat);
            combatSo.FindProperty("classDefinition").objectReferenceValue = warrior;
            combatSo.FindProperty("targetProviderComponent").objectReferenceValue = provider;
            var impactProfiles = AttackImpactSetupBuilder.EnsureDefaultProfiles();
            if (impactProfiles.TryGetValue("NormalSlash", out var normalSlash))
                combatSo.FindProperty("defaultAutoAttackImpact").objectReferenceValue = normalSlash;
            var finisherImpact = impactProfiles.TryGetValue("HeavySlash", out var heavySlash) ? heavySlash : normalSlash;
            var combo = AutoAttackComboSetupBuilder.EnsureWarriorBasicCombo(normalSlash, finisherImpact);
            combatSo.FindProperty("autoAttackCombo").objectReferenceValue = combo;
            combatSo.ApplyModifiedPropertiesWithoutUndo();

            var providerSo = new SerializedObject(provider);
            providerSo.FindProperty("targetLayers").intValue = PhysicsLayerSetupBuilder.EnemyTargetMask.value;
            providerSo.ApplyModifiedPropertiesWithoutUndo();

            var detectorSo = new SerializedObject(groundDetector);
            detectorSo.FindProperty("checkPoint").objectReferenceValue = checkPoint;
            detectorSo.FindProperty("checkRadius").floatValue = .18f;
            detectorSo.ApplyModifiedPropertiesWithoutUndo();

            var rootSo = new SerializedObject(root);
            rootSo.FindProperty("motor").objectReferenceValue = motor;
            rootSo.FindProperty("groundDetector").objectReferenceValue = groundDetector;
            rootSo.FindProperty("facingView").objectReferenceValue = facingView;
            rootSo.FindProperty("animationController").objectReferenceValue = animation;
            rootSo.FindProperty("inputRouter").objectReferenceValue = input;
            rootSo.FindProperty("combatModule").objectReferenceValue = combat;
            rootSo.FindProperty("hitReaction").objectReferenceValue = hitReaction;
            rootSo.FindProperty("movementConfig").objectReferenceValue = config;
            rootSo.ApplyModifiedPropertiesWithoutUndo();

            var inputSo = new SerializedObject(input);
            inputSo.FindProperty("moveLeftKey").enumValueIndex = (int)UnityEngine.InputSystem.Key.A;
            inputSo.FindProperty("moveRightKey").enumValueIndex = (int)UnityEngine.InputSystem.Key.D;
            inputSo.FindProperty("jumpKey").enumValueIndex = (int)UnityEngine.InputSystem.Key.Space;
            inputSo.FindProperty("attackKey").enumValueIndex = (int)UnityEngine.InputSystem.Key.J;
            var skillKeys = inputSo.FindProperty("skillKeys");
            skillKeys.arraySize = 0;
            inputSo.ApplyModifiedPropertiesWithoutUndo();

            for (var i = 0; i < 3; i++)
            {
                var dummy = CreateInScene($"Dummy_{i + 1}", scene);
                PhysicsLayerSetupBuilder.AssignLayer(dummy, PhysicsLayerSetupBuilder.EnemyBodyLayer);
                dummy.transform.position = new Vector3(2f + i * 1.4f, 0f);
                dummy.AddComponent<BoxCollider2D>().size = new Vector2(.8f, 1.6f);
                var enemyBody = dummy.AddComponent<Rigidbody2D>();
                enemyBody.freezeRotation = true;
                dummy.AddComponent<DummyEnemy2D>();
                dummy.AddComponent<CharacterHitReaction2D>();
                dummy.AddComponent<EnemyPatrolAI2D>();
                dummy.AddComponent<FloatingDamageTextController>();
            }

            var floatingTextPrefab = FloatingDamageTextSetupBuilder.EnsurePrefab();
            FloatingDamageTextSetupBuilder.EnsureManager(floatingTextPrefab, scene);
            AttackImpactSetupBuilder.EnsureImpactPool(scene);
            AttackImpactSetupBuilder.AssignProfilesToSkillAssets(impactProfiles);
            var enemyAttack = AttackImpactSetupBuilder.EnsureDefaultEnemyAttack(impactProfiles);
            AttackImpactSetupBuilder.AttachEnemyAttackControllers(enemyAttack);
            AutoAttackComboSetupBuilder.AssignComboToPlayers(combo);
            SkillBarSetupBuilder.SetupScene(scene);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
        }

        private static GameObject CreateInScene(string name, UnityEngine.SceneManagement.Scene scene)
        {
            var instance = new GameObject(name);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, scene);
            return instance;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
#endif
