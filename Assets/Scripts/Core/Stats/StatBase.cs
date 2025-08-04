using Game.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.Core.Stats
{
    public abstract class StatBase<T, TEffect> : ISerializationCallbackReceiver where TEffect : StatEffect
    {
        public T Value => _value;
        protected T _value;

        public T Base
        {
            get => _base;
            set
            {
                _base = value;
                UpdateValue();
            }
        }
        [SerializeField]
        protected T _base = default;

        public IEnumerable<TEffect> Effects => _effects;
        [SerializeField]
        private List<TEffect> _effects = new();

        protected StatBase() : this(default)
        {
        }

        public StatBase(T baseValue)
        {
            _base = baseValue;
            Initialize();
            UpdateValue();
        }

        public virtual void UpdateValue()
        {
            _value = _base;
        }

        public void Apply(TEffect effect)
        {
            ApplyEffect(effect);
            UpdateValue();
            _effects.Add(effect);
        }

        protected abstract void ApplyEffect(TEffect effect);

        public void Remove(TEffect effect)
        {
            if (_effects.Remove(effect))
            {
                RemoveEffect(effect);
                UpdateValue();
            }
        }

        protected abstract void RemoveEffect(TEffect effect);

        public override string ToString()
        {
            return _value.ToString();
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Initialize();
            UpdateValue();
        }

        protected virtual void Initialize()
        {
        }

        public static implicit operator T(StatBase<T, TEffect> stat)
        {
            return stat.Value;
        }
    }

    [Serializable]
    public sealed class StatF : StatBase<float, StatEffectF>
    {
        private float _flat;
        private float _multiply;

        public StatF(float baseValue) : base(baseValue)
        {
        }

        protected override void Initialize()
        {
            _flat = 0f;
            _multiply = 1f;
        }

        public override void UpdateValue()
        {
            _value = (_base + _flat) * _multiply;
        }

        protected override void ApplyEffect(StatEffectF effect)
        {
            _flat += effect.Flat;
            _multiply += effect.Multiply;
        }

        protected override void RemoveEffect(StatEffectF effect)
        {
            _flat -= effect.Flat;
            _multiply -= effect.Multiply;
        }

        public override string ToString()
        {
            return _value.ToString("N2");
        }
    }

    [CustomPropertyDrawer(typeof(StatF))]
    public sealed class StatFDrawer : StatDrawer<float, StatF, StatEffectF>
    {
        public override float GetBaseInput(Rect position, StatF stat)
        {
            return EditorGUI.FloatField(position, stat.Base);
        }
    }

    public abstract class StatDrawer<T, TStat, TEffect> : PropertyDrawer where TStat : StatBase<T, TEffect> where TEffect : StatEffect
    {
        private static readonly float lineHeight = EditorGUIUtility.singleLineHeight;

        private bool _expanded;

        public abstract T GetBaseInput(Rect position, TStat stat);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var stat = (TStat)property.boxedValue;
            EditorGUI.BeginProperty(position, label, property);

            Rect row = position.CropHeight(lineHeight);
            if (stat.Effects.Any())
            {
                _expanded = EditorGUI.Foldout(row, _expanded, label, toggleOnLabelClick: false);
            }
            else
            {
                _expanded = false;
                EditorGUI.LabelField(row, label);
            }
            row = row.PushRight(EditorGUIUtility.labelWidth);

            Rect inputRect = row.CropWidth((row.width - lineHeight) * 0.7f);
            stat.Base = GetBaseInput(inputRect, stat);
            row = row.PushRight(inputRect.width);

            Rect arrowRect = row.Crop(lineHeight, lineHeight);
            EditorGUI.LabelField(arrowRect, "→");
            row = row.PushRight(arrowRect.width);

            EditorGUI.LabelField(row, stat.ToString(), EditorHelper.LabelCentered);

            EditorGUI.EndProperty();
            if (GUI.changed)
            {
                property.boxedValue = stat;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return lineHeight;
        }
    }
}
