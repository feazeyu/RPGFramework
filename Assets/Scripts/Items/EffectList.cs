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
    [Serializable]
    public sealed class EffectList<T> : IList<T> where T : StatEffect
    {
        [SerializeField]
        private List<T> _list;

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public T this[int index] { get => _list[index]; set => _list[index] = value; }

        public void Add(T item) => _list.Add(item);
        public void Insert(int index, T item) => _list.Insert(index, item);
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public int IndexOf(T item) => _list.IndexOf(item);
        public bool Contains(T item) => _list.Contains(item);
        public bool Remove(T item) => _list.Remove(item);
        public void RemoveAt(int index) => _list.RemoveAt(index);
        public void Clear() => _list.Clear();
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [CustomPropertyDrawer(typeof(EffectList<StatEffectF>))]
    public sealed class EffectFListDrawer : PropertyDrawer
    {
        private const float Margin = 4f;

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

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var list = (EffectList<StatEffectF>)property.boxedValue;
            return (list.Count * 2 + 1) * (EditorHelper.LineHeight + Margin);
        }
    }
}
