using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuestGraph.Editor
{
    /// <summary>
    /// Provides the quest-system USS StyleSheet (colour/theme overrides)
    /// layered on top of the shared <c>GraphEditor.uss</c> base sheet.
    ///
    /// Mirrors <see cref="DialogueGraph.Editor.DialogueGraphStyleSheet"/>:
    /// two-folder lookup across both <c>Assets/</c> and <c>Packages/</c>,
    /// and no caching of null so mid-import misses recover on the next call.
    /// </summary>
    public static class QuestGraphStyleSheet
    {
        private static StyleSheet s_Sheet;

        public static StyleSheet Get()
        {
#if UNITY_EDITOR
            if (s_Sheet != null) return s_Sheet;

            var guids = AssetDatabase.FindAssets(
                "QuestGraph t:StyleSheet",
                new[] { "Assets", "Packages" });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("QuestGraph.uss")) continue;

                var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (sheet != null)
                {
                    s_Sheet = sheet;
                    return s_Sheet;
                }
            }

            Debug.LogWarning("[QuestGraph] Could not find QuestGraph.uss. " +
                             "Ensure the package is fully imported.");
#endif
            return null;
        }

        /// <summary>Clears the cached sheet (call after reimport if needed).</summary>
        public static void Invalidate() => s_Sheet = null;
    }
}
