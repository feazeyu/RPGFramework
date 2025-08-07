#nullable enable
using Game.Core.Utilities;
using System;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor;
using UnityEngine;
namespace Game.Inventory
{
    [ExecuteInEditMode, Serializable]
    public class InventoryGridGenerator : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Header("UI Prefab Settings")]
        public Dictionary<string, SlotUIDefinition> slotDefinitions = new();
        [Tooltip("Canvas to generate to")]
        public Canvas? target;

        [Header("Cell Layout Settings")]

        public Vector2 spacing = new(5, 5);

        [HideInInspector, SerializeField]
        private GameObject? lastGeneratedRoot;

        [SerializeField, HideInInspector]
        private SerializableDictionary<string, SlotUIDefinition>? serializableSlotDictionary;

        public void OnEnable()
        {
            ReloadSlotTypes();
        }

        private void ReloadSlotTypes()
        {
            foreach (string typeName in InventoryHelper.GetSlotTypeNames())
            {
                slotDefinitions.TryAdd(typeName, default);
            }
        }

        public void GenerateUI()
        {
            if (!TryGetComponent<InventoryGrid>(out var inventoryGrid))
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
            if (target == null)
            {
                Debug.LogError("InventoryUIGenerator: Target Canvas is not set.");
                return;
            }
            var parentTransform = target.transform;
            rootRect.SetParent(parentTransform != null ? parentTransform : transform, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var grid = root.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            grid.cellSize = InventoryManager.Instance.cellSize;
            grid.spacing = spacing;
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = inventoryGrid.columns;

            for (int y = 0; y < inventoryGrid.rows; y++)
            {
                for (int x = 0; x < inventoryGrid.columns; x++)
                {
                    GenerateSlot(inventoryGrid, root, x, y);
                }
            }
            InventoryHelper.GenerateDragLayer(target);
            lastGeneratedRoot = root;
        }

        private void GenerateSlot(InventoryGrid inventoryGrid, GameObject root, int x, int y)
        {
            InventorySlot cell = inventoryGrid.Cells[x, y];
            SlotUIDefinition definition = slotDefinitions[cell.GetType().Name];

            if (definition.cellPrefab == null || definition.disabledCellPrefab == null)
            {
                Debug.LogWarning($"InventoryUIGenerator: Cell at ({x}, {y}) is missing a prefab.");
                return;
            }
            GameObject selectedPrefab = cell.IsEnabled ? definition.cellPrefab : definition.disabledCellPrefab;
#if UNITY_EDITOR
            GameObject cellInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab, root.transform);
#else
             GameObject cellInstance = Instantiate(selectedPrefab, root.transform);
#endif
            cellInstance.name = $"Cell_{x}_{y}";

            GridUISlot slot = cellInstance.AddComponent<GridUISlot>();
            slot.target = inventoryGrid;
            slot.position = new Vector2Int(x, y);
            CreateSlotItem(inventoryGrid, slot);
        }
        private void CreateSlotItem(InventoryGrid grid, GridUISlot slot)
        {
            GameObject? item = slot.target.GetItem(slot.position);
            var anchor = grid.Cells[slot.position.x, slot.position.y].anchorPosition;
            if (item != null)
            {
                var itemInstance = Instantiate(slot.target.GetItem(slot.position), slot.transform);
                if (itemInstance == null) // Check to make the compiler happy
                {
                    Debug.LogError($"InventoryUIGenerator: Item at {slot.position} failed to instantiate... how?");
                    return;
                }   
                var canvas = itemInstance.AddComponent<Canvas>();
                canvas.overrideSorting = true;

                InventoryHelper.CreateUIDragHandler(slot.gameObject);

            }
            else if (anchor != new Vector2Int(-1, -1))
            {
                var redirect = InventoryHelper.CreateUIDragHandler(slot.gameObject, true);
                redirect.GetComponent<InventoryItemUIRedirectingHandler>().targetPosition = anchor;
            }
        }

        public void OnBeforeSerialize()
        {
            serializableSlotDictionary = new SerializableDictionary<string, SlotUIDefinition>(slotDefinitions);
        }

        public void OnAfterDeserialize()
        {
            if (serializableSlotDictionary is not null)
            {
                slotDefinitions = serializableSlotDictionary.ToDictionary();
            }
        }


    }

    [Serializable]
    public struct SlotUIDefinition
    {
        public GameObject? cellPrefab;
        public GameObject? disabledCellPrefab;
    }
}