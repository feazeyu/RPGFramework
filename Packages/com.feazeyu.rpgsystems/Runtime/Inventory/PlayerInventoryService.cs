using System.Collections.Generic;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// Singleton service that exposes a simple API for quest code to read
    /// and mutate the player's inventory without depending on grid/UI internals.
    ///
    /// Add to any persistent GameObject in the scene (e.g. GameManager).
    /// Assign InventoryList for HasItem/CountItem/TakeItem queries; auto-found if left empty.
    /// </summary>
    public class PlayerInventoryService : MonoBehaviour
    {
        public static PlayerInventoryService Instance { get; private set; }

        [Tooltip("The InventoryList data for HasItem / CountItem / TakeItem. Auto-found if empty.")]
        [SerializeField] private InventoryList m_InventoryList;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            if (m_InventoryList == null)
                m_InventoryList = FindFirstObjectByType<InventoryList>();
        }

        // ── Read (list-based) ────────────────────────────────────────────────

        /// <summary>Total stack count for <paramref name="itemId"/> across all list slots.</summary>
        public int CountItem(int itemId)
        {
            var contents = m_InventoryList?.contents;
            if (contents == null) return 0;
            int total = 0;
            foreach (var slot in contents)
                if (slot.ItemId == itemId)
                    total += Mathf.Max(slot.itemCount, 0);
            return total;
        }

        /// <summary>True if the player has at least <paramref name="count"/> of the item.</summary>
        public bool HasItem(int itemId, int count = 1) => CountItem(itemId) >= count;

        // ── Write ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Tries to add <paramref name="count"/> copies of <paramref name="itemId"/> to
        /// the first inventory in the scene that accepts it. Tries InventoryGrid instances
        /// first, then InventoryListUI instances.
        /// </summary>
        public bool TryAddItem(int itemId, int count = 1)
        {
            foreach (var grid in FindObjectsByType<InventoryGrid>(FindObjectsSortMode.None))
                if (grid.TryAddItem(itemId, count)) return true;
            foreach (var list in FindObjectsByType<InventoryListUI>(FindObjectsSortMode.None))
                if (list.TryAddItem(itemId, count)) return true;
            Debug.LogWarning($"[PlayerInventoryService] No inventory accepted item {itemId}.");
            return false;
        }

        /// <summary>
        /// Remove <paramref name="count"/> items with <paramref name="itemId"/> from
        /// the list inventory. Returns false (no-op) if insufficient.
        /// </summary>
        public bool TakeItem(int itemId, int count = 1)
        {
            if (!HasItem(itemId, count)) return false;
            var contents = m_InventoryList?.contents;
            if (contents == null) return false;

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

            // UI redraw: find the InventoryListUI that owns this list and refresh it.
            foreach (var ui in FindObjectsByType<InventoryListUI>(FindObjectsSortMode.None))
                if (ui.list == m_InventoryList) { ui.RedrawContents(); break; }

            return remaining == 0;
        }
    }
}
