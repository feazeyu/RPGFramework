using System.Collections.Generic;
using UnityEngine;
using Game.Utils;
using UnityEngine.EventSystems;
namespace Game.Inventory
{
    public class InventoryListGenerator : MonoBehaviour
    {
        public int capacity = 20;
        [SerializeField]
        private List<InventorySlot> contents;
        [Tooltip("Background behind the item name.")]
        public GameObject slotPrefab;
        public Canvas targetCanvas;
        [SerializeField, HideInInspector]
        private GameObject inventoryObject;
        [Tooltip("If unset, will generate an empty object.")]
        public GameObject inventoryContainerOverride;
        [Tooltip("Set first inventory element's position relative to the resulting inventory object")]
        public Vector2 firstElementPosition = new Vector2(0, 0);
        public Vector2 margin = new Vector2(0, 0);
        private InventoryList target;
        private void GenerateInventoryObject() {
            if (inventoryContainerOverride == null)
            {
                inventoryObject = Instantiate(new GameObject(), targetCanvas.transform);
                inventoryObject.name = "ListInventory";
                target = inventoryObject.AddComponent<InventoryList>();
                target.gameObject.AddComponent<RectTransform>();
                target.margin = margin;
                target.firstElementPosition = firstElementPosition;
            }
            else 
            {
                inventoryObject = Instantiate(inventoryContainerOverride, targetCanvas.transform);
                target = inventoryObject.GetComponent<InventoryList>();
            }
            if (target!=null) { 
                target.capacity = capacity;
                target.contents = contents;
                target.slotPrefab = slotPrefab;
            }
        }
        public void RedrawContents()
        {
            if (inventoryObject == null) {
                GenerateInventoryObject();
            }
            while (inventoryObject.transform.childCount > 0)
            {
                DestroyImmediate(inventoryObject.transform.GetChild(0).gameObject);
            }
            if (target == null) {
                target = inventoryObject.GetComponent<InventoryList>();
            }
            target.GenerateUI();
        }

    }
}
