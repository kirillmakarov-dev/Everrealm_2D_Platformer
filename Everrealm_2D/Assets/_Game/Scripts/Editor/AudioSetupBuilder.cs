#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using LetterHunter.Audio;
using LetterHunter.Characters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace LetterHunter.EditorTools
{
    public static class AudioSetupBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Game level 1.unity";
        private const string AudioFolder = "Assets/_Game/Audio";
        private const string MixerPath = AudioFolder + "/EverrealmAudio.mixer";
        private const string MusicPath = "Assets/2D Dungeon Tilemap/Musics & Sounds CC0/Music 1.wav";
        private const string ShotPath = AudioFolder + "/LetterHunter_LaserShot_CC0.wav";
        private const string SoundFolder = "Assets/SoftKitty/InventoryEngine/Resources/Sounds/";

        [MenuItem("Tools/Everrealm/Audio/Build Scene Audio")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
                throw new InvalidOperationException($"Open {ScenePath} before building scene audio.");

            EnsureFolder(AudioFolder);
            var mixer = BuildMixer();
            var manager = BuildSoundManager(scene, mixer);
            ConnectPlayer(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Everrealm scene music, mixer groups, combat SFX and UI audio are connected.");
        }

        private static MixerBundle BuildMixer()
        {
            var controllerType = Type.GetType("UnityEditor.Audio.AudioMixerController, UnityEditor");
            if (controllerType == null)
                throw new InvalidOperationException("Unity AudioMixer editor API is unavailable.");

            var controller = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath) as UnityEngine.Object;
            if (controller == null)
            {
                var create = controllerType.GetMethod("CreateMixerControllerAtPath",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                controller = create?.Invoke(null, new object[] { MixerPath }) as UnityEngine.Object;
            }
            if (controller == null)
                throw new InvalidOperationException("Could not create the Everrealm AudioMixer.");

            var masterProperty = controllerType.GetProperty("masterGroup",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var master = masterProperty?.GetValue(controller) as AudioMixerGroup;
            if (master == null)
                throw new InvalidOperationException("AudioMixer has no master group.");

            master.name = "Master";
            var music = EnsureGroup(controller, controllerType, master, "Music");
            var sfx = EnsureGroup(controller, controllerType, master, "SFX");
            var ui = EnsureGroup(controller, controllerType, master, "UI");
            SetChildren(master, music, sfx, ui);

            ExposeVolume(controller, controllerType, master, "MasterVolume");
            ExposeVolume(controller, controllerType, music, "MusicVolume");
            ExposeVolume(controller, controllerType, sfx, "SfxVolume");
            ExposeVolume(controller, controllerType, ui, "UiVolume");
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            return new MixerBundle((AudioMixer)controller, master, music, sfx, ui);
        }

        private static AudioMixerGroup EnsureGroup(UnityEngine.Object controller, Type controllerType,
            AudioMixerGroup master, string name)
        {
            var existing = ((AudioMixer)controller).FindMatchingGroups(name)
                .FirstOrDefault(group => group.name == name);
            if (existing != null)
                return existing;

            var create = controllerType.GetMethod("CreateNewGroup",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return create?.Invoke(controller, new object[] { name, false }) as AudioMixerGroup
                   ?? throw new InvalidOperationException($"Could not create AudioMixer group {name}.");
        }

        private static void SetChildren(AudioMixerGroup master, params AudioMixerGroup[] groups)
        {
            var property = master.GetType().GetProperty("children",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property == null)
                throw new InvalidOperationException("AudioMixer group hierarchy API is unavailable.");

            var array = Array.CreateInstance(property.PropertyType.GetElementType()!, groups.Length);
            for (var i = 0; i < groups.Length; i++)
                array.SetValue(groups[i], i);
            property.SetValue(master, array);
        }

        private static void ExposeVolume(UnityEngine.Object controller, Type controllerType,
            AudioMixerGroup group, string parameterName)
        {
            var exposedProperty = controllerType.GetProperty("exposedParameters",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var exposed = exposedProperty?.GetValue(controller) as Array;
            if (exposed != null)
            {
                foreach (var entry in exposed)
                {
                    var nameField = entry.GetType().GetField("name",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if ((string)nameField?.GetValue(entry)! == parameterName)
                        return;
                }
            }

            var groupType = group.GetType();
            var guid = groupType.GetMethod("GetGUIDForVolume",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.Invoke(group, null);
            var pathType = Type.GetType("UnityEditor.Audio.AudioGroupParameterPath, UnityEditor");
            var path = Activator.CreateInstance(pathType!, BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance, null, new[] { group, guid }, null);
            controllerType.GetMethod("AddExposedParameter",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?.Invoke(controller, new[] { path });

            exposed = exposedProperty?.GetValue(controller) as Array;
            if (exposed == null)
                return;
            for (var i = 0; i < exposed.Length; i++)
            {
                var entry = exposed.GetValue(i)!;
                var guidField = entry.GetType().GetField("guid",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (!Equals(guidField?.GetValue(entry), guid))
                    continue;
                entry.GetType().GetField("name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.SetValue(entry, parameterName);
                exposed.SetValue(entry, i);
                exposedProperty?.SetValue(controller, exposed);
                break;
            }
        }

        private static SoundManager BuildSoundManager(Scene scene, MixerBundle mixer)
        {
            var manager = UnityEngine.Object.FindFirstObjectByType<SoundManager>();
            if (manager == null)
            {
                var root = new GameObject("SoundManager");
                SceneManager.MoveGameObjectToScene(root, scene);
                manager = root.AddComponent<SoundManager>();
            }

            var music = EnsureSource(manager.transform, "MusicSource", mixer.Music, true, 0.55f);
            var sfx = EnsureSource(manager.transform, "SfxSource", mixer.Sfx, false, 1f);
            var ui = EnsureSource(manager.transform, "UiSource", mixer.Ui, false, 1f);

            var serialized = new SerializedObject(manager);
            serialized.FindProperty("mixer").objectReferenceValue = mixer.Mixer;
            serialized.FindProperty("musicSource").objectReferenceValue = music;
            serialized.FindProperty("sfxSource").objectReferenceValue = sfx;
            serialized.FindProperty("uiSource").objectReferenceValue = ui;
            serialized.FindProperty("sceneMusic").objectReferenceValue = Clip(MusicPath);
            serialized.FindProperty("shotClip").objectReferenceValue = Clip(ShotPath);
            serialized.FindProperty("impactClip").objectReferenceValue = Clip(SoundFolder + "ItemDrop.wav");
            serialized.FindProperty("buttonHoverClip").objectReferenceValue = Clip(SoundFolder + "bt_hover.wav");
            serialized.FindProperty("buttonClickClip").objectReferenceValue = Clip(SoundFolder + "bt_up.wav");
            serialized.FindProperty("menuOpenClip").objectReferenceValue = Clip(SoundFolder + "MenuOn.wav");
            serialized.FindProperty("menuCloseClip").objectReferenceValue = Clip(SoundFolder + "MenuOff.wav");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return manager;
        }

        private static AudioSource EnsureSource(Transform parent, string name, AudioMixerGroup group,
            bool loop, float volume)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                child = go.transform;
            }

            var source = child.GetComponent<AudioSource>();
            if (source == null)
                source = child.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.volume = volume;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = group;
            return source;
        }

        private static void ConnectPlayer(SoundManager manager)
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerClassController>();
            if (player == null)
                throw new InvalidOperationException("Game level 1 has no PlayerClassController.");
            var serialized = new SerializedObject(player);
            serialized.FindProperty("soundManager").objectReferenceValue = manager;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioClip Clip(string path) =>
            AssetDatabase.LoadAssetAtPath<AudioClip>(path) ??
            throw new InvalidOperationException($"Missing audio clip: {path}");

        private static void EnsureFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = current + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[i]);
                current = next;
            }
        }

        private readonly struct MixerBundle
        {
            public MixerBundle(AudioMixer mixer, AudioMixerGroup master, AudioMixerGroup music,
                AudioMixerGroup sfx, AudioMixerGroup ui)
            {
                Mixer = mixer;
                Master = master;
                Music = music;
                Sfx = sfx;
                Ui = ui;
            }

            public AudioMixer Mixer { get; }
            public AudioMixerGroup Master { get; }
            public AudioMixerGroup Music { get; }
            public AudioMixerGroup Sfx { get; }
            public AudioMixerGroup Ui { get; }
        }
    }
}
#endif
