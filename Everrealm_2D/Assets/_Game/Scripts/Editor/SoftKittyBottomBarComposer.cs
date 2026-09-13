#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using LetterHunter.Skills;
using LetterHunter.UI.Skills;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LetterHunter.EditorTools
{
    /// <summary>
    /// Authors only the bottom skill bar from the local SoftKitty ActionBar prefab.
    /// The resulting prefab instance and all Everrealm references are saved in the scene.
    /// </summary>
    public static class SoftKittyBottomBarComposer
    {
        private const string ScenePath = "Assets/_Game/Scenes/InventoryLootDebug.unity";
        private const string ActionBarPrefabPath =
            "Assets/SoftKitty/InventoryEngine/Prefabs/Ui/ActionBar.prefab";
        private const string SkillIconFolder =
            "Assets/SoftKitty/InventoryEngine/Textures/SkillIcon";
        private const string ShellName = "SoftKittyBottomSkillBar";

        private static readonly (string assetPath, int iconNumber)[] UsedSkillIcons =
        {
            ("Assets/_Game/Data/Skills/Warrior/IronStrength.asset", 10),
            ("Assets/_Game/Data/Skills/Warrior/FireSpin.asset", 6),
            ("Assets/_Game/Data/Skills/Warrior/GroundSlam.asset", 8),
            ("Assets/_Game/Data/Skills/Warrior/ExtremeFocus.asset", 9)
        };

        [InitializeOnLoadMethod]
        private static void QueueComposition()
        {
            EditorApplication.delayCall += ComposeIfNeeded;
        }

        private static void ComposeIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                return;

            var presenter = UnityEngine.Object.FindFirstObjectByType<SkillBarPresenter>(FindObjectsInactive.Include);
            if (presenter != null && presenter.transform.Find(ShellName) == null)
                Build();
        }

        [MenuItem("Everrealm/Build SoftKitty Bottom Bar", priority = 1)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before building the bottom skill bar.");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                Debug.LogError($"Open {ScenePath} before building the bottom skill bar.");
                return;
            }

            var actionBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActionBarPrefabPath);
            var presenter = UnityEngine.Object.FindFirstObjectByType<SkillBarPresenter>(FindObjectsInactive.Include);
            if (actionBarPrefab == null || presenter == null)
            {
                Debug.LogError("SoftKitty ActionBar prefab or Everrealm SkillBarPresenter is missing.");
                return;
            }

            RegisterSoftKittySettings();
            AssignUsedSkillIcons();
            ComposeBar(presenter, actionBarPrefab);
            RemoveCloseControls();

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("SoftKitty bottom skill bar saved into InventoryLootDebug. Other UI windows were not changed.");
        }

        public static void BuildFromBatchMode()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Build();
        }

        private static void ComposeBar(SkillBarPresenter presenter, GameObject actionBarPrefab)
        {
            var host = (RectTransform)presenter.transform;
            ClearChildren(host);
            foreach (var layout in host.GetComponents<LayoutGroup>())
                UnityEngine.Object.DestroyImmediate(layout, true);
            foreach (var fitter in host.GetComponents<ContentSizeFitter>())
                UnityEngine.Object.DestroyImmediate(fitter, true);
            foreach (var effect in host.GetComponents<BaseMeshEffect>())
                effect.enabled = false;

            var hostImage = host.GetComponent<Image>();
            if (hostImage != null)
            {
                hostImage.enabled = false;
                hostImage.raycastTarget = false;
            }

            host.anchorMin = new Vector2(0.5f, 0f);
            host.anchorMax = new Vector2(0.5f, 0f);
            host.pivot = new Vector2(0.5f, 0f);
            host.anchoredPosition = new Vector2(0f, 8f);
            host.sizeDelta = new Vector2(690f, 150f);
            host.localScale = Vector3.one;

            var shell = PrefabUtility.InstantiatePrefab(actionBarPrefab, host) as GameObject;
            if (shell == null)
                return;
            shell.name = ShellName;
            StripSoftKittyGameplay(shell);

            var shellRect = (RectTransform)shell.transform;
            shellRect.anchorMin = new Vector2(0.5f, 0f);
            shellRect.anchorMax = new Vector2(0.5f, 0f);
            shellRect.pivot = new Vector2(0.5f, 0f);
            shellRect.anchoredPosition = Vector2.zero;
            shellRect.sizeDelta = new Vector2(690f, 150f);
            shellRect.localScale = Vector3.one;

            var slotRoot = FindDeep(shell.transform, "Slots");
            var slotTransforms = slotRoot == null
                ? Array.Empty<Transform>()
                : slotRoot.Cast<Transform>()
                    .Where(child => child.name.StartsWith("Item", StringComparison.Ordinal))
                    .OrderBy(child => child.GetSiblingIndex())
                    .Take(10)
                    .ToArray();

            var views = new List<SkillSlotView>(slotTransforms.Length);
            for (var i = 0; i < slotTransforms.Length; i++)
            {
                var slot = slotTransforms[i];
                slot.name = $"SkillSlot_{i + 1:00}";
                HidePackageSlotNumbers(slot);

                var view = slot.GetComponent<SkillSlotView>() ?? slot.gameObject.AddComponent<SkillSlotView>();
                var button = slot.GetComponent<Button>() ?? slot.gameObject.AddComponent<Button>();
                ResetButton(button);

                DisablePackageIconLayers(slot);
                var packageCooldown = FindImage(slot, "CoolDown");
                if (packageCooldown != null)
                    packageCooldown.enabled = false;

                var icon = CreateSlotImage(slot, "LetterHunterSkillIcon", new Vector2(50f, 50f),
                    new Vector2(0f, 5f));
                icon.enabled = false;
                icon.preserveAspect = true;

                var cooldown = CreateSlotImage(slot, "LetterHunterCooldownOverlay", new Vector2(52f, 52f),
                    new Vector2(0f, 5f));
                cooldown.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                cooldown.color = new Color(0.01f, 0.015f, 0.02f, 0.82f);
                cooldown.type = Image.Type.Filled;
                cooldown.fillMethod = Image.FillMethod.Radial360;
                cooldown.fillOrigin = (int)Image.Origin360.Top;
                cooldown.fillClockwise = false;
                cooldown.fillAmount = 0f;
                cooldown.enabled = false;

                var keyParent = FindDeep(slot, "Key") ?? slot;
                var key = CreateLabel(keyParent, "LetterHunterKey", i == 9 ? "0" : (i + 1).ToString(), 16f);
                var timer = CreateLabel(slot, "LetterHunterCooldown", string.Empty, 17f);
                timer.color = Color.white;
                timer.outlineColor = new Color(0f, 0f, 0f, 0.9f);
                timer.outlineWidth = 0.22f;
                timer.transform.SetAsLastSibling();

                var serializedView = new SerializedObject(view);
                serializedView.FindProperty("button").objectReferenceValue = button;
                serializedView.FindProperty("background").objectReferenceValue = slot.GetComponent<Image>();
                serializedView.FindProperty("icon").objectReferenceValue = icon;
                serializedView.FindProperty("cooldownOverlay").objectReferenceValue = cooldown;
                serializedView.FindProperty("cooldownText").objectReferenceValue = timer;
                serializedView.FindProperty("keyText").objectReferenceValue = key;
                serializedView.FindProperty("nameText").objectReferenceValue = null;
                serializedView.ApplyModifiedPropertiesWithoutUndo();
                views.Add(view);
            }

            ValidateComposedSlots(views);

            ConfigureProgressStrip(shell.transform);
            DisableUnsupportedControls(shell.transform);

            var ghost = CreateGhost(host);
            var serializedPresenter = new SerializedObject(presenter);
            serializedPresenter.FindProperty("slotRoot").objectReferenceValue = slotRoot;
            serializedPresenter.FindProperty("slotPrefab").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<SkillSlotView>("Assets/_Game/Prefabs/UI/SkillSlot.prefab");
            serializedPresenter.FindProperty("maxSlots").intValue = views.Count;
            var authoredSlots = serializedPresenter.FindProperty("authoredSlots");
            authoredSlots.arraySize = views.Count;
            for (var i = 0; i < views.Count; i++)
                authoredSlots.GetArrayElementAtIndex(i).objectReferenceValue = views[i];
            serializedPresenter.FindProperty("assignmentGhost").objectReferenceValue = ghost;
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignUsedSkillIcons()
        {
            foreach (var mapping in UsedSkillIcons)
            {
                var iconPath = $"{SkillIconFolder}/Skill_{mapping.iconNumber}.png";
                PrepareAsSprite(iconPath);
                var skill = AssetDatabase.LoadAssetAtPath<SkillDefinition>(mapping.assetPath);
                var icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                if (skill == null || icon == null)
                    continue;

                var serializedSkill = new SerializedObject(skill);
                serializedSkill.FindProperty("icon").objectReferenceValue = icon;
                serializedSkill.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(skill);
            }
        }

        private static void PrepareAsSprite(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                return;
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single && !importer.mipmapEnabled)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void StripSoftKittyGameplay(GameObject root)
        {
            var gameplayTypes = new HashSet<string>(StringComparer.Ordinal)
            {
                "SoftKitty.InventoryEngine.ActionBarUi",
                "SoftKitty.InventoryEngine.ActionSlot",
                "SoftKitty.InventoryEngine.LinkIcon"
            };

            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null && gameplayTypes.Contains(behaviour.GetType().FullName))
                    UnityEngine.Object.DestroyImmediate(behaviour, true);
            }
        }

        private static void HidePackageSlotNumbers(Transform slot)
        {
            foreach (var text in slot.GetComponentsInChildren<Text>(true))
            {
                var value = text.text?.Trim();
                if (value is "99" or "+3" or "13" or "1")
                    text.enabled = false;
            }
        }

        private static void ConfigureProgressStrip(Transform root)
        {
            var progress = FindDeep(root, "Progress")?.GetComponent<Image>() ??
                           FindDeep(root, "ProgressBar")?.GetComponent<Image>();
            if (progress != null)
            {
                progress.type = Image.Type.Filled;
                progress.fillMethod = Image.FillMethod.Horizontal;
                progress.fillAmount = 1f;
                progress.raycastTarget = false;
            }

            var progressText = root.GetComponentsInChildren<Text>(true)
                .FirstOrDefault(text => text.text != null && text.text.Contains("1000"));
            if (progressText != null)
            {
                progressText.text = "SKILL LOADOUT  •  1–0";
                progressText.raycastTarget = false;
            }
        }

        private static void DisableUnsupportedControls(Transform root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                var name = transform.name;
                if (name.Equals("LockButton", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Lock", StringComparison.OrdinalIgnoreCase))
                    transform.gameObject.SetActive(false);
            }
        }

        private static void RemoveCloseControls()
        {
            foreach (var transform in UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (transform.name.IndexOf("close", StringComparison.OrdinalIgnoreCase) >= 0)
                    transform.gameObject.SetActive(false);
            }

            foreach (var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (string.Equals(text.text?.Trim(), "CLOSE", StringComparison.OrdinalIgnoreCase))
                    text.transform.parent.gameObject.SetActive(false);
            }
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, string value, float fontSize)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(parent, false);
            label.text = value;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = new Color(0.92f, 0.9f, 0.82f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            var rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return label;
        }

        private static Image CreateSlotImage(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            image.transform.SetAsLastSibling();
            return image;
        }

        private static void DisablePackageIconLayers(Transform slot)
        {
            foreach (var image in slot.GetComponentsInChildren<Graphic>(true))
            {
                if (image == null)
                    continue;
                if (image.name.Equals("Icon", StringComparison.OrdinalIgnoreCase) ||
                    image.name.Equals("IconGlow", StringComparison.OrdinalIgnoreCase))
                    image.enabled = false;
            }
        }

        private static void ValidateComposedSlots(IReadOnlyList<SkillSlotView> views)
        {
            if (views == null || views.Count != 10)
                throw new InvalidOperationException("SoftKitty skill bar must contain exactly 10 authored slots.");

            foreach (var view in views)
            {
                var serializedView = new SerializedObject(view);
                var icon = serializedView.FindProperty("icon")?.objectReferenceValue as Image;
                var cooldown = serializedView.FindProperty("cooldownOverlay")?.objectReferenceValue as Image;
                var cooldownText = serializedView.FindProperty("cooldownText")?.objectReferenceValue as TMP_Text;
                if (icon == null || cooldown == null || cooldownText == null)
                    throw new InvalidOperationException($"{view.name} is missing an authored icon or cooldown layer.");
                if (cooldown.type != Image.Type.Filled || cooldown.fillMethod != Image.FillMethod.Radial360)
                    throw new InvalidOperationException($"{view.name} cooldown is not configured as a radial fill.");
            }
        }

        private static Image CreateGhost(Transform host)
        {
            var ghost = new GameObject("DraggedSkillIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                typeof(CanvasGroup));
            ghost.transform.SetParent(host, false);
            var rect = (RectTransform)ghost.transform;
            rect.sizeDelta = new Vector2(58f, 58f);
            var image = ghost.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            ghost.GetComponent<CanvasGroup>().blocksRaycasts = false;
            ghost.transform.SetAsLastSibling();
            return image;
        }

        private static Image FindImage(Transform root, string name)
        {
            return FindDeep(root, name)?.GetComponent<Image>();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return root;
            foreach (Transform child in root)
            {
                var result = FindDeep(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void ResetButton(Button button)
        {
            var serializedButton = new SerializedObject(button);
            var calls = serializedButton.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls != null)
                calls.arraySize = 0;
            serializedButton.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterSoftKittySettings()
        {
            var settings = AssetDatabase.LoadMainAssetAtPath("Assets/SoftKitty/Data/SGD_Settings.asset");
            if (settings != null)
                EditorBuildSettings.AddConfigObject("com.SoftKitty.settings", settings, true);
        }
    }
}
#endif
