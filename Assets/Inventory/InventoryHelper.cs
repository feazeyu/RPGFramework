using System.Linq;
using System.Reflection;

#nullable enable
namespace Game.Inventory
{
    public static class InventoryHelper
    {
        private static string[]? slotNames;

        public static string[] GetSlotTypeNames()
        {
            return slotNames ??= Assembly.GetAssembly(typeof(InventorySlot))
                .GetTypes()
                .Where(t => (t.IsSubclassOf(typeof(InventorySlot)) || t.IsAssignableFrom(typeof(InventorySlot))) && !t.IsAbstract)
                .Select(t => t.Name)
                .ToArray();
        }
    }
}
