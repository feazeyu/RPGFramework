using Game.Core.Utilities;
using Game.Items;
using Game.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Inventory
{
    [Serializable]
    public class InventoryItemUIHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private GameObject originalParent;
        private ISingleItemContainer _slot;
        private Canvas canvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private IItemContainer _target;
        private Transform dragLayer;
        private int draggedId;
        private bool cursorInside = false;
        private bool tooltipShown = false;
        private Coroutine tooltipDisplayCoroutine;
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

            // Find DragLayer
            dragLayer = canvas.transform.Find("DragLayer");
            if (dragLayer == null)
            {
                Debug.LogError("No DragLayer found under Canvas");
            }
        }

        protected virtual GameObject GetOriginalParent()
        {
            return transform.parent.gameObject;
        }

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
                eventData.pointerDrag = null; // Prevents the item from being dragged if it cannot be removed
            }
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (canvas == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint
            );
            rectTransform.localPosition = localPoint;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            GameObject dragTarget = GetDragTarget(eventData);

            if (dragTarget != null)
            {
                if (dragTarget.GetComponent<EventRedirector>() != null)
                {
                    _target = dragTarget.GetComponent<EventRedirector>().redirectTarget.GetComponent<IItemContainer>();
                    Debug.Log($"Redirecting");
                }
                else
                {
                    _target = dragTarget.GetComponent<IItemContainer>();
                }
            }
            if (_target == null || !_target.PutItem(InventoryManager.Instance.GetItemById(draggedId)))
            {
                _slot.ReturnItem(InventoryManager.Instance.GetItemById(draggedId)); // Return item to original slot
            }
            Destroy(gameObject);
        }

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
            //TODO Remove magic multiplier
            draggedItem.GetComponent<RectTransform>().sizeDelta = draggedItem.GetComponent<Image>().sprite.rect.size *100/32;
            canvasGroup.blocksRaycasts = false;
        }

        protected GameObject GetDragTarget(PointerEventData eventData)
        {
            ItemInfo itemInfo = InventoryManager.Instance.GetItemById(draggedId).GetComponent<Item>().info;

            if (canvas == null)
                return null;
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var result in results)
            {
                if (result.gameObject != null && result.isValid && result.gameObject.GetComponent<InventoryItemUIHandler>() == null)
                    return result.gameObject;
            }

            return null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_slot == null)
            {
                return; // No item to display tooltip for
            }
            cursorInside = true;
            tooltipDisplayCoroutine = StartCoroutine(DisplayTooltip(eventData));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            cursorInside = false;
            StopCoroutine(tooltipDisplayCoroutine);
            HideTooltip();
        }
        private static readonly Vector2[] corners = {new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(1f,1f), new Vector2(0f,1f)};
        private IEnumerator<WaitForSeconds> DisplayTooltip(PointerEventData eventData)
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
                        // Set the pivot to top-left by default (can be adjusted for better positioning)
                        tooltipRect.pivot = new Vector2(0f, 1f);

                        // Convert screen position to local position in canvas
                        Vector2 localPoint;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            canvas.transform as RectTransform,
                            eventData.position,
                            eventData.pressEventCamera,
                            out localPoint
                        );

                        // Set the tooltip position to the mouse cursor position
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
