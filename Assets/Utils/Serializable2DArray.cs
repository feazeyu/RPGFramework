using System;
using System.Collections.Generic;
using UnityEngine;

namespace RPGFramework.Utils
{
    [Serializable]
    public class Serializable2DArray<TElement>
    {
        [SerializeField]
        int rows;
        [SerializeField]
        int cols;
        [SerializeReference] List<TElement> elements;

        public Serializable2DArray(TElement[,] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            rows = array.GetLength(0);
            cols = array.GetLength(1);
            elements = new List<TElement>(rows * cols);

            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    elements.Add(array[y, x]);
        }

        public TElement Get(int row, int col)
        {
            return elements[row * cols + col];
        }

        public void Set(int row, int col, TElement value)
        {
            elements[row * cols + col] = value;
        }

        public TElement[,] ToArray()
        {
            var array = new TElement[rows, cols];
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                    array[y, x] = elements[y * cols + x];
            return array;
        }

        public int GetLength(int index)
        {
            if (index == 0)
            {
                return rows;
            }
            if (index == 1)
            {
                return cols;
            }
            return -1;
        }
    }
}