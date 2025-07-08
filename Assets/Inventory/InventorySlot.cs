using RPGFramework.Inventory;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RPGFramework.Inventory
{
    [Serializable]
    public class InventorySlot
    {
        [SerializeField]
        public bool IsEnabled;
        protected virtual Color BaseColor => Color.white;
        public Color color
        {
            get
            {
                return IsEnabled ? BaseColor : BaseColor * 0.5f;
            }
        }
    }
    [Serializable]
    public class LockedInventorySlot : InventorySlot
    {
        protected override Color BaseColor => Color.red;
        public LockedInventorySlot() => IsEnabled = false;
    }
    [Serializable]
    public class EquipmentSlot : InventorySlot
    {
        protected override Color BaseColor => Color.green;
    }
    [Serializable]
    public class ConsumableSlot : InventorySlot
    {
        protected override Color BaseColor => Color.blue;
    }
    [Serializable]
    public static class InventorySlotUtils
    {
        public static string[] GetSlotTypeNames()
        {
            return Assembly.GetAssembly(typeof(InventorySlot))
                .GetTypes()
                .Where(t => (t.IsSubclassOf(typeof(InventorySlot)) || t.IsAssignableFrom(typeof(InventorySlot))) && !t.IsAbstract)
                .Select(t => t.Name)
                .ToArray();
        }
    }
}
