using Game.Utils;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#nullable enable
namespace Game.Inventory
{
    [ExecuteInEditMode, Serializable]
    public class InventoryUIGenerator : MonoBehaviour, ISerializationCallbackReceiver
    {
        [Header("UI Prefab Settings")]
        public Dictionary<string, SlotUIDefinition> slotDefinitions = new();
        [Tooltip("Canvas to generate to")]
        public Canvas? target;

        [Header("Cell Layout Settings")]
        public Vector2 cellSize = new(100, 100);
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
            grid.cellSize = cellSize;
            grid.spacing = spacing;
            grid.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = inventoryGrid.columns;

            for (int y = 0; y < inventoryGrid.rows; y++)
            {
                for (int x = 0; x < inventoryGrid.columns; x++)
                {
                    InventorySlot cell = inventoryGrid.Cells[x, y];
                    SlotUIDefinition definition = slotDefinitions[cell.GetType().Name];

                    if (definition.cellPrefab == null || definition.disabledCellPrefab == null)
                    {
                        Debug.LogWarning($"InventoryUIGenerator: Cell at ({x}, {y}) is missing a prefab.");
                        continue;
                    }
                    GameObject selectedPrefab = cell.IsEnabled ? definition.cellPrefab : definition.disabledCellPrefab;
                    GameObject cellInstance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab, root.transform);
                    cellInstance.name = $"Cell_{x}_{y}";

                    UISlot slot = cellInstance.AddComponent<UISlot>();
                    slot.inventorySlot = cell;
                    if (slot.Item != null)
                    {
                        //Debug.Log(uislot.invSlot.Item);
                        Instantiate(slot.Item, cellInstance.transform, false);
                        //Debug.Log("Generated item");
                    }
                }
            }
            GenerateDragLayer();
            lastGeneratedRoot = root;
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

        private void GenerateDragLayer()
        {
            if (target == null)
            {
                Debug.LogError("InventoryUIGenerator: Target Canvas is not set.");
                return;
            }
            Transform existing = target.transform.Find("DragLayer");
            if (existing != null)
            {
                existing.transform.SetAsLastSibling();
                return;
            }

            GameObject dragLayer = new("DragLayer", typeof(RectTransform), typeof(CanvasRenderer));
            RectTransform rectTransform = dragLayer.GetComponent<RectTransform>();
            dragLayer.transform.SetParent(target.transform, false);
            dragLayer.layer = LayerMask.NameToLayer("UI");
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            dragLayer.transform.SetAsLastSibling();

            Undo.RegisterCreatedObjectUndo(dragLayer, "Create DragLayer");

            Debug.Log("DragLayer created. It is used for handling item drag and drop interactions, do not delete if u don't know what you're doing.");
        }
    }

    [Serializable]
    public struct SlotUIDefinition
    {
        public GameObject? cellPrefab;
        public GameObject? disabledCellPrefab;
    }
}
