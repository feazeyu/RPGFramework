using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Utilities
{
    /// <summary>
    /// A serializable dictionary implementation for Unity, allowing key-value pairs to be serialized and deserialized.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        /// <summary>
        /// The array of key-value pairs used for serialization.
        /// </summary>
        [SerializeField]
        private Pair[] pairs;

        /// <summary>
        /// The internal dictionary storing the key-value pairs.
        /// </summary>
        private Dictionary<TKey, TValue> dictionary;

        /// <inheritdoc/>
        public ICollection<TKey> Keys => dictionary.Keys;

        /// <inheritdoc/>
        public ICollection<TValue> Values => dictionary.Values;

        /// <inheritdoc/>
        public int Count => dictionary.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public TValue this[TKey key] { get => dictionary[key]; set => dictionary[key] = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableDictionary{TKey, TValue}"/> class.
        /// </summary>
        public SerializableDictionary()
        {
            dictionary = new();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableDictionary{TKey, TValue}"/> class with an existing dictionary.
        /// </summary>
        /// <param name="dict">The dictionary to initialize from.</param>
        public SerializableDictionary(Dictionary<TKey, TValue> dict)
        {
            dictionary = dict;
        }

        /// <summary>
        /// Converts the serializable dictionary to a standard <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        /// <returns>The internal dictionary.</returns>
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

        /// <inheritdoc/>
        public void Add(TKey key, TValue value) => dictionary.Add(key, value);

        /// <inheritdoc/>
        public void Add(KeyValuePair<TKey, TValue> item) => dictionary.Add(item.Key, item.Value);

        /// <inheritdoc/>
        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);

        /// <inheritdoc/>
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary)dictionary).Contains(item);

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);

        /// <inheritdoc/>
        public bool Remove(TKey key) => dictionary.Remove(key);

        /// <inheritdoc/>
        public void Clear() => dictionary.Clear();

        /// <inheritdoc/>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary)dictionary).CopyTo(array, arrayIndex);

        /// <inheritdoc/>
        public bool Remove(KeyValuePair<TKey, TValue> item) => dictionary.Remove(item.Key);

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Represents a serializable key-value pair.
        /// </summary>
        [Serializable]
        private struct Pair
        {
            [SerializeField]
            public TKey Key;
            [SerializeField]
            public TValue Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="Pair"/> struct.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            public Pair(TKey key, TValue value) => (Key, Value) = (key, value);

            /// <summary>
            /// Deconstructs the pair into key and value.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            public readonly void Deconstruct(out TKey key, out TValue value) => (key, value) = (Key, Value);
        }
    }
}
