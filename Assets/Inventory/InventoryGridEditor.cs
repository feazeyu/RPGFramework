using System;
using UnityEditor;
using UnityEngine;

namespace Game.Inventory
{
    [CustomEditor(typeof(InventoryGrid))]
    public class InventoryGridEditor : Editor
    {
        private int SelectedType = 0;
        private string[] AvailableTypes;
        private GameObject SelectedItem;
        private ActionMode SelectedMode = ActionMode.IgnoreItems;
        private readonly string[] AvailableModes = { "Ignore items", "Remove items", "Set items" };

        public void OnEnable()
        {
            AvailableTypes = InventoryHelper.GetSlotTypeNames();
        }

        public override void OnInspectorGUI()
        {
            InventoryGrid grid = (InventoryGrid)target;
            EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);

            grid.rows = EditorGUILayout.IntSlider("Rows", grid.rows, 1, 20);
            grid.columns = EditorGUILayout.IntSlider("Columns", grid.columns, 1, 20);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Grid Shape", EditorStyles.boldLabel);

            grid.ResizeIfNecessary();
            BuildGrid(grid);

            SelectedType = EditorGUILayout.Popup("Slot type to be set", SelectedType, AvailableTypes);

            EditorGUILayout.LabelField("Inserting items", EditorStyles.boldLabel);

            SelectedMode = (ActionMode)EditorGUILayout.Popup("Item setting mode", (int)SelectedMode, AvailableModes);
            if (SelectedMode == ActionMode.SetItems)
            {
                SelectedItem = (GameObject)EditorGUILayout.ObjectField(
                    "Item prefab",
                    SelectedItem,
                    typeof(GameObject),
                    allowSceneObjects: false
                );
                EditorGUILayout.HelpBox(
                    "This item will be set in the selected cells when you click them. It should have the appropriate scripts attached. (InventoryUIHandler for drag and drop functionality, and the appropriate item logic)",
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

        private void BuildGrid(InventoryGrid grid)
        {
            Color defaultColor = GUI.backgroundColor;
            for (int y = 0; y < grid.rows; y++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < grid.columns; x++)
                {
                    bool hasCell = grid.TryGetCell(x, y, out var cell);
                    try
                    {
                        GUI.backgroundColor = hasCell ? cell.Color : Color.white;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error setting cell ({x}, {y}): {e.Message}");
                    }

                    // Draw square button with no label
                    if (GUILayout.Button(hasCell ? cell.editorUILabel : "", GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        if (!hasCell)
                        {
                            continue;
                        }
                        if (cell.GetType().Name != AvailableTypes[SelectedType])
                        {
                            cell = (InventorySlot)Activator.CreateInstance(Type.GetType($"Game.Inventory.{AvailableTypes[SelectedType]}"));
                            grid.Cells[x, y] = cell;
                        }
                        else
                        {
                            cell.IsEnabled = !cell.IsEnabled;
                        }
#if UNITY_EDITOR
                        switch (SelectedMode)
                        {
                            case ActionMode.IgnoreItems: break; // Ignore items, do nothing
                            case ActionMode.RemoveItems: cell.EditorOnlyRemoveItem(); break;
                            case ActionMode.SetItems: cell.EditorOnlyPutItem(SelectedItem); break;
                            default: Debug.LogWarning("Unknown item setting mode selected."); break;
                        }
#endif
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUI.backgroundColor = defaultColor;
        }

        private enum ActionMode
        {
            IgnoreItems, RemoveItems, SetItems
        }
    }
}
