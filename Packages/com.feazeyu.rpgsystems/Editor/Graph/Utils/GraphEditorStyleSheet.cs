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
    ///
    /// Search strategy:
    ///   1. AssetDatabase.FindAssets with both "Assets" and "Packages"
    ///      search folders (AssetDatabase only indexes Packages/ when the
    ///      folder list includes it explicitly).
    ///   2. On miss, don't cache — a later import may produce the asset
    ///      and we should pick it up on the next call.
    /// </summary>
    public static class GraphEditorStyleSheet
    {
        private static StyleSheet s_Sheet;

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

            // Don't cache null — if the user is mid-import, a later call
            // should succeed once the AssetDatabase catches up.
            Debug.LogWarning("[GraphEditor] Could not find GraphEditor.uss. " +
                             "Ensure the package is fully imported.");
#endif
            return null;
        }

        public static void Invalidate() => s_Sheet = null;
    }
}
