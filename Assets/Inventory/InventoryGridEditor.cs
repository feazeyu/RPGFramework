using NUnit.Compatibility;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace RPGFramework.Inventory
{
    [CustomEditor(typeof(InventoryGrid))]
    public class InventoryGridEditor : Editor
    {
        int SelectedType = 0;
        string[] AvailableTypes;
        public void OnEnable()
        {
            AvailableTypes = InventorySlotUtils.GetSlotTypeNames();
        }
        public override void OnInspectorGUI()
        {
            InventoryGrid grid = (InventoryGrid)target;
            EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
            int newRows = EditorGUILayout.IntSlider("Rows", grid.rows, 1, 20);
            int newColumns = EditorGUILayout.IntSlider("Columns", grid.columns, 1, 20);

            // Only refresh if size changed
            if (newRows != grid.rows || newColumns != grid.columns)
            {
                grid.rows = newRows;
                grid.columns = newColumns;
                grid.ResizeIfNecessary();
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Grid Shape", EditorStyles.boldLabel);

            Color defaultColor = GUI.backgroundColor;

            if (grid.cells is null || grid.cells.GetLength(0) != grid.columns || grid.cells.GetLength(1) != grid.rows)
            {
                grid.ResizeIfNecessary();
            }

            //Grid building
            for (int y = 0; y < grid.rows; y++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < grid.columns; x++)
                {
                    try
                    {
                        if (grid.TryGetCell(x, y, out var cell))
                        {
                            GUI.backgroundColor = cell.color;
                        }
                        else { 
                            GUI.backgroundColor = Color.white;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error setting cell ({x}, {y}): {e.Message}");
                    }

                    // Draw square button with no label
                    if (GUILayout.Button("", GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        if (grid.TryGetCell(x, y, out var cell)) {
                            if (cell.GetType().Name != AvailableTypes[SelectedType])
                            {
                                var newCell = (InventorySlot)Activator.CreateInstance(Type.GetType($"RPGFramework.Inventory.{AvailableTypes[SelectedType]}"));
                                grid.cells[x, y] = newCell;
                            }
                            else { 
                                cell.IsEnabled = !cell.IsEnabled;
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            GUI.backgroundColor = defaultColor;



            SelectedType = EditorGUILayout.Popup("Slot type to be set", SelectedType, AvailableTypes);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            // Buttons to enable/disable all cells
            if (GUILayout.Button("Disable all", GUILayout.Width(150), GUILayout.Height(25)))
            {
                grid.DisableAll();
            }

            if (GUILayout.Button("Enable all", GUILayout.Width(150), GUILayout.Height(25)))
            {
                grid.EnableAll();
            }
            GUILayout.EndHorizontal();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(grid);
            }
        }
    }
}