#nullable enable
using Game.Items;
using Game.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Inventory
{
    [Serializable]
    public class InventoryManager : MonoBehaviour
    {
        public SerializableDictionary<int, GameObject> items = new SerializableDictionary<int, GameObject>(new Dictionary<int, GameObject>());


        public void ReloadItems() {
            GameObject[] itemPrefabs = Resources.LoadAll<GameObject>("Items");
            var workingDict = items.ToDictionary();
            workingDict.Clear();
            foreach (var item in itemPrefabs)
            {
                if (item.TryGetComponent<Item>(out Item itemComponent))
                {
                    if (!workingDict.ContainsKey(itemComponent.id))
                    {
                        workingDict.Add(itemComponent.id, item);
                    }
                    else
                    {
                        Debug.LogWarning($"Item with ID {itemComponent.id} already exists in the inventory.");
                    }
                }
                else
                {
                    Debug.LogWarning($"Item prefab {item.name} does not have an Item component.");
                }
            }
        }
        public GameObject? GetItemById(int id) { 
            return items.ToDictionary().TryGetValue(id, out var item) ? item : null;
        }
    }
}
