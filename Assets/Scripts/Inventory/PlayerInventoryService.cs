using System.Collections.Generic;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// Singleton service that exposes a simple API for quest code to read
    /// and mutate the player's <see cref="InventoryList"/> without depending
    /// on the grid/UI internals.
    ///
    /// Add to any persistent GameObject in the scene (e.g. GameManager).
    /// Assign the InventoryListUI reference in the Inspector or leave it empty
    /// to let the service find it automatically on Start.
    /// </summary>
    public class PlayerInventoryService : MonoBehaviour
    {
        public static PlayerInventoryService Instance { get; private set; }

        [Tooltip("The InventoryListUI that represents the player's inventory. " +
                 "Auto-found in scene if left empty.")]
        [SerializeField] private InventoryListUI m_InventoryUI;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            if (m_InventoryUI == null)
                m_InventoryUI = FindFirstObjectByType<InventoryListUI>();
        }

        private InventoryListUI UI
        {
            get
            {
                if (m_InventoryUI == null)
                    m_InventoryUI = FindFirstObjectByType<InventoryListUI>();
                return m_InventoryUI;
            }
        }

        // ── Read ──────────────────────────────────────────────────────────────

        /// <summary>Total stack count for <paramref name="itemId"/> across all slots.</summary>
        public int CountItem(int itemId)
        {
            var contents = UI?.list?.contents;
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
        /// Remove <paramref name="count"/> items with <paramref name="itemId"/> from
        /// the inventory. Returns false (no-op) if the player doesn't have enough.
        /// </summary>
        public bool TakeItem(int itemId, int count = 1)
        {
            if (!HasItem(itemId, count)) return false;
            var ui = UI;
            if (ui?.list?.contents == null) return false;

            int remaining = count;
            var toRemove  = new List<StackableInventorySlot>();

            foreach (var slot in ui.list.contents)
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
                ui.list.contents.Remove(s);

            ui.RedrawContents();
            return remaining == 0;
        }

        /// <summary>
        /// Add <paramref name="count"/> items with <paramref name="itemId"/> to
        /// the player's inventory. The prefab is looked up via InventoryManager.
        /// Returns false if the item isn't registered or the inventory is full.
        /// </summary>
        public bool GiveItem(int itemId, int count = 1)
        {
            var ui = UI;
            if (ui == null)
            {
                Debug.LogWarning("[PlayerInventoryService] No InventoryListUI found in scene.");
                return false;
            }
            var prefab = InventoryManager.Instance?.GetItemById(itemId);
            if (prefab == null)
            {
                Debug.LogWarning($"[PlayerInventoryService] Item id {itemId} is not registered " +
                                 "in InventoryManager — did you call ReloadItems()?");
                return false;
            }
            for (int i = 0; i < count; i++)
                ui.PutItem(new Vector2Int(-1, -1), prefab);
            return true;
        }
    }
}
