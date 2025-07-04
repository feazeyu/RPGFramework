using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InventoryGrid))]
public class InventoryGridEditor : Editor {
    public override void OnInspectorGUI() {
        InventoryGrid grid = (InventoryGrid)target;

        EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
        int newRows = EditorGUILayout.IntSlider("Rows", grid.rows, 1, 20);
        int newColumns = EditorGUILayout.IntSlider("Columns", grid.columns, 1, 20);

        // Only refresh if size changed
        if (newRows != grid.rows || newColumns != grid.columns) {
            grid.rows = newRows;
            grid.columns = newColumns;
            grid.Refresh();
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Grid Shape", EditorStyles.boldLabel);

        Color defaultColor = GUI.backgroundColor;

        // Defensive check for cellStates size
        if (grid.cellStates == null || grid.cellStates.GetLength(0) != grid.columns || grid.cellStates.GetLength(1) != grid.rows) {
            grid.Refresh();
        }

        for (int y = 0; y < grid.rows; y++) {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < grid.columns; x++) {
                try {
                    GUI.backgroundColor = (grid.cellStates != null && x < grid.cellStates.GetLength(0) && y < grid.cellStates.GetLength(1) && grid.cellStates[x, y]) ? Color.white : Color.gray;
                }
                catch (Exception e) {
                    Debug.LogError($"Error setting color for cell ({x}, {y}): {e.Message}");
                }

                // Draw square button with no label
                if (GUILayout.Button("", GUILayout.Width(25), GUILayout.Height(25))) {
                    if (grid.cellStates != null && x < grid.cellStates.GetLength(0) && y < grid.cellStates.GetLength(1))
                        grid.cellStates[x, y] = !grid.cellStates[x, y];
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = defaultColor;
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Disable all", GUILayout.Width(150), GUILayout.Height(25))) {
            grid.DisableAll();
        }

        if (GUILayout.Button("Enable all", GUILayout.Width(150), GUILayout.Height(25))) {
            grid.EnableAll();
        }
        GUILayout.EndHorizontal();
        if (GUI.changed) {
            EditorUtility.SetDirty(grid);
        }
    }

}
