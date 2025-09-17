using Game.Core.Interfaces;
using Game.Core.Stats;
using Game.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Items
{
    /// <summary>
    /// Represents a serializable list of stat effects of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of stat effect, must inherit from <see cref="StatEffect"/>.</typeparam>
    [Serializable]
    public sealed class EffectList<T> : IList<T> where T : StatEffect
    {
        [SerializeField]
        private List<T> _list;

        /// <summary>
        /// Gets the number of elements contained in the list.
        /// </summary>
        public int Count => _list.Count;

        /// <summary>
        /// Gets a value indicating whether the list is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public T this[int index] { get => _list[index]; set => _list[index] = value; }

        /// <summary>
        /// Adds an item to the list.
        /// </summary>
        /// <param name="item">The object to add.</param>
        public void Add(T item) => _list.Add(item);

        /// <summary>
        /// Inserts an item to the list at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which item should be inserted.</param>
        /// <param name="item">The object to insert.</param>
        public void Insert(int index, T item) => _list.Insert(index, item);

        /// <summary>
        /// Copies the elements of the list to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from list.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);

        /// <summary>
        /// Determines the index of a specific item in the list.
        /// </summary>
        /// <param name="item">The object to locate.</param>
        /// <returns>The index of item if found; otherwise, -1.</returns>
        public int IndexOf(T item) => _list.IndexOf(item);

        /// <summary>
        /// Determines whether the list contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate.</param>
        /// <returns>true if item is found; otherwise, false.</returns>
        public bool Contains(T item) => _list.Contains(item);

        /// <summary>
        /// Removes the first occurrence of a specific object from the list.
        /// </summary>
        /// <param name="item">The object to remove.</param>
        /// <returns>true if item was successfully removed; otherwise, false.</returns>
        public bool Remove(T item) => _list.Remove(item);

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index) => _list.RemoveAt(index);

        /// <summary>
        /// Removes all items from the list.
        /// </summary>
        public void Clear() => _list.Clear();

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        /// <returns>An enumerator for the list.</returns>
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An enumerator object.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Custom property drawer for <see cref="EffectList{StatEffectF}"/> in the Unity Editor.
    /// </summary>
    [CustomPropertyDrawer(typeof(EffectList<StatEffectF>))]
    public sealed class EffectFListDrawer : PropertyDrawer
    {
        private const float Margin = 4f;

        /// <summary>
        /// Draws the custom property GUI for the effect list.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for the property GUI.</param>
        /// <param name="property">The SerializedProperty to make the custom GUI for.</param>
        /// <param name="label">The label of this property.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var list = (EffectList<StatEffectF>)property.boxedValue;
            EditorGUI.BeginProperty(position, label, property);

            EditorGUI.DrawRect(position.CropWidth(2f), Color.white);
            GUI.BeginGroup(position);
            position.width -= position.x;

            // Effect rows
            for (int i = 0; i < list.Count; i++)
            {
                StatEffectF effect = list[i];
                Rect row = position.CropHeight(EditorHelper.LineHeight);

                // Remove button
                Rect buttonRect = row.PushRight(row.width - EditorHelper.LineHeight * 1.5f);
                if (GUI.Button(buttonRect, EditorHelper.TrashIcon))
                {
                    list.RemoveAt(i--);
                    continue;
                }
                row.width -= buttonRect.width + 10f;

                const float Space = 15f;
                const float SignWidth = 12f;

                EditorGUI.DrawRect(position.Push(-Margin, Margin / 2f).Crop(1f, EditorHelper.LineHeight * 2f), Color.white);

                // Stat selection
                Rect statRect = row.CropWidth(position.width * 0.3f);
                effect.Stat = (Stat)EditorGUI.EnumPopup(statRect, effect.Stat);
                row = row.PushRight(statRect.width);

                float inputWidth = (row.width - (2f * Space) - (3f * SignWidth)) / 2f;

                // Flat
                row = row.PushRight(Space);
                EditorGUI.LabelField(row, "+");
                row = row.PushRight(SignWidth);
                effect.Flat = EditorGUI.FloatField(row.CropWidth(inputWidth), effect.Flat);
                row = row.PushRight(inputWidth + Space);

                // Percentage
                EditorGUI.LabelField(row, "+");
                row = row.PushRight(SignWidth);
                effect.Multiply = EditorGUI.FloatField(row.CropWidth(inputWidth), (effect.Multiply + 1f) * 100f) / 100f - 1f;
                row = row.PushRight(inputWidth - SignWidth);
                EditorGUI.LabelField(row, "%");

                position = position.PushDown(EditorHelper.LineHeight + Margin);
                row = position.CropHeight(EditorHelper.LineHeight);

                Vector2 labelSize = EditorHelper.Label(row, "Scaling");
                row = row.PushRight(labelSize.x + Margin);

                // Scaling
                effect.Scaling = (Scaling)EditorGUI.EnumPopup(row.CropWidth(row.width * 0.3f), effect.Scaling);
                row = row.PushRight(row.width * 0.3f + Margin);

                DrawScalingInputs(row, effect);

                position = position.PushDown(EditorHelper.LineHeight + Margin);
            }

            // Add button
            if (GUI.Button(position.SliceCenter(position.width * 0.6f), "Add Effect"))
            {
                list.Add(new StatEffectF((INamed)property.serializedObject.targetObject, Stat.MaxHitPoints));
            }

            property.boxedValue = list;

            GUI.EndGroup();
            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Draws the scaling input fields for a given effect.
        /// </summary>
        /// <param name="row">The rectangle area for the scaling inputs.</param>
        /// <param name="effect">The effect to modify.</param>
        private void DrawScalingInputs(Rect row, StatEffectF effect)
        {
            switch (effect.Scaling)
            {
                case Scaling.None:
                    effect.FlatScale = effect.MultiplyScale = 0f;
                    return;

                case Scaling.Multiplicative:
                    {
                        effect.FlatScale = 0f;

                        Vector2 signSize = EditorHelper.Label(row, "×");
                        effect.MultiplyScale = EditorGUI.FloatField(row.PushRight(signSize.x), effect.MultiplyScale);
                    }
                    break;

                case Scaling.Linear:
                case Scaling.DiminishingReturns:
                    {
                        Vector2 signSize = EditorHelper.Label(row, "+");
                        float inputSize = (row.width - (2f * signSize.x) - Margin) / 2f;
                        row = row.PushRight(signSize.x);

                        effect.FlatScale = EditorGUI.FloatField(row.CropWidth(inputSize), effect.FlatScale);
                        row = row.PushRight(inputSize + Margin);

                        EditorGUI.LabelField(row, "+");
                        row = row.PushRight(signSize.x);

                        effect.MultiplyScale = EditorGUI.FloatField(row, effect.MultiplyScale * 100f) / 100f;
                        row = row.SliceRight(signSize.x + 5f);

                        EditorGUI.LabelField(row, "%");
                    }
                    break;
            }
        }

        /// <summary>
        /// Gets the height of the property drawer for the effect list.
        /// </summary>
        /// <param name="property">The SerializedProperty to calculate height for.</param>
        /// <param name="label">The label of this property.</param>
        /// <returns>The height in pixels.</returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var list = (EffectList<StatEffectF>)property.boxedValue;
            return (list.Count * 2 + 1) * (EditorHelper.LineHeight + Margin);
        }
    }
}
