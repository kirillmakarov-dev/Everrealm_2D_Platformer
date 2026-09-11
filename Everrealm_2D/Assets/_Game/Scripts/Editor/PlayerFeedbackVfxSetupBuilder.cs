#if UNITY_EDITOR
using LetterHunter.Characters;
using LetterHunter.Feedback;
using LetterHunter.Stats;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LetterHunter.EditorTools
{
    public static class PlayerFeedbackVfxSetupBuilder
    {
        private const string EffectFolder = "Assets/_Game/Prefabs/Effects/PlayerFeedback";
        private const string MaterialFolder = "Assets/_Game/Materials/PlayerFeedback";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Player.prefab";
        private const string LevelUpBannerPath = "Assets/_Game/Art/UI/Feedback/LevelUpBanner.png";
        private const string CircleTexture = "Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/03_Texture/Circle.png";
        private const string FlareTexture = "Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/03_Texture/Flare.png";
        private const string LiquidTexture = "Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/03_Texture/Liquid.png";

        [MenuItem("Tools/Everrealm/Build Player Feedback VFX")]
        public static void Build()
        {
            EnsureFolder(EffectFolder);
            EnsureFolder(MaterialFolder);

            var circleMaterial = CreateMaterial("PFVFX_Circle", CircleTexture);
            var flareMaterial = CreateMaterial("PFVFX_Flare", FlareTexture);
            var liquidMaterial = CreateMaterial("PFVFX_Liquid", LiquidTexture);
            var levelUpBanner = ConfigureBannerTexture();

            var levelUp = BuildLevelUp(circleMaterial, flareMaterial, levelUpBanner);
            var health = BuildPotion("HealthPotionVfx", new Color(1f, 0.12f, 0.2f, 1f),
                new Color(1f, 0.74f, 0.28f, 1f), circleMaterial, flareMaterial, liquidMaterial);
            var mana = BuildPotion("ManaPotionVfx", new Color(0.08f, 0.45f, 1f, 1f),
                new Color(0.12f, 0.95f, 1f, 1f), circleMaterial, flareMaterial, liquidMaterial);

            WirePlayerPrefab(levelUp, health, mana);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Player feedback VFX prefabs built and wired to Player.prefab.");
        }

        private static GameObject BuildLevelUp(Material circleMaterial, Material flareMaterial, Sprite bannerSprite)
        {
            var root = new GameObject("LevelUpVfx");
            CreateBurst(root.transform, "Golden Halo", circleMaterial,
                new Color(1f, 1f, 0.24f, 0.9f), new Color(1f, 0.82f, 0.12f, 0f),
                2, 1.25f, 0f, 3.8f, 5.2f, ParticleSystemShapeType.Circle, 0.05f, 80,
                AnimationCurve.EaseInOut(0f, 0.18f, 1f, 1.35f));
            CreateBurst(root.transform, "Ascension Flash", flareMaterial,
                new Color(1f, 1f, 0.72f, 1f), new Color(1f, 0.72f, 0.12f, 0f),
                2, 0.75f, 0f, 2.8f, 4.2f, ParticleSystemShapeType.Circle, 0.05f, 86,
                AnimationCurve.EaseInOut(0f, 0.2f, 1f, 0f));
            CreateBurst(root.transform, "Ascending Rays", flareMaterial,
                new Color(1f, 0.94f, 0.2f, 0.95f), new Color(1f, 1f, 0.64f, 0f),
                28, 1.5f, 2.8f, 0.38f, 0.85f, ParticleSystemShapeType.Cone, 0.9f, 82,
                AnimationCurve.EaseInOut(0f, 0.35f, 1f, 0f), 15f);
            CreateBurst(root.transform, "Crown Sparkles", flareMaterial,
                new Color(1f, 0.95f, 0.52f, 1f), new Color(1f, 0.55f, 0.08f, 0f),
                20, 1.1f, 3.2f, 0.2f, 0.48f, ParticleSystemShapeType.Circle, 0.9f, 84,
                AnimationCurve.EaseInOut(0f, 0.5f, 1f, 0f));
            BuildScreenBanner(root.transform, bannerSprite);
            return SavePrefab(root, $"{EffectFolder}/LevelUpVfx.prefab");
        }

        private static void BuildScreenBanner(Transform parent, Sprite bannerSprite)
        {
            if (bannerSprite == null)
                throw new System.InvalidOperationException($"Missing level-up banner sprite: {LevelUpBannerPath}");

            var canvasObject = new GameObject("Screen Feedback", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasGroup), typeof(CanvasScaler), typeof(LevelUpBannerView));
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 240;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var group = canvasObject.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var bannerObject = new GameObject("Level Up Banner", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image));
            bannerObject.transform.SetParent(canvasObject.transform, false);
            var rect = bannerObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-760f, 220f);
            rect.sizeDelta = new Vector2(920f, 345f);

            var image = bannerObject.GetComponent<Image>();
            image.sprite = bannerSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = Color.white;

            var view = canvasObject.GetComponent<LevelUpBannerView>();
            var viewObject = new SerializedObject(view);
            viewObject.FindProperty("canvasGroup").objectReferenceValue = group;
            viewObject.FindProperty("banner").objectReferenceValue = rect;
            viewObject.FindProperty("hiddenLeft").vector2Value = new Vector2(-760f, 220f);
            viewObject.FindProperty("shown").vector2Value = new Vector2(0f, 220f);
            viewObject.FindProperty("hiddenRight").vector2Value = new Vector2(760f, 220f);
            viewObject.FindProperty("fadeInDuration").floatValue = 0.35f;
            viewObject.FindProperty("holdDuration").floatValue = 0.95f;
            viewObject.FindProperty("fadeOutDuration").floatValue = 0.45f;
            viewObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite ConfigureBannerTexture()
        {
            var importer = AssetImporter.GetAtPath(LevelUpBannerPath) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException($"Missing level-up banner texture: {LevelUpBannerPath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(LevelUpBannerPath);
        }

        private static GameObject BuildPotion(string name, Color primary, Color accent,
            Material circleMaterial, Material flareMaterial, Material liquidMaterial)
        {
            var root = new GameObject(name);
            CreateBurst(root.transform, "Restoration Ring", circleMaterial, primary,
                new Color(accent.r, accent.g, accent.b, 0f), 2, 0.9f, 0f, 2f, 2.9f,
                ParticleSystemShapeType.Circle, 0.05f, 80,
                AnimationCurve.EaseInOut(0f, 0.25f, 1f, 1.25f));
            CreateBurst(root.transform, "Rising Essence", liquidMaterial, primary,
                new Color(accent.r, accent.g, accent.b, 0f), 18, 1.25f, 1.45f, 0.18f, 0.42f,
                ParticleSystemShapeType.Cone, 0.65f, 82,
                AnimationCurve.EaseInOut(0f, 0.4f, 1f, 0f), 20f, 3, 4);
            CreateBurst(root.transform, "Potion Sparkles", flareMaterial, accent,
                new Color(primary.r, primary.g, primary.b, 0f), 12, 0.9f, 1.75f, 0.1f, 0.24f,
                ParticleSystemShapeType.Circle, 0.5f, 84,
                AnimationCurve.EaseInOut(0f, 0.7f, 1f, 0f));
            return SavePrefab(root, $"{EffectFolder}/{name}.prefab");
        }

        private static ParticleSystem CreateBurst(Transform parent, string name, Material material,
            Color startColor, Color endColor, int count, float lifetime, float speed,
            float minSize, float maxSize, ParticleSystemShapeType shapeType, float radius,
            int sortingOrder, AnimationCurve sizeCurve, float coneAngle = 0f,
            int textureTilesX = 1, int textureTilesY = 1)
        {
            var effectObject = new GameObject(name, typeof(ParticleSystem));
            effectObject.transform.SetParent(parent, false);
            var particle = effectObject.GetComponent<ParticleSystem>();

            var main = particle.main;
            main.duration = Mathf.Max(0.1f, lifetime);
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.65f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.65f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = startColor;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = Mathf.Max(8, count * 2);
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;

            var emission = particle.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = particle.shape;
            shape.enabled = true;
            shape.shapeType = shapeType;
            shape.radius = radius;
            if (shapeType == ParticleSystemShapeType.Cone)
            {
                // In a side-view game, an explicit local Y velocity is more reliable than
                // a 3D cone whose forward axis can disappear into camera depth.
                shape.shapeType = ParticleSystemShapeType.Circle;
                main.startSpeed = 0f;
                var velocity = particle.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
                velocity.y = new ParticleSystem.MinMaxCurve(speed * 0.65f, speed);
                // Unity requires every axis in Velocity over Lifetime to use the same
                // curve mode. Keep Z as Two Constants even though this is a 2D effect.
                velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            }

            var color = particle.colorOverLifetime;
            color.enabled = true;
            color.color = CreateGradient(startColor, endColor);

            var size = particle.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var noise = particle.noise;
            noise.enabled = speed > 0f;
            noise.strength = 0.24f;
            noise.frequency = 0.55f;
            noise.scrollSpeed = 0.35f;

            var rotation = particle.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-1.25f, 1.25f);

            if (textureTilesX > 1 || textureTilesY > 1)
            {
                var sheet = particle.textureSheetAnimation;
                sheet.enabled = true;
                sheet.mode = ParticleSystemAnimationMode.Grid;
                sheet.numTilesX = Mathf.Max(1, textureTilesX);
                sheet.numTilesY = Mathf.Max(1, textureTilesY);
                sheet.animation = ParticleSystemAnimationType.WholeSheet;
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f,
                    AnimationCurve.Linear(0f, 0f, 1f, 1f));
                sheet.cycleCount = 1;
            }

            var renderer = effectObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            return particle;
        }

        private static ParticleSystem.MinMaxGradient CreateGradient(Color start, Color end)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
                new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(gradient);
        }

        private static Material CreateMaterial(string name, string texturePath)
        {
            var path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            // The source pack uses this additive particle shader. Reusing its proven
            // transparent blend path avoids opaque texture quads in the current URP project.
            var shader = Shader.Find("Mobile/Particles/Additive") ??
                         Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null)
                throw new System.InvalidOperationException("No supported unlit particle shader was found.");

            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
                throw new System.InvalidOperationException($"Missing VFX texture: {texturePath}");

            material.mainTexture = texture;
            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            if (prefab == null)
                throw new System.InvalidOperationException($"Could not save VFX prefab: {path}");
            return prefab;
        }

        private static void WirePlayerPrefab(GameObject levelUp, GameObject health, GameObject mana)
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var anchor = root.transform.Find("Player Feedback VFX Anchor");
                if (anchor == null)
                {
                    var anchorObject = new GameObject("Player Feedback VFX Anchor");
                    anchor = anchorObject.transform;
                    anchor.SetParent(root.transform, false);
                }
                anchor.localPosition = new Vector3(0f, 1.15f, 0f);
                anchor.localRotation = Quaternion.identity;
                anchor.localScale = Vector3.one;

                var feedback = root.GetComponent<PlayerFeedbackVfx>() ?? root.AddComponent<PlayerFeedbackVfx>();
                var feedbackObject = new SerializedObject(feedback);
                feedbackObject.FindProperty("progression").objectReferenceValue = root.GetComponent<PlayerLevelProgression>();
                feedbackObject.FindProperty("effectAnchor").objectReferenceValue = anchor;
                feedbackObject.FindProperty("levelUpPrefab").objectReferenceValue = levelUp;
                feedbackObject.FindProperty("healthPotionPrefab").objectReferenceValue = health;
                feedbackObject.FindProperty("manaPotionPrefab").objectReferenceValue = mana;
                feedbackObject.FindProperty("levelUpLifetime").floatValue = 2.2f;
                feedbackObject.FindProperty("potionLifetime").floatValue = 1.5f;
                feedbackObject.ApplyModifiedPropertiesWithoutUndo();

                var player = root.GetComponent<PlayerClassController>();
                if (player == null)
                    throw new System.InvalidOperationException("Player.prefab has no PlayerClassController.");
                var playerObject = new SerializedObject(player);
                playerObject.FindProperty("feedbackVfx").objectReferenceValue = feedback;
                playerObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index++)
            {
                var next = $"{current}/{parts[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
#endif
