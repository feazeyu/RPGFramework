#nullable enable
using Game.Items;
using System;
using UnityEngine;

namespace Game.Inventory
{
    [Serializable]
    internal class UISlot : MonoBehaviour, IUIItemContainer
    {
        public GameObject? Item => inventorySlot.Item;

        [SerializeReference]
        public InventorySlot inventorySlot;

        public virtual bool PutItem(GameObject item)
        {
            bool success = inventorySlot.PutItem(item);
            RedrawContents();
            return success;
        }

        public virtual int RemoveItem(GameObject? item = null)
        {
            int removedId = inventorySlot.RemoveItem(item);
            RedrawContents();
            return removedId;
        }

        public virtual void ReturnItem(GameObject item) {
            ((IItemContainer)inventorySlot).ReturnItem(item);
            RedrawContents();
        }

        void IItemContainer.ReturnItem(GameObject item)
        {
            ReturnItem(item);
        }

        public virtual void RedrawContents() {
            foreach (Transform child in transform)
            {
                Item? itemComponent = child.GetComponent<Item>();
                if (itemComponent != null) {
                    Destroy(child.gameObject);
                }
            }
            if (Item != null)
            {
                var newitem = Instantiate(Item, transform, false);
                InventoryHelper.CreateUIDragHandler(gameObject);
                newitem.transform.SetAsFirstSibling();
            }

        }
    }

    
}
