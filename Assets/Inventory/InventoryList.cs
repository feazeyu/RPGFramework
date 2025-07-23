using Game.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Inventory
{
    [Serializable]
    public class InventoryList : MonoBehaviour, IEndDragHandler, IScrollHandler, IItemContainer
    {
        public int capacity = 20;
        [SerializeReference]
        public List<ListInventorySlot> contents = new();
        [Tooltip("Background behind the item name.")]
        public GameObject slotPrefab;
        public Vector2 firstElementPosition = new Vector2(0, 0);
        public Vector2 margin = new Vector2(0, 0);
        private GameObject dragLayer;
        private void Start()
        {
            if (gameObject.GetComponent<RectTransform>()== null) {
                Debug.LogWarning($"The InventoryList {gameObject} is missing a RectTransform. Some functionality may be affected.");
            }
            dragLayer = GameObject.Find("DragLayer");
            if (dragLayer == null) {
                Debug.LogWarning($"No DragLayer found, drag and drop won't work.");
            }
            foreach (var slot in contents)
            {
                if (slot == null)
                {
                    Debug.LogWarning("Found a null slot in the contents of InventoryList. This may cause issues.");
                    continue;
                }
                slot.Target = this;
            }
            RedrawContents();
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log($"End drag event on {gameObject.name} at position {eventData.position} with delta {eventData.delta}");
        }

        public void OnScroll(PointerEventData eventData)
        {
            Debug.Log($"Scroll event on {gameObject.name} with delta {eventData.scrollDelta}");
        }
        public void RedrawContents()
        {
            
            foreach (Transform child in gameObject.transform)
            {
                Destroy(child.gameObject);
            }
            Utils.EventRedirector.AddEventRedirector(gameObject, gameObject);
            int i = 0;
            foreach (InventorySlot slot in contents)
            {
                DrawSlotUI(slot, i);
                i++;
            }
        }
        protected void DrawSlotUI(InventorySlot slot, int offset)
        {
            if (slot.Item != null)
            {
                var slotUIElement = Instantiate(slotPrefab, gameObject.GetComponent<RectTransform>());
                slotUIElement.GetComponent<RectTransform>().anchoredPosition = new Vector3(firstElementPosition.x + margin.x*offset, firstElementPosition.y-offset * slotPrefab.transform.GetComponent<RectTransform>().sizeDelta.y-offset*margin.y, 0);
                slotUIElement.AddComponent<UISlot>().inventorySlot = slot;
                ((ListInventorySlot)slotUIElement.GetComponent<UISlot>().inventorySlot).Target = this;
                if (slot.Item!=null) { 
                    Instantiate(slot.Item, slotUIElement.transform);
                }
            }
        }
        public bool PutItem(GameObject item)
        {
            if (contents.Count < capacity) {
                var newSlot = new ListInventorySlot(item);
                newSlot.Target = this;
                contents.Add(newSlot);
                RedrawContents();
                return true;
            }
            return false;
        }

        public bool RemoveItem(GameObject item)
        {
            var itemSlot = FindSlotByItem(item);
            if (itemSlot != null) { 
                contents.Remove(itemSlot);
                return true;
            }
            return false;
        }

        private ListInventorySlot FindSlotByItem(GameObject item) {
            foreach (var slot in contents)
            {
                if (slot.Item != null && slot.Item == item)
                {
                    return slot;
                }
            }
            return null;
        }
    }
}
