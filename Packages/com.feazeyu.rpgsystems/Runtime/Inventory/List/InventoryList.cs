using System;
using System.Collections.Generic;
using Feazeyu.RPGSystems.Items;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.EventSystems;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// Represents an inventory list that manages inventory slots and interacts with the UI generator.
    /// </summary>
    [Serializable]
    public class InventoryList : MonoBehaviour, IUIPositionalItemContainer, IDropHandler, IItemCountNotifier
    {
        /// <summary>Raised after an item is added to the list: (itemId, count).</summary>
        public event Action<int, int> OnItemAdded;

        /// <summary>Raised after an item is removed from the list: (itemId, count).</summary>
        public event Action<int, int> OnItemRemoved;

        private void RaiseItemAdded(GameObject item)
        {
            var comp = item != null ? item.GetComponent<Item>() : null;
            if (comp != null) OnItemAdded?.Invoke(comp.info.id, 1);
        }

        /// <summary>
        /// Indicates whether slot capacity is enabled.
        /// </summary>
        public bool EnableSlotCapacity = false;

        /// <summary>
        /// The maximum number of slots in the inventory.
        /// </summary>
        public int capacity = 20;

        /// <summary>
        /// The scroll sensitivity for the inventory UI.
        /// </summary>
        public int scrollSensitivity = 10;

        /// <summary>
        /// The list of stackable inventory slots.
        /// </summary>
        public List<StackableInventorySlot> contents;

        /// <summary>
        /// Suppresses automatic addition of the UI generator component.
        /// </summary>
        private bool suppressAutoAddUI = false;

        /// <summary>
        /// The UI generator responsible for creating and managing the inventory UI.
        /// </summary>
        [SerializeField]
        private InventoryListGenerator _uiGenerator;

        /// <summary>
        /// Gets the UI generator for this inventory list, adding it if necessary.
        /// </summary>
        public InventoryListGenerator uiGenerator
        {
            get
            {
                if (_uiGenerator == null)
                {
                    _uiGenerator = GetComponent<InventoryListGenerator>();
                }
                return _uiGenerator;
            }
        }

        /// <summary>
        /// Unity callback invoked when the script is loaded or a value changes in the Inspector.
        /// Ensures the UI generator is added only once in the editor.
        /// </summary>
        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
            if (!suppressAutoAddUI && GetComponent<InventoryListGenerator>() == null)
            {
                EditorApplication.delayCall += () => {
                    if (gameObject != null && gameObject.GetComponent<InventoryListGenerator>() == null && !Application.isPlaying)
                    {
                        suppressAutoAddUI = true;
                        var gen = Undo.AddComponent<InventoryListGenerator>(gameObject);
                        gen.list = this;
                    }
                };
            }
#endif
        }


        /// <summary>Put item.</summary>
        public virtual bool PutItem(Vector2Int position, GameObject item)
        {
            contents ??= new();
            bool added = false;

            if (position.y >= 0 && position.y < contents.Count && contents[position.y].PutItem(item))
            {
                added = true;
            }

            if (!added)
            {
                foreach (var slot in contents)
                {
                    if (slot.PutItem(item)) { added = true; break; }
                }
            }

            if (!added)
            {
                var newSlot = new StackableInventorySlot(item);
                if (EnableSlotCapacity)
                    newSlot.stackSize = capacity;
                contents.Add(newSlot);
                added = true;
            }

            if (added)
            {
                RedrawContents();
                RaiseItemAdded(item);
            }
            return added;
        }

        /// <summary>Remove item.</summary>
        public virtual int RemoveItem(Vector2Int position)
        {
            var itemSlot = contents[position.y];
            if (itemSlot != null)
            {
                int itemId = itemSlot.RemoveItem();
                if (itemSlot.itemCount <= 0)
                    contents.Remove(itemSlot);
                RedrawContents();
                if (itemId >= 0) OnItemRemoved?.Invoke(itemId, 1);
                return itemId;
            }
            return -1;
        }

        /// <summary>Get item.</summary>
        public GameObject GetItem(Vector2Int position)
        {
            if (contents == null || position.y < 0 || position.y >= contents.Count)
            {
                Debug.LogError($"InventoryList: Invalid position {position}. Contents count: {contents?.Count ?? 0}");
                return null;
            }
            return contents[position.y].Item;
        }

        /// <summary>Try add item.</summary>
        public bool TryAddItem(int itemId, int count = 1)
        {
            var prefab = InventoryManager.Instance?.GetItemById(itemId);
            if (prefab == null) return false;
            for (int i = 0; i < count; i++)
                PutItem(new Vector2Int(-1, -1), prefab);
            return true;
        }

        int IItemContainer.CountItem(int itemId)
        {
            if (contents == null) return 0;
            int total = 0;
            foreach (var slot in contents)
                if (slot.ItemId == itemId)
                    total += Mathf.Max(slot.itemCount, 0);
            return total;
        }

        bool IItemContainer.RemoveItem(int itemId, int count)
        {
            if (((IItemContainer)this).CountItem(itemId) < count) return false;

            int remaining = count;
            var toRemove = new List<StackableInventorySlot>();

            foreach (var slot in contents)
            {
                if (slot.ItemId != itemId || slot.itemCount <= 0) continue;
                while (remaining > 0 && slot.itemCount > 0)
                {
                    slot.RemoveItem();
                    remaining--;
                }
                if (slot.itemCount <= 0)
                    toRemove.Add(slot);
                if (remaining == 0) break;
            }

            foreach (var s in toRemove)
                contents.Remove(s);

            RedrawContents();
            OnItemRemoved?.Invoke(itemId, count);
            return true;
        }

        /// <summary>Redraw contents.</summary>
        public void RedrawContents()
        {
            uiGenerator?.GenerateUI();
        }

        /// <summary>
        /// The text shown for a slot's item in the list UI. Defaults to the item's name; subclasses
        /// (e.g. <see cref="ShopListUI"/>) override this to append extra info such as a price.
        /// </summary>
        public virtual string GetItemLabel(StackableInventorySlot slot)
        {
            var item = slot.Item != null ? slot.Item.GetComponent<Item>() : null;
            return item != null ? item.info.Name : string.Empty;
        }

        /// <inheritdoc/>
        public void OnDrop(PointerEventData eventData)
        {
            var handler = eventData.pointerDrag?.GetComponent<InventoryItemUIHandler>();
            if (handler == null || handler.DropHandled) return;
            var item = InventoryManager.Instance.GetItemById(handler.DraggedId);
            if (item != null && ((IItemContainer)this).PutItem(item))
                handler.DropHandled = true;
        }


        /// <summary>
        /// Toggles the active state of the inventory UI.
        /// </summary>
        public virtual void ToggleInventory()
        {
            if (uiGenerator)
                uiGenerator.ToggleInventoryActiveState();
        }

        /// <summary>
        /// Opens the inventory UI.
        /// </summary>
        public virtual void OpenInventory()
        {
            if (uiGenerator)
                uiGenerator.SetInventoryActiveState(true);
        }

        /// <summary>
        /// Closes the inventory UI.
        /// </summary>
        public virtual void CloseInventory()
        {
            if (uiGenerator)
                uiGenerator.SetInventoryActiveState(false);
        }
    }
}
