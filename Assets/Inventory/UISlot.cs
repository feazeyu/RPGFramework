
using System;
using UnityEngine;

namespace RPGFramework.Inventory
{
    //[HideInInspector]
    [Serializable]
    class UISlot : MonoBehaviour
    {
        [SerializeReference, HideInInspector]
        public InventorySlot invSlot;
    }
}
