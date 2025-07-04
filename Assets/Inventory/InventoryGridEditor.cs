using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InventoryGrid))]
public class InventoryGridEditor : Editor {
    public override void OnInspectorGUI() {
        InventoryGrid grid = (InventoryGrid)target;

        EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
        grid.rows = EditorGUILayout.IntSlider("Rows", grid.rows, 1, 20);
        grid.columns = EditorGUILayout.IntSlider("Columns", grid.columns, 1, 20);

        grid.Refresh();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Grid Shape", EditorStyles.boldLabel);

        Color defaultColor = GUI.backgroundColor;

        for (int y = 0; y < grid.rows; y++) {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < grid.columns; x++) {

                // Set color based on state
                GUI.backgroundColor = grid.cellStates[x, y] ? Color.white : Color.gray;

                // Draw square button with no label
                if (GUILayout.Button("", GUILayout.Width(25), GUILayout.Height(25))) {
                    grid.cellStates[x, y] = !grid.cellStates[x, y];
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = defaultColor;
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Disable all", GUILayout.Width(150),GUILayout.Height(25))) { 
            grid.DisableAll();
        }
        ;

        if (GUILayout.Button("Enable all", GUILayout.Width(150), GUILayout.Height(25))) { 
            grid.EnableAll();
        }
        ;
        GUILayout.EndHorizontal();
        if (GUI.changed) {
            EditorUtility.SetDirty(grid);
        }
    }

}
