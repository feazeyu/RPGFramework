using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Navigates a SerializedObject to find SerializedProperty fields inside a
    /// BlackboardVariable stored in a [SerializeReference] array.
    ///
    /// CRITICAL: FindPropertyRelative() does NOT cross [SerializeReference]
    /// boundaries — it silently returns null for every child field.
    /// We must use full absolute paths with SerializedObject.FindProperty().
    ///
    /// CRITICAL: Never call so.Update() inside a lookup. The caller owns the
    /// SO lifecycle. Calling Update() mid-build discards unsaved in-memory
    /// changes (e.g. a variable just added but not yet written to disk),
    /// which makes FindProperty return null for the new variable.
    /// </summary>
    public static class BlackboardPropertyBridge
    {
        // ── Index lookup ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns the array index of the variable with the given GUID, or -1.
        /// Does NOT call so.Update() — the caller is responsible for that.
        /// </summary>
        public static int FindVariableIndex(SerializedObject so, string variableGuid)
        {
            var listProp = so.FindProperty("m_Blackboard.m_Variables");
            if (listProp == null || !listProp.isArray) return -1;

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var guidProp = so.FindProperty($"m_Blackboard.m_Variables.Array.data[{i}].m_Guid");
                if (guidProp != null && guidProp.stringValue == variableGuid)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Builds a guid→index map for all variables in one pass.
        /// Use this when you need to look up multiple variables in the same
        /// rebuild to avoid O(N²) FindProperty calls.
        /// Does NOT call so.Update().
        /// </summary>
        public static Dictionary<string, int> BuildIndexMap(SerializedObject so)
        {
            var map = new Dictionary<string, int>();
            var listProp = so.FindProperty("m_Blackboard.m_Variables");
            if (listProp == null || !listProp.isArray) return map;

            for (int i = 0; i < listProp.arraySize; i++)
            {
                var guidProp = so.FindProperty($"m_Blackboard.m_Variables.Array.data[{i}].m_Guid");
                if (guidProp != null)
                    map[guidProp.stringValue] = i;
            }
            return map;
        }

        // ── Field accessors ───────────────────────────────────────────────────

        public static SerializedProperty FindVariableProperty(SerializedObject so, string guid)
        {
            int i = FindVariableIndex(so, guid);
            return i < 0 ? null : so.FindProperty($"m_Blackboard.m_Variables.Array.data[{i}]");
        }

        public static SerializedProperty FindValueProperty(SerializedObject so, string guid)
        {
            int i = FindVariableIndex(so, guid);
            return i < 0 ? null : so.FindProperty($"m_Blackboard.m_Variables.Array.data[{i}].m_Value");
        }

        public static SerializedProperty FindValuePropertyAt(SerializedObject so, int index)
            => so.FindProperty($"m_Blackboard.m_Variables.Array.data[{index}].m_Value");

        public static SerializedProperty FindVariableField(SerializedObject so, string guid, string fieldName)
        {
            int i = FindVariableIndex(so, guid);
            return i < 0 ? null : so.FindProperty($"m_Blackboard.m_Variables.Array.data[{i}].{fieldName}");
        }

        public static SerializedProperty FindVariableFieldAt(SerializedObject so, int index, string fieldName)
            => so.FindProperty($"m_Blackboard.m_Variables.Array.data[{index}].{fieldName}");

        public static (SerializedProperty element, SerializedProperty value)
            FindBoth(SerializedObject so, string guid)
        {
            int i = FindVariableIndex(so, guid);
            if (i < 0) return (null, null);
            return (
                so.FindProperty($"m_Blackboard.m_Variables.Array.data[{i}]"),
                so.FindProperty($"m_Blackboard.m_Variables.Array.data[{i}].m_Value")
            );
        }

        // ── SerializedObject cache ────────────────────────────────────────────

        private static readonly Dictionary<int, SerializedObject> s_Cache
            = new Dictionary<int, SerializedObject>();

        public static SerializedObject GetSerializedObject(UnityEngine.Object asset)
        {
            if (asset == null) return null;
            int id = asset.GetInstanceID();
            if (s_Cache.TryGetValue(id, out var cached) && cached != null && cached.targetObject != null)
                return cached;
            var so = new SerializedObject(asset);
            s_Cache[id] = so;
            return so;
        }

        public static void Invalidate(UnityEngine.Object asset)
        {
            if (asset == null) return;
            s_Cache.Remove(asset.GetInstanceID());
        }

        public static void InvalidateAll() => s_Cache.Clear();
    }
}
