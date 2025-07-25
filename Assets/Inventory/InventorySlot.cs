#nullable enable
using Game.Items;
using System;
using UnityEngine;

namespace Game.Inventory
{
    public interface HideInSelections { };
    [Serializable]
    public class InventorySlot : IItemContainer
    {
        public InventorySlot(GameObject item)
        {
            ItemId = item.GetComponent<Item>().id;
        }
        public InventorySlot(int id)
        {
            ItemId = id;
        }
        public InventorySlot(){
            ItemId = -1;
        }
        [SerializeField]
        public bool IsEnabled = true;
        protected virtual Color BaseColor => Color.white;
        protected string defaultUILabel = "I";
        [HideInInspector]
        public string editorUILabel = "";
        [SerializeField]
        private int _itemId;
        public int ItemId{ 
            get 
            {
                return _itemId;
            }
            private set 
            {
                _itemId = value;
            } 
        }
        private InventoryManager? _inventoryManager;
        public GameObject? Item
        {
            get
            {
                if (_inventoryManager == null)
                {
                    _inventoryManager = GameObject.Find("InventoryManager").GetComponent<InventoryManager>();
                }
                return _inventoryManager.GetItemById(ItemId);
            }
        }
        // These methods are here only for use in the editor, to be able to force items into slots they don't belong.
        // And slots like the locked ones, which cannot be edited by the player.
#if UNITY_EDITOR
        public virtual bool EditorOnlyPutItem(GameObject item)
        {
            if (item == null) return false;
            editorUILabel = defaultUILabel;
            ItemId = item.GetComponent<Item>().id;
            return true;
        }

        public virtual bool EditorOnlyRemoveItem()
        {
            editorUILabel = "";
            ItemId = -1;
            return true;
        }
#endif
        public virtual bool PutItem(GameObject item)
        {
            if (ItemId != -1 || !IsEnabled) return false;
            editorUILabel = defaultUILabel;
            ItemId = item.GetComponent<Item>().id;
            return true;
        }

        public virtual int RemoveItem(GameObject? item = null)
        {
            editorUILabel = "";
            int removedItemId = ItemId;
            ItemId = -1;
            return removedItemId;
        }

        public Color Color => IsEnabled ? BaseColor : BaseColor * 0.5f;

    }

    [Serializable]
    public class LockedInventorySlot : InventorySlot
    {
        public LockedInventorySlot(GameObject item) : base(item)
        {
        }
        public LockedInventorySlot(int id) : base(id)
        {
        }
        public LockedInventorySlot() : base()
        {
        }
        protected override Color BaseColor => Color.red;
        public override bool PutItem(GameObject item) => false;
        public override int RemoveItem(GameObject? item) => -1;
    }

}
