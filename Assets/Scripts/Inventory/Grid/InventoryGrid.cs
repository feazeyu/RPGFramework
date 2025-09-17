using Game.Core.Utilities;
using Game.Items;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable
namespace Game.Inventory
{
    /// <summary>
    /// Represents a grid-based inventory system that manages item placement, removal, and UI generation.
    /// </summary>
    [Serializable]
    public class InventoryGrid : MonoBehaviour, IUIPositionalItemContainer
    {
        /// <summary>
        /// Called when the script instance is being loaded.
        /// Initializes the inventory UI contents.
        /// </summary>
        private void Awake()
        {
            RedrawContents();
        }

        /// <summary>
        /// If true, suppresses automatic addition of the UI generator component.
        /// </summary>
        [HideInInspector]
        public bool suppressAutoAddUI = false;

        /// <summary>
        /// Number of rows in the inventory grid.
        /// </summary>
        [Range(1, 20)]
        public int rows = 5;

        /// <summary>
        /// Number of columns in the inventory grid.
        /// </summary>
        [Range(1, 20)]
        public int columns = 5;

        /// <summary>
        /// 2D array of inventory slots.
        /// </summary>
        [SerializeReference]
        public Array2D<InventorySlot> Cells = new(0, 0);

        /// <summary>
        /// Reference to the UI generator for this inventory grid.
        /// </summary>
        private InventoryGridGenerator? uiGenerator;

        /// <summary>
        /// Resizes the grid if the current dimensions do not match the specified rows and columns.
        /// </summary>
        public void ResizeIfNecessary()
        {
            if (Cells is null || Cells.Columns != columns || Cells.Rows != rows)
            {
                var newStates = new Array2D<InventorySlot>(columns, rows);
                for (int x = 0; x < columns; x++)
                {
                    for (int y = 0; y < rows; y++)
                    {
                        if (TryGetCell(x, y, out var existing))
                        {
                            newStates[x, y] = existing;
                        }
                        else
                        {
                            newStates[x, y] = new(new Vector2Int(x, y));
                        }
                    }
                }
                Cells = newStates;
            }
        }

        /// <summary>
        /// Attempts to get the cell at the specified coordinates.
        /// </summary>
        /// <param name="x">The column index.</param>
        /// <param name="y">The row index.</param>
        /// <param name="cell">The output cell if found.</param>
        /// <returns>True if the cell exists; otherwise, false.</returns>
        public bool TryGetCell(int x, int y, [NotNullWhen(true)] out InventorySlot? cell)
        {
            return Cells.TryGet(x, y, out cell);
        }

        /// <summary>
        /// Disables all slots in the inventory grid.
        /// </summary>
        public void DisableAll()
        {
            SetEnabledAll(false);
        }

        /// <summary>
        /// Enables all slots in the inventory grid.
        /// </summary>
        public void EnableAll()
        {
            SetEnabledAll(true);
        }

        /// <summary>
        /// Sets the enabled state for all slots in the grid.
        /// </summary>
        /// <param name="enabled">If true, enables all slots; otherwise, disables them.</param>
        private void SetEnabledAll(bool enabled)
        {
            for (int x = 0; x < Cells.Columns; x++)
            {
                for (int y = 0; y < Cells.Rows; y++)
                {
                    Cells[x, y].IsEnabled = enabled;
                }
            }
            ResizeIfNecessary();
        }

        /// <summary>
        /// Called when the script is loaded or a value changes in the Inspector (Editor only).
        /// Ensures the UI generator is added if needed.
        /// </summary>
        private void OnValidate()
        {
#if UNITY_EDITOR
            //Ensure the generator UI is only added once
            if (!suppressAutoAddUI && GetComponent<InventoryGridGenerator>() == null)
            {
                EditorApplication.delayCall += () => {
                    if (gameObject != null && gameObject.GetComponent<InventoryGridGenerator>() == null && !Application.isPlaying)
                    {
                        suppressAutoAddUI = true;
                        Undo.AddComponent<InventoryGridGenerator>(gameObject);
                    }
                };
            }
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Puts an item into the grid at the specified position in the editor only.
        /// </summary>
        /// <param name="position">The grid position.</param>
        /// <param name="item">The item GameObject.</param>
        /// <returns>True if the item was placed successfully.</returns>
        public bool EditorOnlyPutItem(Vector2Int position, GameObject item)
        {
            Cells[position.x, position.y].EditorOnlyPutItem(item);
            SetAnchors(position, item.GetComponent<Item>().info, item.GetComponent<Item>().GetAnchorSlot());
            return true;
        }
#endif

        /// <summary>
        /// Removes the item at the specified position from the grid.
        /// </summary>
        /// <param name="position">The grid position.</param>
        /// <returns>The result of the removal operation, or -1 if no item was present.</returns>
        public int RemoveItem(Vector2Int position)
        {
            if (!Cells.TryGet(position.x, position.y, out var cell) || cell.Item == null)
                return -1;

            var item = cell.Item;
            var itemInfo = item.GetComponent<Item>().info;
            var center = item.GetComponent<Item>().GetAnchorSlot();

            // Remove anchor references for all slots occupied by the item
            foreach (Vector2Int otherPosition in itemInfo.Shape.Positions)
            {
                int x = position.x + otherPosition.x - center.x;
                int y = position.y + otherPosition.y - center.y;
                if (Cells.TryGet(x, y, out var slot))
                {
                    slot.anchorPosition = new Vector2Int(-1, -1);
                }
            }

            // Remove the item from the anchor slot
            return cell.RemoveItem();
        }

        /// <summary>
        /// Attempts to place an item at the specified position in the grid.
        /// </summary>
        /// <param name="position">The grid position.</param>
        /// <param name="item">The item GameObject.</param>
        /// <returns>True if the item was placed successfully; otherwise, false.</returns>
        public bool PutItem(Vector2Int position, GameObject item)
        {
            ItemInfo itemInfo = item.GetComponent<Item>().info;
            Vector2Int center = item.GetComponent<Item>().GetAnchorSlot();
            bool valid = IsPlacementValid(position, itemInfo, center);
            if (!valid) return false;
            return PutItemUnchecked(position, item, itemInfo, center);
        }

        /// <summary>
        /// Places an item in the grid without validation.
        /// </summary>
        /// <param name="position">The grid position.</param>
        /// <param name="item">The item GameObject.</param>
        /// <param name="itemInfo">The item's info.</param>
        /// <param name="center">The anchor slot of the item.</param>
        /// <returns>True if the item was placed.</returns>
        private bool PutItemUnchecked(Vector2Int position, GameObject item, ItemInfo itemInfo, Vector2Int center)
        {
            Cells[position.x, position.y].PutItem(item);
            SetAnchors(position, itemInfo, center);
            return true;
        }

        /// <summary>
        /// Sets anchor references for all slots occupied by the item.
        /// </summary>
        /// <param name="position">The anchor position.</param>
        /// <param name="itemInfo">The item's info.</param>
        /// <param name="center">The anchor slot of the item.</param>
        private void SetAnchors(Vector2Int position, ItemInfo itemInfo, Vector2Int center)
        {
            foreach (Vector2Int otherPosition in itemInfo.Shape.Positions)
            {
                Vector2Int translatedOther = new(position.x + otherPosition.x - center.x, position.y + otherPosition.y - center.y);
                if (translatedOther != position)
                {
                    Cells[translatedOther.x, translatedOther.y].anchorPosition = position;
                }
            }
        }

        /// <summary>
        /// Determines whether the item can be placed at the specified position.
        /// </summary>
        /// <param name="position">The grid position.</param>
        /// <param name="itemInfo">The item's info.</param>
        /// <param name="center">The anchor slot of the item.</param>
        /// <returns>True if placement is valid; otherwise, false.</returns>
        private bool IsPlacementValid(Vector2Int position, ItemInfo itemInfo, Vector2Int center)
        {
            foreach (Vector2Int otherPosition in itemInfo.Shape.Positions)
            {
                Cells.TryGet(position.x + otherPosition.x - center.x, position.y + otherPosition.y - center.y, out var cell);
                if (cell == null || !cell.AcceptsItem())
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns an item to the specified position in the grid.
        /// </summary>
        /// <param name="position">The grid position.</param>
        /// <param name="item">The item GameObject.</param>
        /// <returns>True if the item was returned successfully; otherwise, false.</returns>
        public bool ReturnItem(Vector2Int position, GameObject item)
        {
            return PutItem(position, item);
        }

        /// <summary>
        /// Regenerates the inventory UI contents.
        /// </summary>
        public void RedrawContents()
        {
            if (uiGenerator == null)
            {
                uiGenerator = gameObject.GetComponent<InventoryGridGenerator>();
                if (uiGenerator == null)
                {
                    Debug.LogWarning("UIGenerator of inventoryGrid couldn't be found.");
                    return;
                }
            }
            uiGenerator.GenerateUI();
        }

        /// <summary>
        /// Gets the item at the specified position in the grid.
        /// </summary>
        /// <param name="position">The grid position.</param>
        /// <returns>The item GameObject if present; otherwise, null.</returns>
        public GameObject? GetItem(Vector2Int position)
        {
            return Cells.TryGet(position.x, position.y, out var cell) ? cell.Item : null;
        }

        /// <summary>
        /// Toggles the active state of the inventory UI.
        /// </summary>
        public void ToggleInventory()
        {
            if (uiGenerator)
                uiGenerator.ToggleInventoryActiveState();
        }

        /// <summary>
        /// Opens the inventory UI.
        /// </summary>
        public void OpenInventory()
        {
            if (uiGenerator)
                uiGenerator.SetInventoryActiveState(true);
        }

        /// <summary>
        /// Closes the inventory UI.
        /// </summary>
        public void CloseInventory()
        {
            if (uiGenerator)
                uiGenerator.SetInventoryActiveState(false);
        }
    }
}
