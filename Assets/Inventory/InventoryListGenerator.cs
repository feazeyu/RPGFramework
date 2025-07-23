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
        private List<ListInventorySlot> contents;
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
        private void GenerateInventoryObject() {
            InventoryList target = null;
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
            Utils.EventRedirector.AddEventRedirector(inventoryObject, inventoryObject);
            int i = 0;
            foreach (InventorySlot slot in contents)
            {
                DrawSlotUI(slot, i);
                i++;
            }
        }
        protected void DrawSlotUI(InventorySlot slot, int offset) { 
            if(slot.Item != null)
            {
                var slotUIElement = Instantiate(slotPrefab, inventoryObject.GetComponent<RectTransform>());
                slotUIElement.GetComponent<RectTransform>().anchoredPosition = new Vector3(firstElementPosition.x + margin.x*offset, firstElementPosition.y-offset * slotPrefab.transform.GetComponent<RectTransform>().sizeDelta.y-offset*margin.y, 0);
                slotUIElement.AddComponent<UISlot>().inventorySlot = slot;
                if (slot.Item != null) { 
                    Instantiate(slot.Item, slotUIElement.transform);
                }
            }
        }


    }
}
