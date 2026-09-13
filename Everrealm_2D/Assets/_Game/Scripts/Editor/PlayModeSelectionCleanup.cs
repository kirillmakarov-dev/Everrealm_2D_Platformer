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
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            foreach (var selectedObject in Selection.objects)
            {
                if ((selectedObject is GameObject gameObject && gameObject.scene.IsValid()) ||
                    (selectedObject is Component component && component.gameObject.scene.IsValid()))
                {
                    Selection.objects = System.Array.Empty<Object>();
                    return;
                }
            }
        }
    }
}
