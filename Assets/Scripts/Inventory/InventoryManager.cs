#nullable enable
using Game.Core.Utilities;
using Game.Items;
using System;
using System.Collections.Generic;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// Manages the item ID's
    /// Implements a singleton pattern for global access.
    /// </summary>
    [Serializable, ExecuteInEditMode]
    public class InventoryManager : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance of the <see cref="InventoryManager"/>.
        /// </summary>
        [SerializeReference]
        private static InventoryManager? _instance;

        /// <summary>
        /// Gets the singleton instance of the <see cref="InventoryManager"/>.
        /// </summary>
        public static InventoryManager Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }
                _instance = FindInstance();
                return _instance;
            }
        }

        /// <summary>
        /// Unity Awake method. Ensures the singleton instance is set.
        /// </summary>
        void Awake()
        {
            _instance = _instance != null ? _instance : this;
        }

        /// <summary>
        /// Finds an existing <see cref="InventoryManager"/> in the scene or creates a new one if none exists.
        /// </summary>
        /// <returns>The found or newly created <see cref="InventoryManager"/> instance.</returns>
        private static InventoryManager FindInstance()
        {
            return FindFirstObjectByType<InventoryManager>() ?? new GameObject("InventoryManager").AddComponent<InventoryManager>();
        }

        /// <summary>
        /// The dictionary of items in the inventory, keyed by item ID.
        /// </summary>
        public SerializableDictionary<int, GameObject> items = new(new Dictionary<int, GameObject>());

        /// <summary>
        /// The size of each grid slot in the inventory UI.
        /// </summary>
        [Header("Grid slot size")]
        public Vector2 cellSize = new(100, 100);

        /// <summary>
        /// Reloads all items from the specified resources path and updates the inventory.
        /// </summary>
        /// <param name="path">The path in the Resources folder to load item prefabs from.</param>
        public void ReloadItems(string path)
        {
            GameObject[] itemPrefabs = Resources.LoadAll<GameObject>(path);
            var workingDict = items.ToDictionary();
            workingDict.Clear();
            foreach (var item in itemPrefabs)
            {
                if (item.TryGetComponent<Item>(out Item itemComponent))
                {
                    if (!workingDict.ContainsKey(itemComponent.info.id))
                    {
                        workingDict.Add(itemComponent.info.id, item);
                    }
                    else
                    {
                        Debug.LogWarning($"Item with ID {itemComponent.info.id} already exists in the inventory.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Item prefab {item.name} does not have an Item component.");
                }
            }
        }

        /// <summary>
        /// Retrieves an item GameObject by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the item.</param>
        /// <returns>The <see cref="GameObject"/> representing the item, or null if not found.</returns>
        public GameObject? GetItemById(int id)
        {
            return items.ToDictionary().TryGetValue(id, out var item) ? item : null;
        }
    }
}
