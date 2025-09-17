#nullable enable
using Game.Items;
using System;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// Marker interface to hide in selection lists.
    /// </summary>
    public interface IHideInSelections { };
    /// <summary>
    /// Represents a single inventory slot that can hold one item.
    /// </summary>
    [Serializable]
    public class InventorySlot : ISingleItemContainer
    {
        /// <summary>
        /// The position of the slot in the inventory.
        /// </summary>
        public Vector2Int position = Vector2Int.zero;

        /// <summary>
        /// The position of the main itemslot for multislot items. If it is not -1,-1, the item ui is not drawn
        /// </summary>
        [HideInInspector]
        public Vector2Int anchorPosition = new(-1, -1);

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="InventorySlot"/> class with the specified item.
        /// </summary>
        /// <param name="item">The item to place in the slot.</param>
        public InventorySlot(GameObject item)
        {
            ItemId = item.GetComponent<Item>().info.id;
            IsEnabled = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InventorySlot"/> class with the specified item and position.
        /// </summary>
        /// <param name="item">The item to place in the slot.</param>
        /// <param name="position">The position of the slot.</param>
        public InventorySlot(GameObject item, Vector2Int position)
        {
            ItemId = item.GetComponent<Item>().info.id;
            this.position = position;
            IsEnabled = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InventorySlot"/> class with the specified item id and position.
        /// </summary>
        /// <param name="id">The item id to place in the slot.</param>
        /// <param name="position">The position of the slot.</param>
        public InventorySlot(int id, Vector2Int position)
        {
            ItemId = id;
            this.position = position;
            IsEnabled = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InventorySlot"/> class with the specified item id.
        /// </summary>
        /// <param name="id">The item id to place in the slot.</param>
        public InventorySlot(int id)
        {
            ItemId = id;
            IsEnabled = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InventorySlot"/> class with the specified position.
        /// </summary>
        /// <param name="position">The position of the slot.</param>
        public InventorySlot(Vector2Int position)
        {
            ItemId = -1;
            this.position = position;
            IsEnabled = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InventorySlot"/> class.
        /// </summary>
        public InventorySlot()
        {
            ItemId = -1;
            IsEnabled = true;
        }
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether the slot is enabled.
        /// </summary>
        public bool IsEnabled = true;

        /// <summary>
        /// Gets the base color of the slot. This affects only the editor ui
        /// </summary>
        protected virtual Color BaseColor => Color.white;

        [SerializeField]
        private int _itemId;

        /// <summary>
        /// Gets a label for the editor UI.
        /// </summary>
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

                return "I";
                // Might have too much performance impact    
                // return ItemId.ToString();
            }
        }

        /// <summary>
        /// Gets the item id in the slot.
        /// </summary>
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

        /// <summary>
        /// Gets the item GameObject in the slot, or null if empty.
        /// </summary>
        public GameObject? Item
        {
            get
            {
                return InventoryManager.Instance.GetItemById(ItemId);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Puts an item in the slot for editor purposes, bypassing normal checks.
        /// </summary>
        /// <param name="item">The item to put in the slot.</param>
        /// <returns>True if the item was placed; otherwise, false.</returns>
        public virtual bool EditorOnlyPutItem(GameObject item)
        {
            if (item == null) return false;
            ItemId = item.GetComponent<Item>().info.id;
            return true;
        }

        /// <summary>
        /// Removes the item from the slot for editor purposes.
        /// </summary>
        /// <returns>True if the item was removed; otherwise, false.</returns>
        public virtual bool EditorOnlyRemoveItem()
        {
            ItemId = -1;
            anchorPosition = new Vector2Int(-1, -1);
            return true;
        }
#endif

        /// <summary>
        /// Attempts to put an item in the slot.
        /// </summary>
        /// <param name="item">The item to put in the slot.</param>
        /// <returns>True if the item was placed; otherwise, false.</returns>
        public virtual bool PutItem(GameObject item)
        {
            if (item == null) return false;
            if (!AcceptsItem()) return false;
            ItemId = item.GetComponent<Item>().info.id;
            return true;
        }

        /// <summary>
        /// Determines whether the slot can accept a new item.
        /// </summary>
        /// <returns>True if the slot can accept an item; otherwise, false.</returns>
        public virtual bool AcceptsItem()
        {
            if (ItemId != -1 || !IsEnabled || anchorPosition != new Vector2Int(-1, -1)) return false;
            return true;
        }

        /// <summary>
        /// Removes the item from the slot.
        /// </summary>
        /// <returns>The id of the removed item, or -1 if none was removed.</returns>
        public virtual int RemoveItem()
        {
            if (!IsEnabled) return -1;
            int removedItemId = ItemId;
            ItemId = -1;
            anchorPosition = new Vector2Int(-1, -1);
            return removedItemId;
        }

        /// <summary>
        /// Gets the color of the slot, depending on its enabled state.
        /// </summary>
        public Color Color => IsEnabled ? BaseColor : BaseColor * 0.5f;
    }

    /// <summary>
    /// Represents a locked inventory slot that cannot accept or remove items.
    /// </summary>
    [Serializable]
    public class LockedInventorySlot : InventorySlot
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="LockedInventorySlot"/> class with the specified item.
        /// </summary>
        /// <param name="item">The item to place in the slot.</param>
        public LockedInventorySlot(GameObject item) : base(item)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockedInventorySlot"/> class with the specified item id.
        /// </summary>
        /// <param name="id">The item id to place in the slot.</param>
        public LockedInventorySlot(int id) : base(id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LockedInventorySlot"/> class.
        /// </summary>
        public LockedInventorySlot() : base()
        {
        }
        #endregion

        /// <summary>
        /// Always returns false; locked slots cannot accept items.
        /// </summary>
        public override bool AcceptsItem() => false;

        /// <summary>
        /// Gets the base color for locked slots.
        /// </summary>
        protected override Color BaseColor => Color.red;

        /// <summary>
        /// Always returns false; locked slots cannot have items put in them.
        /// </summary>
        public override bool PutItem(GameObject item) => false;

        /// <summary>
        /// Always returns -1; locked slots cannot have items removed.
        /// </summary>
        public override int RemoveItem() => -1;
    }

    /// <summary>
    /// Represents an inventory slot that can hold multiple items of the same type (stackable).
    /// </summary>
    [Serializable]
    public class StackableInventorySlot : InventorySlot
    {
        /// <summary>
        /// The maximum stack size for this slot. -1 means unlimited.
        /// </summary>
        public int stackSize = -1;

        /// <summary>
        /// The current number of items in the stack.
        /// </summary>
        public int itemCount;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="StackableInventorySlot"/> class.
        /// </summary>
        public StackableInventorySlot() : base()
        {
            IsEnabled = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackableInventorySlot"/> class with the specified item.
        /// </summary>
        /// <param name="item">The item to place in the slot.</param>
        public StackableInventorySlot(GameObject item) : base(item)
        {
            itemCount = 1;
            IsEnabled = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StackableInventorySlot"/> class with the specified item id.
        /// </summary>
        /// <param name="id">The item id to place in the slot.</param>
        public StackableInventorySlot(int id) : base(id)
        {
            itemCount = 1;
            IsEnabled = true;
        }
        #endregion

        /// <summary>
        /// Gets the base color for stackable slots.
        /// </summary>
        protected override Color BaseColor => Color.green;

        /// <summary>
        /// Attempts to add an item to the stack.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>True if the item was added; otherwise, false.</returns>
        public override bool PutItem(GameObject item)
        {
            if (!IsEnabled) return false;
            if ((itemCount < stackSize || stackSize == -1) && (ItemId == -1 || ItemId == item.GetComponent<Item>().info.id))
            {
                ItemId = item.GetComponent<Item>().info.id;
                itemCount++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes an item from the stack.
        /// </summary>
        /// <returns>The id of the removed item, or -1 if none was removed.</returns>
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
