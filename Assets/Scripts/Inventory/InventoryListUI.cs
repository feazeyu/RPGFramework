using Game.Items;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Inventory
{
    [Serializable]
    public class InventoryListUI: MonoBehaviour, IScrollHandler, IUIPositionalItemContainer
    {
        [SerializeReference]
        public InventoryList list;
        private RectTransform rectTransform;
        private RectTransform slotPrefabRect;
        [Tooltip("Background behind the item name.")]
        public GameObject slotPrefab;
        public Vector2 firstElementPosition = new Vector2(0, 0);
        public Vector2 margin = new Vector2(0, 0);
        private GameObject dragLayer;
        [SerializeField, HideInInspector]
        private GameObject originPoint;
        private void Start()
        {
            if (gameObject.GetComponent<RectTransform>()== null)
            {
                Debug.LogWarning($"The InventoryList {gameObject} is missing a RectTransform. Some functionality may be affected.");
            }
            dragLayer = GameObject.Find("DragLayer");
            if (dragLayer == null)
            {
                Debug.LogWarning($"No DragLayer found, drag and drop won't work.");
            }
            RedrawContents();
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            //Debug.Log($"End drag event on {gameObject.name} at position {eventData.position} with delta {eventData.delta}");
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (rectTransform == null)
            {
                rectTransform = gameObject.GetComponent<RectTransform>();
            }
            if (slotPrefabRect == null)
            {
                slotPrefabRect = slotPrefab.GetComponent<RectTransform>();
            }
            float overflowSize = -rectTransform.sizeDelta.y + list.contents.Count*(slotPrefabRect.sizeDelta.y+margin.y);
            if (overflowSize > 0)
            {
                firstElementPosition += new Vector2(0, eventData.scrollDelta.y * list.scrollSensitivity); // Adjust the scroll sensitivity as needed
                firstElementPosition.y = Mathf.Clamp(firstElementPosition.y, 0, overflowSize);
                originPoint.transform.localPosition = firstElementPosition;
            }
        }
        public void RedrawContents()
        {
            ClearItemSlots();
            CreateOriginPoint();
            GenerateUI();
        }

        public void CreateOriginPoint()
        {
            if (originPoint == null)
            {
                originPoint = transform.Find("OriginPoint")?.gameObject;
            }
            else
            {
                Destroy(originPoint);
            }
            originPoint = new GameObject("OriginPoint");
            originPoint.transform.SetParent(transform, false);
            originPoint.transform.localPosition = firstElementPosition;
        }

        public void GenerateUI()
        {
            int i = 0;
            if (list.contents == null)
            {
                list.contents = new();
            }
            foreach (StackableInventorySlot slot in list.contents)
            {
                DrawSlotUI(slot, i);
                i++;
            }
        }

        private void ClearItemSlots()
        {
            foreach (Transform child in gameObject.transform)
            {
                if (child.GetComponent<InventoryItemUIHandler>() != null)
                {
                    // If the child has a drag handler, it is an inventory slot.
                    Destroy(child.gameObject);
                }
                else if (child.GetComponent<PositionalUISlot>() != null)
                {
                    // If the child has a PositionalUISlot, it is also an inventory slot.
                    Destroy(child.gameObject);
                }
            }
        }
        public void DrawSlotUI(StackableInventorySlot slot, int offset)
        {
            if (slot.Item != null)
            {
#if UNITY_EDITOR
                GameObject slotUIElement = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, originPoint.transform);
#else
                GameObject slotUIElement = Instantiate(slotPrefab, originPoint.transform);  
#endif
                slotUIElement.GetComponent<RectTransform>().anchoredPosition = new Vector3(margin.x*offset, -offset*slotPrefab.transform.GetComponent<RectTransform>().sizeDelta.y-offset*margin.y, 0);
                PositionalUISlot positional = slotUIElement.AddComponent<PositionalUISlot>();
                positional.target = this;
                positional.position = new Vector2Int(0, offset);
                slot.position = new Vector2Int(0, offset);
                if (slotUIElement.TryGetComponent<TextCountItemRenderer>(out var text))
                {
                    text.CountText.text = slot.itemCount.ToString();
                    text.ItemText.text = slot.Item.GetComponent<Item>().info.Name;
                }
                InventoryHelper.CreateUIDragHandler(slotUIElement);
            }
        }
        public bool PutItem(Vector2Int position, GameObject item)
        {
            list.contents ??= new();
            if (list.EnableSlotCapacity)
            {
                StackableInventorySlot existingSlot = null;
                if (position.y >= 0 && position.y < list.contents.Count)
                {
                    existingSlot = list.contents[position.y];
                }
                if (existingSlot != null && existingSlot.PutItem(item))
                {
                    RedrawContents();
                    return true;
                }
            }
            else
            {
                foreach (var slot in list.contents)
                {
                    if (slot.PutItem(item))
                    {
                        RedrawContents();
                        return true;
                    }
                }
            }
            StackableInventorySlot newSlot = new StackableInventorySlot(item);
            if (list.EnableSlotCapacity)
            {
                newSlot.stackSize = list.capacity;
            }
            list.contents.Add(newSlot);
            RedrawContents();
            return true;
        }


        public int RemoveItem(Vector2Int position)
        {
            var itemSlot = list.contents[position.y];
            if (itemSlot != null)
            {
                int itemId = itemSlot.RemoveItem();
                if (itemSlot.itemCount <= 0)
                {
                    list.contents.Remove(itemSlot);
                }
                RedrawContents();
                return itemId;
            }
            return -1;
        }

        public GameObject GetItem(Vector2Int position)
        {
            if (list.contents == null || position.y < 0 || position.y >= list.contents.Count)
            {
                Debug.LogError($"InventoryList: Invalid position {position}. Contents count: {list.contents?.Count ?? 0}");
                return null;
            }
            return list.contents[position.y].Item;
        }
    }
}
