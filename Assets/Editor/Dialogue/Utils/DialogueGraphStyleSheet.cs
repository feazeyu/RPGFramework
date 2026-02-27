using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Provides the shared USS StyleSheet to any editor UI element that needs it.
    /// Loads from the package's Editor/Utils folder via AssetDatabase or a fallback
    /// inline style block if the .uss file is not found (e.g. first import).
    /// </summary>
    public static class DialogueGraphStyleSheet
    {
        private static StyleSheet s_Sheet;

        public static StyleSheet Get()
        {
#if UNITY_EDITOR
            if (s_Sheet != null) return s_Sheet;

            // Try to locate the .uss anywhere in the project.
            var guids = AssetDatabase.FindAssets("DialogueGraph t:StyleSheet");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("DialogueGraph.uss"))
                {
                    s_Sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                    if (s_Sheet != null) return s_Sheet;
                }
            }

            // Fallback: return null (GraphView will still render, just unstyled).
            Debug.LogWarning("[DialogueGraph] Could not find DialogueGraph.uss. " +
                             "Ensure the package is fully imported.");
#endif
            return null;
        }

        /// <summary>Clears the cached sheet (call after reimport if needed).</summary>
        public static void Invalidate() => s_Sheet = null;
    }
}
