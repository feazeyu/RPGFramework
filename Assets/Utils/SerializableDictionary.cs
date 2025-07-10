using System.Collections.Generic;
using System;
using UnityEngine;
namespace RPGFramework.Utils
{
    //For large dictionaries, this is going to be very slow. Don't use it.
    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        [SerializeField]
        private List<DictItem> items = new();
        [Serializable]
        private struct DictItem
        {
            public TKey Key;
            public TValue Value;
        }
        public SerializableDictionary(Dictionary<TKey, TValue> dict)
        {
            FromDictionary(dict);
        }
        public void FromDictionary(Dictionary<TKey, TValue> dict)
        {
            foreach (var kvp in dict)
            {
                items.Add(new DictItem { Key = kvp.Key, Value = kvp.Value });
            }
        }
        public Dictionary<TKey, TValue> ToDictionary()
        {
            var result = new Dictionary<TKey, TValue>();
            foreach (var item in items)
            {
                if (item.Key != null && item.Value != null)
                {
                    result[item.Key] = item.Value;
                }
            }
            if (result.Count > 20)
            {
                Debug.LogWarning($"The SerializableDictionary implementation you are using is not made with performance in mind, and may severely impact performance. Consider making your own.");
            }
            return result;
        }
    }

}