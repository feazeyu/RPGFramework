using System.Collections.Generic;
using UnityEngine;
using Feazeyu.RPGSystems.Inventory;

namespace Feazeyu.RPGSystems.Demo
{
    /// <summary>
    /// Seeds each chest's InventoryList with items from the InventoryManager at startup.
    /// Distributes registered item IDs round-robin across the chests.
    /// </summary>
    public class InventoryDemoBootstrap : MonoBehaviour
    {
        [SerializeField] private InventoryList[] m_Chests;
        [SerializeField] private int m_ItemsPerChest = 3;

        private void Start()
        {
            var mgr = InventoryManager.Instance;
            if (mgr == null)
            {
                Debug.LogWarning("[InventoryDemo] InventoryManager not found — chests will be empty.");
                return;
            }

            var ids = new List<int>(mgr.items.ToDictionary().Keys);
            if (ids.Count == 0)
            {
                Debug.LogWarning("[InventoryDemo] No items in Resources/Items/ — chests will be empty.");
                return;
            }

            for (int c = 0; c < m_Chests.Length; c++)
            {
                if (m_Chests[c] == null) continue;
                int id = ids[c % ids.Count];
                m_Chests[c].TryAddItem(id, m_ItemsPerChest);
            }
        }
    }
}
