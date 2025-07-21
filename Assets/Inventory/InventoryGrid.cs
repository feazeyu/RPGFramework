using Game.Utils;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;

#nullable enable
namespace Game.Inventory
{
    [Serializable]
    public class InventoryGrid : MonoBehaviour
    {
        [HideInInspector]
        public bool suppressAutoAddUI = false;

        [Range(1, 20)]
        public int rows = 5;
        [Range(1, 20)]
        public int columns = 5;
        public Array2D<InventorySlot> Cells = new(0, 0);

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
                            newStates[x, y] = new();
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
            if (!suppressAutoAddUI && GetComponent<InventoryUIGenerator>() == null)
            {
                EditorApplication.delayCall += () =>
                {
                    if (gameObject != null && gameObject.GetComponent<InventoryUIGenerator>() == null && !Application.isPlaying)
                    {
                        suppressAutoAddUI = true;
                        Undo.AddComponent<InventoryUIGenerator>(gameObject);
                    }
                };
            }
#endif
        }
    }
}
