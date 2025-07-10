using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;
using RPGFramework.Utils;
#nullable enable
namespace RPGFramework.Inventory {
    [Serializable]
    public class InventoryGrid : MonoBehaviour, ISerializationCallbackReceiver {
        public void OnBeforeSerialize()
        {
            serializableCells = new Serializable2DArray<InventorySlot>(cells);  
        }
        public void OnAfterDeserialize()
        {
            if (serializableCells!=null) { 
                cells = (InventorySlot[,])serializableCells.ToArray();
            }
        }

        [HideInInspector] public bool suppressAutoAddUI = false;

        [SerializeField, Range(1, 20)]
        public int rows = 5;
        [SerializeField, Range(1, 20)]
        public int columns = 5;
        [HideInInspector, SerializeField]
        private Serializable2DArray<InventorySlot>? serializableCells;

        public InventorySlot[,] cells = new InventorySlot[0, 0];
        public void ResizeIfNecessary() {
            if (cells is null || cells.GetLength(0) != columns || cells.GetLength(1) != rows) {
                var newStates = new InventorySlot[columns, rows];
                for (int x = 0; x < columns; x++) {
                    for (int y = 0; y < rows; y++) {
                        if (TryGetCell(x, y, out var existing)) {
                            newStates[x, y] = existing;
                        }
                        else {
                            newStates[x, y] = new();
                        }
                    }
                }
                cells = newStates;
            }
        }

        public bool TryGetCell(int x, int y, [NotNullWhen(true)] out InventorySlot? cell)
        {
            if (cells is not null && (uint)x < (uint)cells.GetLength(0) && (uint)y < (uint)cells.GetLength(1)) {
                cell = cells[x, y];
                return true;
            }
            cell = null;
            return false;
        }

        public void DisableAll() {
            SetEnabledAll(false);
        }

        public void EnableAll() {
            SetEnabledAll(true);
        }

        private void SetEnabledAll(bool enabled) {
            for (int x = 0; x < cells.GetLength(0); x++) {
                for (int y = 0; y < cells.GetLength(1); y++) {
                    cells[x, y].IsEnabled = enabled;
                }
            }
            ResizeIfNecessary();
        }

        void OnValidate()
        {
#if UNITY_EDITOR
            if (!suppressAutoAddUI && GetComponent<InventoryUIGenerator>() == null)
            {
                EditorApplication.delayCall += () =>
                {
                    if (gameObject != null && gameObject.GetComponent<InventoryUIGenerator>() == null && !Application.isPlaying)
                    {
                        Undo.AddComponent<InventoryUIGenerator>(gameObject);
                    }
                };
            }
#endif
        }
    }


    
}