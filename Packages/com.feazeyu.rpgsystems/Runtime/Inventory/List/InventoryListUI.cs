using Feazeyu.RPGSystems.Items;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// Manages the UI for an inventory list, including slot creation, drawing, scrolling, and item manipulation.
    /// </summary>
    [Serializable]
    public class InventoryListUI : MonoBehaviour, IScrollHandler, IItemContainer, IDropHandler
    {
        /// <summary>
        /// The inventory list data source.
        /// </summary>
        [SerializeReference]
        public InventoryList list;

        /// <summary>
        /// Cached RectTransform of this UI.
        /// </summary>
        private RectTransform rectTransform;

        /// <summary>
        /// Cached RectTransform of the slot prefab.
        /// </summary>
        private RectTransform slotPrefabRect;

        /// <summary>
        /// Prefab for the background behind the item name.
        /// </summary>
        [Tooltip("Background behind the item name.")]
        public GameObject slotPrefab;

        /// <summary>
        /// The position of the first element in the inventory UI.
        /// </summary>
        [HideInInspector]
        public Vector2 firstElementPosition = new Vector2(0, 0);

        /// <summary>
        /// Margin between inventory slots.
        /// </summary>
        public Vector2 margin = new Vector2(0, 0);

        /// <summary>
        /// The drag layer GameObject for drag-and-drop operations.
        /// </summary>
        private GameObject dragLayer;

        /// <summary>
        /// The origin point GameObject for slot positioning.
        /// </summary>
        [SerializeField, HideInInspector]
        private GameObject originPoint;

        /// <summary>
        /// Unity Start method. Initializes components and draws the inventory contents.
        /// </summary>
        private void Start()
        {
            if (gameObject.GetComponent<RectTransform>() == null)
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

        public void OnDrop(PointerEventData eventData)
        {
            var handler = eventData.pointerDrag?.GetComponent<InventoryItemUIHandler>();
            if (handler == null || handler.DropHandled) return;
            var item = InventoryManager.Instance.GetItemById(handler.DraggedId);
            if (item != null && PutItem(item))
                handler.DropHandled = true;
        }

        /// <summary>
        /// Handles scroll events to move the inventory UI vertically.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
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
            float overflowSize = -rectTransform.sizeDelta.y + list.contents.Count * (slotPrefabRect.sizeDelta.y + margin.y);
            if (overflowSize > 0)
            {
                firstElementPosition += new Vector2(0, eventData.scrollDelta.y * list.scrollSensitivity);
                firstElementPosition.y = Mathf.Clamp(firstElementPosition.y, 0, overflowSize);
                originPoint.transform.localPosition = OriginBase() + (Vector3)firstElementPosition;
            }
        }

        /// <summary>
        /// Redraws the inventory UI contents.
        /// </summary>
        public void RedrawContents()
        {
            ClearItemSlots();
            CreateOriginPoint();
            GenerateUI();
        }

        /// <summary>
        /// Creates or recreates the origin point GameObject for slot positioning.
        /// </summary>
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
            originPoint.transform.localPosition = OriginBase() + (Vector3)firstElementPosition;
        }

        /// <summary>
        /// Top-left inside corner of this container's rect, in local space. Item rows
        /// are laid out downward/rightward from here, so they stay inside the
        /// RectMask2D no matter which corner the inventory is anchored/pivoted to.
        /// (Previously the origin sat at the pivot, so only top-anchored layouts
        /// drew their rows inside the mask.)
        /// </summary>
        private Vector3 OriginBase()
        {
            if (transform is RectTransform rt)
            {
                var r = rt.rect;
                return new Vector3(r.xMin, r.yMax, 0f);
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Generates the UI elements for each inventory slot.
        /// </summary>
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

        /// <summary>
        /// Removes all item slot UI elements from the inventory UI.
        /// </summary>
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

        public bool PutItem(GameObject item) => list != null && list.PutItem(new Vector2Int(-1, -1), item);
        public void ReturnItem(GameObject item) => list?.PutItem(new Vector2Int(-1, -1), item);

        /// <summary>
        /// Draws a single inventory slot UI element.
        /// </summary>
        /// <param name="slot">The inventory slot to draw.</param>
        /// <param name="offset">The vertical offset index for positioning.</param>
        public void DrawSlotUI(StackableInventorySlot slot, int offset)
        {
            if (slot.Item != null)
            {
#if UNITY_EDITOR
                GameObject slotUIElement = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, originPoint.transform);
#else
                    GameObject slotUIElement = Instantiate(slotPrefab, originPoint.transform);  
#endif
                slotUIElement.GetComponent<RectTransform>().anchoredPosition = new Vector3(margin.x * offset, -offset * slotPrefab.transform.GetComponent<RectTransform>().sizeDelta.y - offset * margin.y, 0);
                PositionalUISlot positional = slotUIElement.AddComponent<PositionalUISlot>();
                positional.target = list;
                positional.position = new Vector2Int(0, offset);
                slot.position = new Vector2Int(0, offset);
                if (slotUIElement.TryGetComponent<TextCountItemRenderer>(out var text))
                {
                    text.CountText.text = slot.infinite ? "∞" : slot.itemCount.ToString();
                    text.ItemText.text = list.GetItemLabel(slot);
                }
                InventoryHelper.CreateUIDragHandler(slotUIElement);
            }
        }

    }
}
