using Game.Core.Utilities;
using Game.Items;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;

#nullable enable
namespace Game.Inventory
{
    [Serializable]
    public class InventoryGrid : MonoBehaviour, IUIPositionalItemContainer
    {

        private void Awake()
        {
            RedrawContents();
        }

        [HideInInspector]
        public bool suppressAutoAddUI = false;

        [Range(1, 20)]
        public int rows = 5;
        [Range(1, 20)]
        public int columns = 5;
        [SerializeReference]
        public Array2D<InventorySlot> Cells = new(0, 0);
        private InventoryGridGenerator? uiGenerator;
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

        public bool TryGetCell(int x, int y, [NotNullWhen(true)] out InventorySlot? cell)
        {
            return Cells.TryGet(x, y, out cell);
        }

        public void DisableAll()
        {
            SetEnabledAll(false);
        }

        public void EnableAll()
        {
            SetEnabledAll(true);
        }

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

        public bool PutItem(Vector2Int position, GameObject item)
        {
            ItemInfo itemInfo = item.GetComponent<Item>().info;
            Vector2Int center = item.GetComponent<Item>().GetAnchorSlot();
            foreach (Vector2Int otherPosition in itemInfo.Shape.Positions)
            {
                Cells.TryGet(position.x + otherPosition.x - center.x, position.y + otherPosition.y-center.y, out var cell);
                if (cell == null || !cell.AcceptsItem())
                {
                    return false;
                }
            }
            Cells[position.x, position.y].PutItem(item);
            foreach (Vector2Int otherPosition in itemInfo.Shape.Positions)
            {
                Vector2Int translatedOther = new(position.x + otherPosition.x - center.x, position.y + otherPosition.y - center.y);
                if (translatedOther != position)
                {
                    Cells[translatedOther.x, translatedOther.y].anchorPosition = position;
                }
            }
            return true;
        }
        public bool ReturnItem(Vector2Int position, GameObject item)
        {
            return PutItem(position, item);
        }

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

        public GameObject? GetItem(Vector2Int position)
        {
            return Cells.TryGet(position.x, position.y, out var cell) ? cell.Item : null;
        }

        public void ToggleInventory() { 
            if(uiGenerator)
                uiGenerator.ToggleInventoryActiveState();
        }
        public void OpenInventory() {
            if(uiGenerator)
            uiGenerator.SetInventoryActiveState(true);
        }

        public void CloseInventory() {
            if (uiGenerator)
                uiGenerator.SetInventoryActiveState(false);
        }
    }
}
