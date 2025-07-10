using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Utils
{

    //UNTESTED WILL PROBABLY CRASH YOUR EDITOR (Mine crashed when testing and after that I decided to go for 2D arrays only)
    [Serializable]
    public class SerializableMultiArray<TElement>
    {
        [SerializeField] 
        int[] dimensions;
        [SerializeReference] 
        List<TElement> elements;

        public SerializableMultiArray(Array array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            Type elementType = array.GetType().GetElementType();
            if (!typeof(TElement).IsAssignableFrom(elementType))
                throw new ArgumentException($"Array must be of {typeof(TElement)}.", nameof(array));

            int rank = array.Rank;
            dimensions = new int[rank];
            for (int i = 0; i < rank; i++)
                dimensions[i] = array.GetLength(i);

            int totalSize = array.Length;
            elements = new List<TElement>(totalSize);

            // Flatten recursively
            int[] indices = new int[rank];
            Flatten(array, 0, indices);
        }

        private void Flatten(Array array, int dimension, int[] indices)
        {
            int length = array.GetLength(dimension);
            for (int i = 0; i < length; i++)
            {
                indices[dimension] = i;
                if (dimension == array.Rank - 1)
                {
                    TElement value = (TElement)array.GetValue(indices);
                    elements.Add(value);
                }
                else
                {
                    Flatten(array, dimension + 1, indices);
                }
            }
        }

        public TElement Get(params int[] indices)
        {
            if (indices.Length != dimensions.Length)
                throw new ArgumentException("Incorrect number of indices.");

            int flatIndex = GetFlatIndex(indices);
            return elements[flatIndex];
        }

        public void Set(TElement value, params int[] indices)
        {
            if (indices.Length != dimensions.Length)
                throw new ArgumentException("Incorrect number of indices.");

            int flatIndex = GetFlatIndex(indices);
            elements[flatIndex] = value;
        }

        private int GetFlatIndex(int[] indices)
        {
            int flatIndex = 0;
            int stride = 1;
            for (int i = dimensions.Length - 1; i >= 0; i--)
            {
                flatIndex += indices[i] * stride;
                stride *= dimensions[i];
            }
            return flatIndex;
        }

        public Array ToArray()
        {
            Array array = Array.CreateInstance(typeof(TElement), dimensions);
            int[] indices = new int[dimensions.Length];
            RecurseSet(array, 0, indices, 0);
            return array;
        }

        private int RecurseSet(Array array, int dimension, int[] indices, int flatIndex)
        {
            int length = dimensions[dimension];
            for (int i = 0; i < length; i++)
            {
                indices[dimension] = i;
                if (dimension == dimensions.Length - 1)
                {
                    array.SetValue(elements[flatIndex], indices);
                    flatIndex++;
                }
                else
                {
                    flatIndex = RecurseSet(array, dimension + 1, indices, flatIndex);
                }
            }
            return flatIndex;
        }
    }
}
