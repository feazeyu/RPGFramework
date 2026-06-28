using Feazeyu.RPGSystems.Core.Utilities;
using Feazeyu.RPGSystems.Items;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// Handles UI interactions for an inventory item, including drag-and-drop, tooltip display, and pointer events.
    /// </summary>
    [Serializable]
    public class InventoryItemUIHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// The original parent GameObject of this item.
        /// </summary>
        private GameObject originalParent;

        /// <summary>
        /// The slot container interface for this item.
        /// </summary>
        private ISingleItemContainer _slot;

        /// <summary>
        /// The canvas this item is rendered on.
        /// </summary>
        private Canvas canvas;

        /// <summary>
        /// The RectTransform component of this item.
        /// </summary>
        private RectTransform rectTransform;

        /// <summary>
        /// The CanvasGroup component for controlling raycast blocking.
        /// </summary>
        private CanvasGroup canvasGroup;

        /// <summary>
        /// The transform used for dragging items.
        /// </summary>
        private Transform dragLayer;

        /// <summary>
        /// The ID of the item being dragged.
        /// </summary>
        private int draggedId;

        /// <summary>Dragged id.</summary>
        public int DraggedId => draggedId;
        /// <summary>Drop handled.</summary>
        public bool DropHandled { get; set; }

        /// <summary>
        /// Indicates if the cursor is inside the item area.
        /// </summary>
        private bool cursorInside = false;

        /// <summary>
        /// Indicates if the tooltip is currently shown.
        /// </summary>
        private bool tooltipShown = false;

        /// <summary>
        /// Reference to the coroutine displaying the tooltip.
        /// </summary>
        private Coroutine tooltipDisplayCoroutine;

        /// <summary>
        /// Initializes the handler, setting up references and validating components.
        /// </summary>
        public void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvas = GetComponentInParent<Canvas>();
            originalParent = GetOriginalParent();
            _slot = originalParent.GetComponent<ISingleItemContainer>();

            if (_slot == null)
                Debug.LogWarning($"InventoryItem {gameObject.name} is not in an inventory");

            dragLayer = canvas.transform.Find("DragLayer");
            if (dragLayer == null)
            {
                Debug.LogError("No DragLayer found under Canvas");
            }
        }

        /// <summary>
        /// Gets the original parent GameObject of this item.
        /// </summary>
        /// <returns>The parent GameObject.</returns>
        protected virtual GameObject GetOriginalParent()
        {
            return transform.parent.gameObject;
        }

        /// <summary>
        /// Called when dragging begins. Removes the item from its slot and prepares the drag visual.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            HideTooltip();
            originalParent = GetOriginalParent();
            gameObject.transform.SetParent(dragLayer, false);
            int removedItemId = _slot.RemoveItem();
            if (removedItemId != -1)
            {
                draggedId = removedItemId;
                CreateDraggedItem(draggedId);
            }
            else
            {
                eventData.pointerDrag = null;
            }
        }

        /// <summary>
        /// Called during dragging. Updates the item's position to follow the pointer.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        public virtual void OnDrag(PointerEventData eventData)
        {
            if (canvas == null) return;
            var pointer = Pointer.current;
            if (pointer == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                pointer.position.ReadValue(),
                eventData.pressEventCamera,
                out Vector2 localPoint
            );
            rectTransform.localPosition = localPoint;
        }

        /// <summary>
        /// Called when dragging ends. Waits one frame for OnDrop to fire, then returns the item if no drop was handled.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        public virtual void OnEndDrag(PointerEventData eventData)
        {
            StartCoroutine(FinalizeDrop());
        }

        private IEnumerator FinalizeDrop()
        {
            yield return null;
            if (!DropHandled)
                _slot.ReturnItem(InventoryManager.Instance.GetItemById(draggedId));
            Destroy(gameObject);
        }

        /// <summary>
        /// Instantiates the visual representation of the dragged item.
        /// </summary>
        /// <param name="itemId">The ID of the item to create.</param>
        protected void CreateDraggedItem(int itemId)
        {
            GameObject draggedItem = InventoryManager.Instance.GetItemById(draggedId);
            if (draggedItem == null)
            {
                Debug.LogError($"No item prefab found for ID {itemId}");
                return;
            }
            draggedItem = Instantiate(draggedItem, gameObject.transform);
            draggedItem.transform.localPosition = Vector3.zero;
            draggedItem.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            draggedItem.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            draggedItem.GetComponent<RectTransform>().sizeDelta = draggedItem.GetComponent<Image>().sprite.rect.size *100/32;
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Called when the pointer enters the item's area. Starts the tooltip display coroutine.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_slot == null)
            {
                return;
            }
            cursorInside = true;
            tooltipDisplayCoroutine = StartCoroutine(DisplayTooltip(eventData));
        }

        /// <summary>
        /// Called when the pointer exits the item's area. Stops the tooltip display coroutine and hides the tooltip.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            cursorInside = false;
            StopCoroutine(tooltipDisplayCoroutine);
            HideTooltip();
        }

        /// <summary>
        /// The possible pivot corners for tooltip placement.
        /// </summary>
        private static readonly Vector2[] corners = { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };

        /// <summary>
        /// Coroutine to display the tooltip after a delay if the cursor remains inside the item area.
        /// </summary>
        /// <param name="eventData">Pointer event data.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator DisplayTooltip(PointerEventData eventData)
        {
            yield return new WaitForSeconds(0.5f);
            if (cursorInside)
            {
                tooltipShown = true;
                var tooltip = _slot.Item.GetComponent<Item>().DisplayTooltip(canvas.transform);
                if (tooltip != null)
                {
                    RectTransform tooltipRect = tooltip.GetComponent<RectTransform>();
                    if (tooltipRect != null)
                    {
                        tooltipRect.pivot = new Vector2(0f, 1f);
                        Vector2 localPoint;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            canvas.transform as RectTransform,
                            eventData.position,
                            eventData.pressEventCamera,
                            out localPoint
                        );
                        tooltipRect.anchoredPosition = localPoint;

                        foreach (var corner in corners)
                        {
                            tooltipRect.pivot = corner;
                            if (RectBoundCheck.IsElementWithinAnother(canvas.GetComponent<RectTransform>(), tooltipRect))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Hides the tooltip if it is currently shown.
        /// </summary>
        private void HideTooltip()
        {
            if (tooltipShown)
            {
                tooltipShown = false;
                _slot.Item.GetComponent<Item>().HideTooltip();
            }
        }
    }
}
