using UnityEditor;
using UnityEngine;
namespace Feazeyu.RPGSystems.Inventory
{
    [CustomEditor(typeof(InventoryListGenerator))]
    class InventoryListGeneratorEditor : Editor
    {
        private static readonly GUIContent[] s_AnchorContents =
        {
            new("↖", "Upper Left"),   new("↑", "Upper Center"),  new("↗", "Upper Right"),
            new("←", "Middle Left"),  new("●", "Middle Center"), new("→", "Middle Right"),
            new("↙", "Lower Left"),   new("↓", "Lower Center"),  new("↘", "Lower Right"),
        };

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawAnchorGrid();
            if (GUILayout.Button("Generate Inventory UI"))
            {
                ((InventoryListGenerator)target).DrawContents();
                InventoryHelper.GenerateDragLayer(((InventoryListGenerator)target).targetCanvas);
            }
        }

        private void DrawAnchorGrid()
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Anchor", EditorStyles.boldLabel);
            serializedObject.Update();
            var prop = serializedObject.FindProperty("anchorPosition");
            int selected = GUILayout.SelectionGrid(prop.enumValueIndex, s_AnchorContents, 3, GUILayout.Width(90), GUILayout.Height(66));
            if (selected != prop.enumValueIndex)
                prop.enumValueIndex = selected;
            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.Space(4);
        }
    }
}
