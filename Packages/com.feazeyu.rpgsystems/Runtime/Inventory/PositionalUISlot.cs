using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Feazeyu.RPGSystems.Items;

namespace Feazeyu.RPGSystems.Inventory
{
    [Serializable]
    internal class PositionalUISlot : MonoBehaviour, IUIItemContainer, ISingleItemContainer, IDropHandler
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
