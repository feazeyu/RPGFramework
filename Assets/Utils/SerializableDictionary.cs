using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Utils
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField]
        private TKey[] keys;
        [SerializeField]
        private TValue[] values;

        private Dictionary<TKey, TValue> dictionary;

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

            keys = new TKey[dictionary.Count];
            values = new TValue[dictionary.Count];

            int index = 0;
            foreach (var (key, value) in dictionary)
            {
                keys[index] = key;
                values[index] = value;
                index++;
            }
        }

        public void OnAfterDeserialize()
        {
            dictionary ??= new();

            for (int i = 0; i < keys.Length; i++)
            {
                dictionary[keys[i]] = values[i];
            }
        }
    }
}
