using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Feazeyu.RPGSystems.EditorTools
{
    /// <summary>
    /// Loads the shared GraphEditor.uss base stylesheet.
    /// Every graph editor window loads this first, then loads its own
    /// system-specific theme sheet (e.g. DialogueGraph.uss).
    /// </summary>
    public static class GraphEditorStyleSheet
    {
        private static StyleSheet s_Sheet;

        /// <summary>Get.</summary>
        public static StyleSheet Get()
        {
#if UNITY_EDITOR
            if (s_Sheet != null) return s_Sheet;

            var guids = AssetDatabase.FindAssets(
                "GraphEditor t:StyleSheet",
                new[] { "Assets", "Packages" });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("GraphEditor.uss")) continue;

                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (sheet != null)
                {
                    s_Sheet = sheet;
                    return s_Sheet;
                }
            }

            Debug.LogWarning("[GraphEditor] Could not find GraphEditor.uss. " +
                             "Ensure the package is fully imported.");
#endif
            return null;
        }

        /// <summary>Invalidate.</summary>
        public static void Invalidate() => s_Sheet = null;
    }
}
