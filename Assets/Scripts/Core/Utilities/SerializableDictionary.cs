using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Utilities
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private Pair[] pairs;

        private Dictionary<TKey, TValue> dictionary;

        public ICollection<TKey> Keys => dictionary.Keys;
        public ICollection<TValue> Values => dictionary.Values;
        public int Count => dictionary.Count;
        public bool IsReadOnly => false;
        public TValue this[TKey key] { get => dictionary[key]; set => dictionary[key] = value; }

        public SerializableDictionary()
        {
            dictionary = new();
        }

        public SerializableDictionary(Dictionary<TKey, TValue> dict)
        {
            dictionary = dict;
        }

        public Dictionary<TKey, TValue> ToDictionary()
        {
            return dictionary;
        }

        public void OnBeforeSerialize()
        {
            dictionary ??= new();

            pairs = new Pair[dictionary.Count];

            int index = 0;
            foreach (var (key, value) in dictionary)
            {
                if (key is null)
                {
                    continue;
                }

                pairs[index] = new(key, value);
                index++;
            }
        }

        public void OnAfterDeserialize()
        {
            dictionary ??= new();

            foreach (var (key, value) in pairs)
            {
                if (key is null)
                {
                    continue;
                }

                dictionary[key] = value;
            }
        }

        public void Add(TKey key, TValue value) => dictionary.Add(key, value);
        public void Add(KeyValuePair<TKey, TValue> item) => dictionary.Add(item.Key, item.Value);
        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary)dictionary).Contains(item);
        public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);
        public bool Remove(TKey key) => dictionary.Remove(key);
        public void Clear() => dictionary.Clear();
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary)dictionary).CopyTo(array, arrayIndex);
        public bool Remove(KeyValuePair<TKey, TValue> item) => dictionary.Remove(item.Key);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [Serializable]
        private struct Pair
        {
            [SerializeField]
            public TKey Key;
            [SerializeField]
            public TValue Value;

            public Pair(TKey key, TValue value) => (Key, Value) = (key, value);
            public readonly void Deconstruct(out TKey key, out TValue value) => (key, value) = (Key, Value);
        }
    }
}
