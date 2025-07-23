using Game.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Inventory
{
    public class InventoryItemUIHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private GameObject originalParent;
        private IItemContainer Slot;
        private Canvas canvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;

        private Transform dragLayer;

        public void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            canvas = GetComponentInParent<Canvas>();
            originalParent = transform.parent.gameObject;
            Slot = originalParent.GetComponent<IItemContainer>();

            if (Slot == null)
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
            //TODO Tidy up this workaround, too tired to do it today.
            if (Slot.RemoveItem(null))
            {
                transform.SetParent(dragLayer, true);
                canvasGroup.blocksRaycasts = false;
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

            GameObject target = eventData.pointerCurrentRaycast.gameObject;
            IItemContainer targetSlot = null;

            if (target != null)
                if (target.GetComponent<EventRedirector>() != null)
                {
                    targetSlot = target.GetComponent<EventRedirector>().redirectTarget?.GetComponent<IItemContainer>();
                    Debug.Log($"Redirecting");
                }
                else { 
                    targetSlot = target.GetComponent<IItemContainer>();
                }

            if (targetSlot != null && targetSlot.PutItem(gameObject))
            {
                ListInventorySlot oriParent = originalParent.GetComponent<UISlot>().inventorySlot as ListInventorySlot;
                if (oriParent != null)
                {
                    oriParent.Target.RedrawContents();
                    originalParent = null;
                }
                InventoryList targetList = targetSlot as InventoryList;
                if (targetList != null) {
                    Destroy(gameObject);
                }
                transform.SetParent(targetSlot.transform, false);
                
                Slot = targetSlot;
            }
            else
            {
                transform.SetParent(originalParent.transform, false);
                Slot.PutItem(gameObject); // Put back in original slot
            }
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
