using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LetterHunter.Editor
{
    /// <summary>
    /// Prevents built-in Inspectors from retaining scene-object targets while Unity
    /// reloads the scene and domain on entry to Play Mode.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayModeSelectionCleanup
    {
        static PlayModeSelectionCleanup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Unity can restore stale Inspector targets while assemblies reload on Play entry.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                ClearSelectionAndRebuildInspectors();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode && state != PlayModeStateChange.EnteredPlayMode)
                return;

            ClearSelectionAndRebuildInspectors();
        }

        private static void ClearSelectionAndRebuildInspectors()
        {
            // Clear all entries, including stale/null references, then discard cached
            // built-in Editor targets before Unity reloads the scene and domain.
            Selection.objects = System.Array.Empty<Object>();
            ActiveEditorTracker.sharedTracker.ForceRebuild();
            ClearSceneTargetsFromLockedInspectors();
        }

        private static void ClearSceneTargetsFromLockedInspectors()
        {
            // A locked Inspector owns a separate tracker, so clearing Selection and the
            // shared tracker alone cannot release scene objects that are about to reload.
            var inspectorType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
            var getInspectors = inspectorType?.GetMethod("GetAllInspectorWindows",
                BindingFlags.Static | BindingFlags.NonPublic);
            var getLockedObjects = inspectorType?.GetMethod("GetObjectsLocked",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var setLockedObjects = inspectorType?.GetMethod("SetObjectsLocked",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var isLockedProperty = inspectorType?.GetProperty("isLocked",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var trackerProperty = inspectorType?.BaseType?.GetProperty("tracker",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (getInspectors == null || getLockedObjects == null || setLockedObjects == null)
                return;

            if (getInspectors.Invoke(null, null) is not System.Collections.IEnumerable inspectors)
                return;

            foreach (var inspector in inspectors)
            {
                if (inspector == null)
                    continue;

                var lockedObjects = new List<Object>();
                getLockedObjects.Invoke(inspector, new object[] { lockedObjects });
                if (!ContainsSceneOrMissingObject(lockedObjects))
                    continue;

                setLockedObjects.Invoke(inspector, new object[] { new List<Object>() });
                isLockedProperty?.SetValue(inspector, false);
                (trackerProperty?.GetValue(inspector) as ActiveEditorTracker)?.ForceRebuild();
                (inspector as EditorWindow)?.Repaint();
            }
        }

        private static bool ContainsSceneOrMissingObject(IEnumerable<Object> objects)
        {
            foreach (var selectedObject in objects)
            {
                if (selectedObject == null)
                    return true;

                if (selectedObject is GameObject gameObject && gameObject.scene.IsValid())
                    return true;

                if (selectedObject is Component component &&
                    (component.gameObject == null || component.gameObject.scene.IsValid()))
                    return true;
            }

            return false;
        }
    }
}
