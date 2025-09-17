#nullable enable
using Game.Core.Utilities;
using Game.Items;
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
        public Vector2 spacing = new(0, 0);

        [Header("UI Position Settings")]
        [Tooltip("Position of the generated UI from the top left (in pixels)")]
        public Vector2 uiPosition = new(0, 0);

        [HideInInspector, SerializeField]
        private GameObject? lastGeneratedRoot;

        [SerializeField, HideInInspector]
        private SerializableDictionary<string, SlotUIDefinition>? serializableSlotDictionary;

        public void OnEnable()
        {
            ReloadSlotTypes();
        }

        /// <summary>
        /// Reloads the slot types from the InventoryHelper and ensures all slot definitions are present.
        /// </summary>
        private void ReloadSlotTypes()
        {
            foreach (string typeName in InventoryHelper.GetSlotTypeNames())
            {
                slotDefinitions.TryAdd(typeName, default);
            }
        }

        /// <summary>
        /// Generates the inventory UI grid based on the current InventoryGrid configuration.
        /// This method creates and arranges UI slot elements as children of the specified target Canvas,
        /// using the defined slot UI prefabs and layout settings. It cleans up any previously generated UI,
        /// instantiates new slot UI elements for each cell in the grid, and sets up drag layers for item interaction.
        /// </summary>
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

            // Set anchors to top-left, but position with uiPosition
            rootRect.anchorMin = new Vector2(0, 1);
            rootRect.anchorMax = new Vector2(0, 1);
            rootRect.pivot = new Vector2(0, 1);
            rootRect.anchoredPosition = new Vector2(uiPosition.x, -uiPosition.y); // Y is negative for top-left origin

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

        /// <summary>
        /// Generates a single slot UI element for the specified cell in the inventory grid.
        /// </summary>
        /// <param name="inventoryGrid">The inventory grid containing the cell.</param>
        /// <param name="root">The root GameObject to parent the slot UI element to.</param>
        /// <param name="x">The column index of the cell.</param>
        /// <param name="y">The row index of the cell.</param>
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

        /// <summary>
        /// Creates and attaches the item UI to the specified slot, if an item exists at the slot's position.
        /// Handles drag handler setup and anchor redirection for non-anchor slots.
        /// </summary>
        /// <param name="grid">The inventory grid.</param>
        /// <param name="slot">The UI slot to attach the item to.</param>
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
                itemInstance.GetComponent<Item>().CalculatePivot();
                InventoryHelper.CreateUIDragHandler(slot.gameObject);

            }
            else if (anchor != new Vector2Int(-1, -1))
            {
                var redirect = InventoryHelper.CreateUIDragHandler(slot.gameObject, true);
                redirect.GetComponent<InventoryItemUIRedirectingHandler>().targetPosition = anchor;
            }
        }

        /// <summary>
        /// Called before Unity serializes this object. Serializes the slotDefinitions dictionary.
        /// </summary>
        public void OnBeforeSerialize()
        {
            serializableSlotDictionary = new SerializableDictionary<string, SlotUIDefinition>(slotDefinitions);
        }

        /// <summary>
        /// Called after Unity deserializes this object. Restores the slotDefinitions dictionary.
        /// </summary>
        public void OnAfterDeserialize()
        {
            if (serializableSlotDictionary is not null)
            {
                slotDefinitions = serializableSlotDictionary.ToDictionary();
            }
        }

        /// <summary>
        /// Sets the active state of the generated inventory UI.
        /// </summary>
        /// <param name="active">True to activate, false to deactivate.</param>
        public void SetInventoryActiveState(bool active)
        {
            if (lastGeneratedRoot)
                lastGeneratedRoot.SetActive(active);
        }

        /// <summary>
        /// Toggles the active state of the generated inventory UI.
        /// </summary>
        public void ToggleInventoryActiveState()
        {
            if (lastGeneratedRoot)
                SetInventoryActiveState(!lastGeneratedRoot.activeSelf);
        }
    }




    [Serializable]
    public struct SlotUIDefinition
    {
        public GameObject? cellPrefab;
        public GameObject? disabledCellPrefab;
    }
}