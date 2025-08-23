#nullable enable
using Game.Items;
using System;
using UnityEngine;

namespace Game.Inventory
{
    public interface IHideInSelections { };
    [Serializable]
    public class InventorySlot : ISingleItemContainer
    {
        public Vector2Int position = Vector2Int.zero;

        [HideInInspector] //Leads to the main item slot
        public Vector2Int anchorPosition = new(-1, -1);

        #region Constructors
        public InventorySlot(GameObject item)
        {
            ItemId = item.GetComponent<Item>().info.id;
            IsEnabled = true;
        }
        public InventorySlot(GameObject item, Vector2Int position)
        {
            ItemId = item.GetComponent<Item>().info.id;
            this.position = position;
            IsEnabled = true;
        }
        public InventorySlot(int id, Vector2Int position)
        {
            ItemId = id;
            this.position = position;
            IsEnabled = true;
        }
        public InventorySlot(int id)
        {
            ItemId = id;
            IsEnabled = true;
        }
        public InventorySlot(Vector2Int position)
        {
            ItemId = -1;
            this.position = position;
            IsEnabled = true;
        }
        public InventorySlot()
        {
            ItemId = -1;
            IsEnabled = true;
        }
        #endregion
        //[HideInInspector]
        public bool IsEnabled = true;
        protected virtual Color BaseColor => Color.white;
        [SerializeField]
        private int _itemId;
        [HideInInspector]
        public string EditorUILabel
        {
            get
            {
                if (anchorPosition.x != -1 && anchorPosition.y != -1)
                {
                    return "A";
                }
                if (_itemId == -1)
                {
                    return "";
                }

                return "I"
                //Might have too much performance impact    
                //return ItemId.ToString();
                ;
            }
        }
        public int ItemId
        {
            get
            {
                return _itemId;
            }
            protected set
            {
                _itemId = value;
            }
        }
        public GameObject? Item
        {
            get
            {
                return InventoryManager.Instance.GetItemById(ItemId);
            }
        }
        // These methods are here only for use in the editor, to be able to force items into slots they don't belong.
        // And slots like the locked ones, which cannot be edited by the player.
#if UNITY_EDITOR
        public virtual bool EditorOnlyPutItem(GameObject item)
        {
            if (item == null) return false;
            ItemId = item.GetComponent<Item>().info.id;
            return true;
        }

        public virtual bool EditorOnlyRemoveItem()
        {
            ItemId = -1;
            anchorPosition = new Vector2Int(-1, -1);
            return true;
        }
#endif
        public virtual bool PutItem(GameObject item)
        {
            if (item == null) return false;
            if (!AcceptsItem()) return false;
            ItemId = item.GetComponent<Item>().info.id;
            return true;
        }
        public virtual bool AcceptsItem()
        {
            if (ItemId != -1 || !IsEnabled || anchorPosition != new Vector2Int(-1, -1)) return false;
            return true;
        }



        public virtual int RemoveItem()
        {
            if (!IsEnabled) return -1;
            int removedItemId = ItemId;
            ItemId = -1;
            anchorPosition = new Vector2Int(-1, -1);
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
        public override int RemoveItem() => -1;
    }

    [Serializable]
    public class StackableInventorySlot : InventorySlot
    {
        public int stackSize = -1;
        public int itemCount;
        public StackableInventorySlot() : base()
        {
            IsEnabled = true;
        }
        public StackableInventorySlot(GameObject item) : base(item)
        {
            itemCount = 1;
            IsEnabled = true;
        }
        public StackableInventorySlot(int id) : base(id)
        {
            itemCount = 1;
            IsEnabled = true;
        }
        protected override Color BaseColor => Color.green;
        public override bool PutItem(GameObject item)
        {
            if (!IsEnabled) return false;
            if ((itemCount < stackSize || stackSize ==-1) && (ItemId == -1 || ItemId == item.GetComponent<Item>().info.id))
            {
                ItemId = item.GetComponent<Item>().info.id;
                itemCount++;
                return true;
            }
            return false;
        }
        public override int RemoveItem()
        {
            if (!IsEnabled || itemCount <= 0) return -1;
            int removedItemId = ItemId;
            itemCount--;
            if (itemCount <= 0)
            {
                ItemId = -1;
            }
            return removedItemId;
        }
    }

}
