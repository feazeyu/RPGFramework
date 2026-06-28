using Feazeyu.RPGSystems.Core.Utilities;
using Feazeyu.RPGSystems.Items;
using System;
using System.Diagnostics.CodeAnalysis;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;

#nullable enable
namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// Represents a grid-based inventory system that manages item placement, removal, and UI generation.
    /// </summary>
    [Serializable]
    public class InventoryGrid : MonoBehaviour, IUIPositionalItemContainer, IItemCountNotifier
    {
        /// <summary>Raised after an item is added to the grid: (itemId, count).</summary>
        public event Action<int, int> OnItemAdded;

        /// <summary>Raised after an item is removed from the grid: (itemId, count).</summary>
        public event Action<int, int> OnItemRemoved;

        private void RaiseItemAdded(int itemId) => OnItemAdded?.Invoke(itemId, 1);

        private void RaiseItemRemoved(int itemId) => OnItemRemoved?.Invoke(itemId, 1);

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Initializes the inventory UI contents.
        /// </summary>
        protected virtual void Awake()
        {
            RedrawContents();
        }

        /// <summary>Suppress auto add ui.</summary>
        [HideInInspector]
        public bool suppressAutoAddUI = false;

#if UNITY_EDITOR
        /// <summary>Persist play mode changes.</summary>
        [HideInInspector]
        public bool persistPlayModeChanges = false;
#endif

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
        /// When enabled, adding an item that matches an existing stack consolidates into that
        /// stack (incrementing its count) instead of occupying a new cell. Counts are rendered in
        /// the bottom-left of the stack's bottom-left cell. Requires the involved cells to be
        /// <see cref="StackableInventorySlot"/> to hold a count — newly created cells become
        /// stackable automatically while this is on (see <see cref="CreateEmptyCell"/>).
        /// </summary>
        public bool allowStacking = false;

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
                            newStates[x, y] = CreateEmptyCell(new Vector2Int(x, y));
                        }
                    }
                }
                Cells = newStates;
            }
        }

        /// <summary>
        /// Creates an empty cell for the given position. When <see cref="allowStacking"/> is on,
        /// new cells are <see cref="StackableInventorySlot"/> so they can hold a stack count.
        /// </summary>
        /// <param name="position">The grid coordinate the cell occupies.</param>
        /// <returns>A new empty <see cref="InventorySlot"/>.</returns>
        protected virtual InventorySlot CreateEmptyCell(Vector2Int position)
        {
            return allowStacking
                ? new StackableInventorySlot { position = position }
                : new InventorySlot(position);
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
        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
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
        public virtual int RemoveItem(Vector2Int position)
        {
            if (!Cells.TryGet(position.x, position.y, out var cell) || cell.Item == null)
                return -1;

            if (cell is StackableInventorySlot stack && stack.WouldRemainAfterRemove)
            {
                int removedId = cell.RemoveItem();
                if (removedId >= 0) RaiseItemRemoved(removedId);
                return removedId;
            }

            var item = cell.Item;
            var itemInfo = item.GetComponent<Item>().info;
            var center = item.GetComponent<Item>().GetAnchorSlot();

            foreach (Vector2Int otherPosition in itemInfo.Shape.Positions)
            {
                int x = position.x + otherPosition.x - center.x;
                int y = position.y + otherPosition.y - center.y;
                if (Cells.TryGet(x, y, out var slot))
                {
                    slot.anchorPosition = new Vector2Int(-1, -1);
                }
            }

            int removed = cell.RemoveItem();
            if (removed >= 0) RaiseItemRemoved(removed);
            return removed;
        }

        /// <summary>
        /// Attempts to place an item at the specified position in the grid.
        /// </summary>
        /// <param name="position">The grid position.</param>
        /// <param name="item">The item GameObject.</param>
        /// <returns>True if the item was placed successfully; otherwise, false.</returns>
        public virtual bool PutItem(Vector2Int position, GameObject item)
        {
            ItemInfo itemInfo = item.GetComponent<Item>().info;
            Vector2Int center = item.GetComponent<Item>().GetAnchorSlot();

            if (allowStacking
                && Cells.TryGet(position.x, position.y, out var target)
                && target is StackableInventorySlot stack
                && stack.anchorPosition == new Vector2Int(-1, -1)
                && stack.ItemId == itemInfo.id
                && stack.HasRoom)
            {
                return target.PutItem(item);
            }

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
            if (!Cells[position.x, position.y].PutItem(item))
                return false;
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
                if (cell == null || !cell.AcceptsItem(itemInfo))
                {
                    return false;
                }
            }
            return true;
        }

        bool IItemContainer.PutItem(GameObject item)
        {
            if (!TryAddItem(item)) return false;
            RedrawContents();
            return true;
        }

        /// <summary>Try add item.</summary>
        protected virtual bool TryAddItem(GameObject item)
        {
            var itemComp = item.GetComponent<Item>();
            if (itemComp == null) return false;

            if (allowStacking && TryStackInto(itemComp.info.id, item))
            {
                RaiseItemAdded(itemComp.info.id);
                return true;
            }

            var anchor = itemComp.GetAnchorSlot();
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var pos = new Vector2Int(x, y);
                    if (IsPlacementValid(pos, itemComp.info, anchor)
                        && PutItemUnchecked(pos, item, itemComp.info, anchor))
                    {
                        RaiseItemAdded(itemComp.info.id);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Finds an anchor cell holding a non-full <see cref="StackableInventorySlot"/> of the same
        /// item and adds one to it. Returns false if no such stack exists.
        /// </summary>
        private bool TryStackInto(int itemId, GameObject item)
        {
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (Cells.TryGet(x, y, out var cell)
                        && cell is StackableInventorySlot s
                        && s.anchorPosition == new Vector2Int(-1, -1)
                        && s.ItemId == itemId
                        && s.HasRoom
                        && cell.PutItem(item))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Try add item.</summary>
        public bool TryAddItem(int itemId, int count = 1)
        {
            var prefab = InventoryManager.Instance?.GetItemById(itemId);
            if (prefab == null) return false;
            for (int i = 0; i < count; i++)
            {
                var instance = Instantiate(prefab);
                if (!TryAddItem(instance))
                {
                    Destroy(instance);
                    return false;
                }
            }
            RedrawContents();
            return true;
        }

        int IItemContainer.CountItem(int itemId)
        {
            int count = 0;
            for (int x = 0; x < Cells.Columns; x++)
                for (int y = 0; y < Cells.Rows; y++)
                    if (Cells.TryGet(x, y, out var cell) && cell.ItemId == itemId
                        && cell.anchorPosition == new Vector2Int(-1, -1))
                        count += cell is StackableInventorySlot s ? Mathf.Max(s.itemCount, 0) : 1;
            return count;
        }

        bool IItemContainer.RemoveItem(int itemId, int count)
        {
            if (((IItemContainer)this).CountItem(itemId) < count) return false;

            int remaining = count;
            for (int x = 0; x < Cells.Columns && remaining > 0; x++)
            {
                for (int y = 0; y < Cells.Rows && remaining > 0; y++)
                {
                    if (!Cells.TryGet(x, y, out var cell) || cell.ItemId != itemId
                        || cell.anchorPosition != new Vector2Int(-1, -1))
                        continue;

                    var pos = new Vector2Int(x, y);
                    while (remaining > 0 && cell.ItemId == itemId)
                    {
                        RemoveItem(pos);
                        remaining--;
                    }
                }
            }
            RedrawContents();
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
        public virtual void RedrawContents()
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
        /// Removes and destroys all items in the grid.
        /// </summary>
        public void Clear()
        {
            for (int x = 0; x < Cells.Columns; x++)
            {
                for (int y = 0; y < Cells.Rows; y++)
                {
                    if (Cells.TryGet(x, y, out var cell) && cell.Item != null)
                    {
                        var item = cell.Item;
                        if (cell is StackableInventorySlot s)
                        {
                            s.itemCount = 1;
                            s.infinite = false;
                        }
                        RemoveItem(new Vector2Int(x, y));
#if UNITY_EDITOR
                        if (UnityEditor.EditorUtility.IsPersistent(item)) continue;
                        DestroyImmediate(item);
#else
                        Destroy(item);
#endif
                    }
                }
            }
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
        public virtual void ToggleInventory()
        {
            if (uiGenerator)
                uiGenerator.ToggleInventoryActiveState();
        }

        /// <summary>
        /// Enables the inventory UI.
        /// </summary>
        public virtual void OpenInventory()
        {
            if (uiGenerator)
                uiGenerator.SetInventoryActiveState(true);
        }

        /// <summary>
        /// Disables the inventory UI.
        /// </summary>
        public virtual void CloseInventory()
        {
            if (uiGenerator)
                uiGenerator.SetInventoryActiveState(false);
        }
    }
}
