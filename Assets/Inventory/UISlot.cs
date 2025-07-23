using Game.Items;
using System;
using UnityEngine;

namespace Game.Inventory
{
    [Serializable]
    internal class UISlot : MonoBehaviour, IItemContainer
    {
        private void Start()
        {
            inventorySlot.uiSlot = this;
        }
        public GameObject Item => inventorySlot.Item;

        [SerializeReference]
        public InventorySlot inventorySlot;

        public bool PutItem(GameObject item)
        {
            return inventorySlot.PutItem(item);
        }

        public bool RemoveItem(GameObject item = null)
        {
            return inventorySlot.RemoveItem(item);
        }
    }
}
