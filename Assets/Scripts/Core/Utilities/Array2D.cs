using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#nullable enable
namespace Game.Core.Utilities
{
    /// <summary>
    /// Represents a two-dimensional array with serialization support for Unity.
    /// </summary>
    /// <typeparam name="TElement">The type of elements in the array.</typeparam>
    [Serializable]
    public sealed class Array2D<TElement> : IEnumerable<TElement>
    {
        /// <summary>
        /// Gets the number of rows in the array.
        /// </summary>
        public int Rows => rows;

        /// <summary>
        /// Gets the number of columns in the array.
        /// </summary>
        public int Columns => columns;

        [SerializeField]
        private int rows;
        [SerializeField]
        private int columns;
        [SerializeReference] private TElement[] elements;

        /// <summary>
        /// Initializes a new instance of the <see cref="Array2D{TElement}"/> class with the specified number of rows and columns.
        /// </summary>
        /// <param name="rows">The number of rows.</param>
        /// <param name="columns">The number of columns.</param>
        public Array2D(int rows, int columns)
        {
            this.rows = rows;
            this.columns = columns;
            elements = new TElement[rows * columns];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Array2D{TElement}"/> class from a two-dimensional array.
        /// </summary>
        /// <param name="array">The source two-dimensional array.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="array"/> is null.</exception>
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

        /// <summary>
        /// Gets or sets the element at the specified row and column.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="column">The column index.</param>
        /// <returns>The element at the specified position.</returns>
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

        /// <summary>
        /// Attempts to get the element at the specified row and column.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="column">The column index.</param>
        /// <param name="value">When this method returns, contains the element if found; otherwise, the default value for the type.</param>
        /// <returns><c>true</c> if the element exists and is not null; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Returns an enumerator that iterates through the elements of the array.
        /// </summary>
        /// <returns>An enumerator for the array.</returns>
        public IEnumerator<TElement> GetEnumerator()
        {
            for (int i = 0; i < elements.Length; i++)
            {
                yield return elements[i];
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the elements of the array.
        /// </summary>
        /// <returns>An enumerator for the array.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
