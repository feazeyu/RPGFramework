using Feazeyu.RPGSystems.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Feazeyu.RPGSystems.Core.Stats
{
    /// <summary>
    /// Base class for a stat whose effective <see cref="Value"/> is derived from a
    /// serialized base value and a list of applied <typeparamref name="TEffect"/>s.
    /// </summary>
    /// <typeparam name="T">Underlying numeric type of the stat.</typeparam>
    /// <typeparam name="TEffect">Effect type that can modify the stat.</typeparam>
    public abstract class StatBase<T, TEffect> : ISerializationCallbackReceiver where TEffect : StatEffect
    {
        /// <summary>The effective value after all effects are applied.</summary>
        public T Value => _value;
        /// <summary>Value.</summary>
        protected T _value;

        /// <summary>The unmodified base value; setting it recomputes <see cref="Value"/>.</summary>
        public T Base
        {
            get => _base;
            set
            {
                _base = value;
                UpdateValue();
            }
        }
        /// <summary>Base.</summary>
        [SerializeField]
        protected T _base = default;

        /// <summary>The effects currently applied to this stat.</summary>
        public IEnumerable<TEffect> Effects => _effects;
        [SerializeField]
        private List<TEffect> _effects = new();

        /// <summary>Creates a stat with the type's default base value.</summary>
        protected StatBase() : this(default)
        {
        }

        /// <summary>Creates a stat with the given base value.</summary>
        public StatBase(T baseValue)
        {
            _base = baseValue;
            Initialize();
            UpdateValue();
        }

        /// <summary>Recomputes <see cref="Value"/> from the base value and applied effects.</summary>
        public virtual void UpdateValue()
        {
            _value = _base;
        }

        /// <summary>Applies an effect and recomputes <see cref="Value"/>.</summary>
        public void Apply(TEffect effect)
        {
            ApplyEffect(effect);
            UpdateValue();
            _effects.Add(effect);
        }

        /// <summary>Folds a single effect into the accumulated modifiers.</summary>
        protected abstract void ApplyEffect(TEffect effect);

        /// <summary>Removes a previously applied effect and recomputes <see cref="Value"/>.</summary>
        public void Remove(TEffect effect)
        {
            if (_effects.Remove(effect))
            {
                RemoveEffect(effect);
                UpdateValue();
            }
        }

        /// <summary>Reverses a single effect's contribution to the accumulated modifiers.</summary>
        protected abstract void RemoveEffect(TEffect effect);

        /// <summary>Returns the effective value as a string.</summary>
        public override string ToString()
        {
            return _value.ToString();
        }

        /// <inheritdoc/>
        public void OnBeforeSerialize()
        {
        }

        /// <inheritdoc/>
        public void OnAfterDeserialize()
        {
            Initialize();
            UpdateValue();
        }

        /// <summary>Resets the accumulated modifiers to their identity values.</summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>Implicitly converts the stat to its effective <see cref="Value"/>.</summary>
        public static implicit operator T(StatBase<T, TEffect> stat)
        {
            return stat.Value;
        }
    }

    /// <summary>
    /// A floating-point stat: effective value is <c>(base + flat) * multiply</c>.
    /// </summary>
    [Serializable]
    public sealed class StatF : StatBase<float, StatEffectF>
    {
        private float _flat;
        private float _multiply;

        /// <summary>Creates a float stat with the given base value.</summary>
        public StatF(float baseValue) : base(baseValue)
        {
        }

        /// <inheritdoc/>
        protected override void Initialize()
        {
            _flat = 0f;
            _multiply = 1f;
        }

        /// <inheritdoc/>
        public override void UpdateValue()
        {
            _value = (_base + _flat) * _multiply;
        }

        /// <inheritdoc/>
        protected override void ApplyEffect(StatEffectF effect)
        {
            _flat += effect.Flat;
            _multiply += effect.Multiply;
        }

        /// <inheritdoc/>
        protected override void RemoveEffect(StatEffectF effect)
        {
            _flat -= effect.Flat;
            _multiply -= effect.Multiply;
        }

        /// <summary>Returns the effective value formatted to two decimals.</summary>
        public override string ToString()
        {
            return _value.ToString("N2");
        }
    }

#if UNITY_EDITOR
    /// <summary>Property drawer for <see cref="StatF"/>.</summary>
    [CustomPropertyDrawer(typeof(StatF))]
    public sealed class StatFDrawer : StatDrawer<float, StatF, StatEffectF>
    {
        /// <inheritdoc/>
        public override float GetBaseInput(Rect position, StatF stat)
        {
            return EditorGUI.FloatField(position, stat.Base);
        }
    }

    /// <summary>
    /// Base property drawer for stats: renders the base input, an arrow, and the
    /// effective value, with a foldout when effects are present.
    /// </summary>
    public abstract class StatDrawer<T, TStat, TEffect> : PropertyDrawer where TStat : StatBase<T, TEffect> where TEffect : StatEffect
    {
        private static readonly float lineHeight = EditorGUIUtility.singleLineHeight;

        private bool _expanded;

        /// <summary>Draws the editable base-value field and returns the entered value.</summary>
        public abstract T GetBaseInput(Rect position, TStat stat);

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return lineHeight;
        }
    }
#endif
}
