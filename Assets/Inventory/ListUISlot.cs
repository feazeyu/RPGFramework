using UnityEngine;

namespace Game.Inventory
{
    internal class ListUISlot : UISlot
    {
        public InventoryList target;
        public override void RedrawContents()
        {
            target.RedrawContents();
        }
        public override bool PutItem(GameObject item)
        {
            bool success = target.PutItem(item);
            RedrawContents();
            return success;
        }

        public override int RemoveItem(GameObject item = null)
        {
            int removedId = target.RemoveItem(Item);
            RedrawContents();
            return removedId;
        }

        public override void ReturnItem(GameObject item)
        {
            ((IItemContainer)target).ReturnItem(item);
            RedrawContents();
        }
    }
}
