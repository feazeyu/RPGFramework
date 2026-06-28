using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;

namespace Feazeyu.RPGSystems.EditorTools
{
    /// <summary>
    /// Inspector for <see cref="GraphRunner"/> and every subclass
    /// (<c>DialogueRunner</c>, <c>QuestRunner</c>, …) via
    /// <c>editorForChildClasses: true</c>.
    ///
    /// Draws the normal serialized fields (including each subclass's own
    /// fields) through <see cref="Editor.DrawDefaultInspector"/>, then adds an
    /// "Exposed Variables" section that lets you override the initial value of a
    /// graph's Exposed (non-Shared) blackboard variables per scene instance —
    /// mirroring Unity Behavior's "Expose → Behavior Agent inspector" flow.
    ///
    /// Overrides are stored on the runner (<c>m_Overrides</c>) and applied onto
    /// the runtime blackboard when it is first built; the authored asset is never
    /// mutated. Shared variables are intentionally excluded because they resolve
    /// to one global instance.
    /// </summary>
    [CustomEditor(typeof(GraphRunner), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    public class GraphRunnerEditor : Editor
    {
        private const string OverridesField = "m_Overrides";

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (targets.Length != 1)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Select a single Graph Runner to edit its exposed variable overrides.",
                    MessageType.Info);
                return;
            }

            var runner = (GraphRunner)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Exposed Variables", EditorStyles.boldLabel);

            if (runner.Graph == null)
            {
                EditorGUILayout.HelpBox("Assign a Graph to override its exposed variables.", MessageType.None);
                return;
            }

            var exposed = new List<BlackboardVariable>();
            foreach (var v in runner.Graph.Blackboard.Variables)
                if (v != null && v.Exposed && !v.Shared)
                    exposed.Add(v);

            if (exposed.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "This graph has no exposed (non-shared) variables. Mark a blackboard variable " +
                    "as Exposed (and not Shared) to override its initial value here.",
                    MessageType.None);
                return;
            }

            serializedObject.Update();
            var overridesProp = serializedObject.FindProperty(OverridesField);
            if (overridesProp == null)
            {
                EditorGUILayout.HelpBox($"Could not find serialized field '{OverridesField}'.", MessageType.Error);
                return;
            }

            foreach (var authored in exposed)
            {
                if (DrawRow(authored, overridesProp))
                {
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>Returns true if the override list's structure changed (entry added/removed).</summary>
        private static bool DrawRow(BlackboardVariable authored, SerializedProperty overridesProp)
        {
            int idx       = IndexOfOverride(overridesProp, authored.Guid);
            bool isOverridden = idx >= 0;

            EditorGUILayout.BeginHorizontal();

            bool wantOverride = EditorGUILayout.Toggle(isOverridden, GUILayout.Width(16));
            var toggleRect = GUILayoutUtility.GetLastRect();
            EditorGUI.LabelField(toggleRect, new GUIContent("", "Override this variable's initial value on this instance."));

            if (isOverridden)
            {
                DrawOverrideValue(authored, overridesProp.GetArrayElementAtIndex(idx));
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.LabelField(
                        new GUIContent(authored.Name),
                        new GUIContent(Stringify(authored.ObjectValue) + "  (asset default)"));
            }

            EditorGUILayout.EndHorizontal();

            if (wantOverride == isOverridden) return false;

            if (wantOverride)
            {
                int newIdx = overridesProp.arraySize;
                overridesProp.InsertArrayElementAtIndex(newIdx);
                overridesProp.GetArrayElementAtIndex(newIdx).managedReferenceValue = authored.Clone();
            }
            else
            {
                overridesProp.DeleteArrayElementAtIndex(idx);
            }
            return true;
        }

        private static void DrawOverrideValue(BlackboardVariable authored, SerializedProperty element)
        {
            var label = new GUIContent(authored.Name);
            var valueProp = element.FindPropertyRelative("m_Value");

            if (valueProp == null)
            {
                EditorGUILayout.LabelField(label, new GUIContent("(value not serialisable — set at runtime)"));
                return;
            }

            if (valueProp.propertyType == SerializedPropertyType.ObjectReference)
            {
                var objType = authored.ValueType ?? typeof(Object);
                valueProp.objectReferenceValue =
                    EditorGUILayout.ObjectField(label, valueProp.objectReferenceValue, objType, allowSceneObjects: true);
            }
            else
            {
                EditorGUILayout.PropertyField(valueProp, label, true);
            }
        }

        private static int IndexOfOverride(SerializedProperty overridesProp, string guid)
        {
            for (int i = 0; i < overridesProp.arraySize; i++)
            {
                var el = overridesProp.GetArrayElementAtIndex(i);
                var guidProp = el?.FindPropertyRelative("m_Guid");
                if (guidProp != null && guidProp.stringValue == guid) return i;
            }
            return -1;
        }

        private static string Stringify(object value)
        {
            if (value == null) return "(null)";
            if (value is Object o) return o ? o.name : "(none)";
            return value.ToString();
        }
    }
}
