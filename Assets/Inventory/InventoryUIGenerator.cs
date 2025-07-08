using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RPGFramework.Inventory
{
#nullable enable
    [ExecuteInEditMode]
    public class InventoryUIGenerator : MonoBehaviour
    {
        [Header("UI Prefab Settings")]
        public Dictionary<string, SlotUIDefinition> slotDefinitions = new();
        public RectTransform? parentTransform;

        [Header("Cell Layout Settings")]
        public Vector2 cellSize = new Vector2(100, 100);
        public Vector2 spacing = new Vector2(5, 5);

        [HideInInspector] public GameObject? lastGeneratedRoot;
        public void OnEnable()
        {
            ReloadSlotTypes();
        }
        private void ReloadSlotTypes() { 
            InventorySlotUtils.GetSlotTypeNames().ToList().ForEach(typeName => {
                if (!slotDefinitions.ContainsKey(typeName))
                {
                    slotDefinitions[typeName] = new SlotUIDefinition();
                    //Debug.Log($"InventoryUIGenerator: Added new slot type '{typeName}' to definitions.");
                }
            });
        }
        public void GenerateUI()
        {
            var inventoryGrid = GetComponent<InventoryGrid>();
            if (inventoryGrid == null)
            {
                Debug.LogError("InventoryUIGenerator: Missing InventoryGrid component.");
                return;
            }

            inventoryGrid.ResizeIfNecessary();

            // Clean up previous UI
            if (lastGeneratedRoot != null)
            {
                DestroyImmediate(lastGeneratedRoot);
            }

            // Create root container
            var root = new GameObject("GeneratedInventoryUI");
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.SetParent(parentTransform != null ? parentTransform : transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var grid = root.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = inventoryGrid.columns;

            for (int y = 0; y < inventoryGrid.rows; y++)
            {
                for (int x = 0; x < inventoryGrid.columns; x++)
                {
                    var slotType = inventoryGrid.cells[x, y].GetType().Name;
                    if (slotDefinitions[slotType].cellPrefab == null || slotDefinitions[slotType].disabledCellPrefab == null) {
                        Debug.LogWarning($"InventoryUIGenerator: Cell at ({x}, {y}) is missing a prefab.");
                        continue;
                    }
                    var selectedPrefab = inventoryGrid.cells[x, y].IsEnabled ? slotDefinitions[slotType].cellPrefab : slotDefinitions[slotType].disabledCellPrefab;
                    var cellInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab, root.transform);
                    cellInstance.name = $"Cell_{x}_{y}";

                    if (inventoryGrid.TryGetCell(x, y, out var cell))
                    {

                    }
                }
            }

            lastGeneratedRoot = root;
        }
    }
    public struct SlotUIDefinition
    {
        public GameObject? cellPrefab;
        public GameObject? disabledCellPrefab;
    }

}
