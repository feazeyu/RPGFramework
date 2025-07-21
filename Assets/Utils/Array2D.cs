using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#nullable enable
namespace Game.Utils
{
    [Serializable]
    public sealed class Array2D<TElement>
    {
        public int Rows => rows;
        public int Columns => columns;

        [SerializeField]
        private int rows;
        [SerializeField]
        private int columns;
        [SerializeReference] private TElement[] elements;

        public Array2D(int rows, int columns)
        {
            this.rows = rows;
            this.columns = columns;
            elements = new TElement[rows * columns];
        }

        public Array2D(TElement[,] array)
        {
            if (array is null)
                throw new ArgumentNullException(nameof(array));

            rows = array.GetLength(0);
            columns = array.GetLength(1);
            elements = new TElement[rows * columns];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    elements[y * columns + x] = array[y, x];
                }
            }
        }

        public TElement this[int row, int column]
        {
            get
            {
                return elements[row * columns + column];
            }

            set
            {
                elements[row * columns + column] = value;
            }
        }

        public bool TryGet(int row, int column, [NotNullWhen(true)] out TElement? value)
        {
            if ((uint)row < (uint)rows && (uint)column < (uint)columns)
            {
                value = elements[row * columns + column];
                return value is not null;
            }
            value = default;
            return false;
        }
    }
}
