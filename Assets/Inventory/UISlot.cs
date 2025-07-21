using System;
using UnityEngine;

namespace Game.Inventory
{
    [Serializable]
    internal class UISlot : MonoBehaviour
    {
        public GameObject Item => inventorySlot.Item;

        [SerializeReference, HideInInspector]
        public InventorySlot inventorySlot;
    }
}
