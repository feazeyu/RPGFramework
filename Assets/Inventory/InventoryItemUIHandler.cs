using Game.Utils;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Inventory
{
    [Serializable]
    public class InventoryItemUIHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private GameObject originalParent;
        private IItemContainer _slot;
        private Canvas canvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private IItemContainer _target;
        private Transform dragLayer;
        private InventoryManager inventoryManager;
        private int draggedId;
        public void Start()
        {
            inventoryManager = GameObject.Find("InventoryManager").GetComponent<InventoryManager>();
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvas = GetComponentInParent<Canvas>();
            originalParent = transform.parent.gameObject;
            _slot = originalParent.GetComponent<IItemContainer>();

            if (_slot == null)
                Debug.LogWarning($"InventoryItem {gameObject.name} is not in an inventory");

            // Find DragLayer
            dragLayer = canvas.transform.Find("DragLayer");
            if (dragLayer == null)
            {
                Debug.LogError("No DragLayer found under Canvas");
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            originalParent = transform.parent.gameObject;
            gameObject.transform.SetParent(dragLayer, false);
            int removedItemId = _slot.RemoveItem(null);
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

        public void OnDrag(PointerEventData eventData)
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

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;

            GameObject dragTarget = eventData.pointerCurrentRaycast.gameObject;

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
            if (_target == null || !_target.PutItem(inventoryManager.GetItemById(draggedId)))
            {
                _slot.ReturnItem(inventoryManager.GetItemById(draggedId)); // Return item to original slot
            }
            Destroy(gameObject);
        }

        private void CreateDraggedItem(int itemId)
        {
            GameObject draggedItem = inventoryManager.GetItemById(draggedId);
            if (draggedItem == null)
            {
                Debug.LogError($"No item prefab found for ID {itemId}");
                return;
            }
            draggedItem = Instantiate(draggedItem, gameObject.transform);
            draggedItem.transform.localPosition = Vector3.zero;
            draggedItem.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            draggedItem.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            canvasGroup.blocksRaycasts = false;
        }

    }
}
