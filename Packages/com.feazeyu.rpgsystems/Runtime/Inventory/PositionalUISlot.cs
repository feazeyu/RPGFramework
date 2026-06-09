using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Feazeyu.RPGSystems.Items;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// A reusable UI cell that bridges a single drawn slot to an
    /// <see cref="IUIPositionalItemContainer"/>. It is both the drag source
    /// (<see cref="ISingleItemContainer"/>) and the drop target
    /// (<see cref="IDropHandler"/>) for the item rendered in that cell, so any
    /// inventory — grid, list, or a custom layout — gets drag-and-drop by
    /// attaching one of these per cell.
    ///
    /// Wire it in code (the <see cref="target"/> field is an interface and so is
    /// not Inspector-serializable): add the component, then set
    /// <see cref="target"/> to your container and <see cref="position"/> to the
    /// cell's address. Build the item visual with
    /// <see cref="InventoryHelper.CreateUIDragHandler"/> and ensure a DragLayer
    /// exists via <see cref="InventoryHelper.GenerateDragLayer"/>.
    /// </summary>
    [Serializable]
    public class PositionalUISlot : MonoBehaviour, IUIItemContainer, ISingleItemContainer, IDropHandler
    {
        public Vector2Int position;
        public IUIPositionalItemContainer target;
        public virtual GameObject Item
        {
            get
            {
                if (target == null)
                {
                    Debug.LogError("PositionalUISlot: Target is not set.");
                    return null;
                }
                return target.GetItem(position);
            }
        }
        public bool PutItem(GameObject item)
        {
            bool success = target.PutItem(position, item);
            if (success)
                RedrawContents();
            return success;
        }

        public int RemoveItem()
        {
            int removedId = target.RemoveItem(position);
            RedrawContents();
            return removedId;
        }

        public void ReturnItem(GameObject item)
        {
            target.ReturnItem(position, item);
            RedrawContents();
        }
        public virtual void RedrawContents()
        {
            target.RedrawContents();
        }

        public void OnDrop(PointerEventData eventData)
        {
            var handler = eventData.pointerDrag?.GetComponent<InventoryItemUIHandler>();
            if (handler == null || handler.DropHandled) return;
            var item = InventoryManager.Instance.GetItemById(handler.DraggedId);
            if (item != null && PutItem(item))
                handler.DropHandled = true;
        }
    }
}
