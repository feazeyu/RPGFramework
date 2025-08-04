#nullable enable
using Game.Core.Utilities;
using Game.Items;
using System;
using System.Collections.Generic;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Game.Inventory
{
    [Serializable, ExecuteInEditMode]
    public class InventoryManager : MonoBehaviour
    {
        [SerializeReference]
        private static InventoryManager? _instance;
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
        void Awake()
        {
            _instance = _instance != null ? _instance : this;
        }
        private static InventoryManager FindInstance()
        {
            return FindFirstObjectByType<InventoryManager>() ?? new GameObject("InventoryManager").AddComponent<InventoryManager>();
        }


        public SerializableDictionary<int, GameObject> items = new(new Dictionary<int, GameObject>());
        [Header("Grid slot size")]
        public Vector2 cellSize = new(100, 100);
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
        public GameObject? GetItemById(int id)
        {
            return items.ToDictionary().TryGetValue(id, out var item) ? item : null;
        }
    }
}
