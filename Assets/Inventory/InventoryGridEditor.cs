using Codice.CM.Common.Update.Partial;
using NUnit.Compatibility;
using NUnit.Framework;
using PlasticGui;
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

        GameObject SelectedItem;
        int SelectedMode = 0;
        string[] AvailableModes = { "Ignore items" , "Remove items", "Set items" };

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
                    bool hasCell = grid.TryGetCell(x, y, out var cell);
                    try
                    {
                        if (hasCell)
                        {
                            GUI.backgroundColor = cell.color;
                        }
                        else
                        {
                            GUI.backgroundColor = Color.white;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error setting cell ({x}, {y}): {e.Message}");
                    }
                    // Draw square button with no label

                    if (GUILayout.Button(hasCell ? cell.EditorUILabel : "", GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        if (hasCell)
                        {
                            if (cell.GetType().Name != AvailableTypes[SelectedType])
                            {
                                var newCell = (InventorySlot)Activator.CreateInstance(Type.GetType($"RPGFramework.Inventory.{AvailableTypes[SelectedType]}"));
                                grid.cells[x, y] = newCell;
                            }
                            else
                            {
                                cell.IsEnabled = !cell.IsEnabled;
                            }
#if UNITY_EDITOR
                            switch(SelectedMode)
                            {
                                case 0: break; //Ignore items, do nothing
                                case 1: grid.cells[x, y].EditorOnlyRemoveItem(); break;
                                case 2: grid.cells[x, y].EditorOnlyPutItem(SelectedItem);break;
                                default: Debug.LogWarning("Unknown item setting mode selected."); break;
                            }
#endif
                        }
                    }

                }
                EditorGUILayout.EndHorizontal();
            }

            GUI.backgroundColor = defaultColor;



            SelectedType = EditorGUILayout.Popup("Slot type to be set", SelectedType, AvailableTypes);

            EditorGUILayout.LabelField("Inserting items", EditorStyles.boldLabel);


            SelectedMode = EditorGUILayout.Popup("Item setting mode", SelectedMode, AvailableModes);

            //Magic number ugly, I know. No other pretty way to do it afaik.
            if (SelectedMode == 2) { 
            
            SelectedItem = (GameObject)EditorGUILayout.ObjectField(
                    $"Item prefab",
                    SelectedItem,
                    typeof(GameObject),
                    false
                );
            EditorGUILayout.HelpBox(
                "This item will be set in the selected cells when you click them. It should have the appropriate scripts attached.",
                MessageType.Info
            );
            }

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