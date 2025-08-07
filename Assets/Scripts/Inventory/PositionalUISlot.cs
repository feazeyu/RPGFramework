using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Items;

namespace Game.Inventory
{
    [Serializable]
    internal class PositionalUISlot : MonoBehaviour, IUIItemContainer, ISingleItemContainer
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
            Debug.Log($"Putting to {position}");
            bool success = target.PutItem(position, item);
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
    }
}
