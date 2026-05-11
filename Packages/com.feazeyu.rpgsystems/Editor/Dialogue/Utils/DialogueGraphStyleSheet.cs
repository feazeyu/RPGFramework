using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Provides the dialogue-system USS StyleSheet (colour/theme overrides)
    /// layered on top of the shared GraphEditor.uss base sheet.
    ///
    /// Search strategy: see GraphEditorStyleSheet — same two-folder lookup
    /// and same don't-cache-null policy so mid-import misses recover on
    /// the next call.
    /// </summary>
    public static class DialogueGraphStyleSheet
    {
        private static StyleSheet s_Sheet;

        public static StyleSheet Get()
        {
#if UNITY_EDITOR
            if (s_Sheet != null) return s_Sheet;

            var guids = AssetDatabase.FindAssets(
                "DialogueGraph t:StyleSheet",
                new[] { "Assets", "Packages" });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("DialogueGraph.uss")) continue;

                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (sheet != null)
                {
                    s_Sheet = sheet;
                    return s_Sheet;
                }
            }

            Debug.LogWarning("[DialogueGraph] Could not find DialogueGraph.uss. " +
                             "Ensure the package is fully imported.");
#endif
            return null;
        }

        /// <summary>Clears the cached sheet (call after reimport if needed).</summary>
        public static void Invalidate() => s_Sheet = null;
    }
}
