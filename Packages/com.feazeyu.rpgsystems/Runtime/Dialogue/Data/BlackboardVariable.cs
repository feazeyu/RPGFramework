using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Abstract base — holds Name, Guid, Exposed, Shared metadata.
    /// Never serialised directly; always via a concrete subclass.
    /// </summary>
    [Serializable]
    public abstract class BlackboardVariable
    {
        [SerializeField] private string m_Name;
        [SerializeField] private string m_Guid;
        [SerializeField] private bool   m_Exposed;
        [SerializeField] private bool   m_Shared;

        public string Name    { get => m_Name;    set => m_Name    = value; }
        public string Guid    { get => m_Guid;    set => m_Guid    = value; }
        public bool   Exposed { get => m_Exposed; set => m_Exposed = value; }
        public bool   Shared  { get => m_Shared;  set => m_Shared  = value; }

        public event Action OnValueChanged;
        protected void NotifyValueChanged() => OnValueChanged?.Invoke();
        public void InvokeValueChanged() => OnValueChanged?.Invoke();

        public abstract Type   ValueType   { get; }
        public abstract object ObjectValue { get; set; }
        public abstract BlackboardVariable Clone();
    }

    /// <summary>
    /// Typed intermediate — provides the strongly-typed Value property.
    /// Subclasses must be concrete (non-generic) so Unity's SerializedProperty
    /// absolute-path lookup can resolve m_Value against the concrete runtime type.
    ///
    /// WHY CONCRETE SUBCLASSES:
    /// SerializedObject.FindProperty("...Array.data[i].m_Value") resolves field
    /// names against the actual stored type. A raw BlackboardVariable&lt;T&gt; works
    /// at runtime but the serializer's reflection sees the closed generic name
    /// (e.g. "BlackboardVariable`1[[System.Int32]]") and cannot reliably navigate
    /// into m_Value when T is a value type. A concrete sealed class gives the
    /// serializer a stable, non-generic type to reflect against.
    /// </summary>
    [Serializable]
    public abstract class BlackboardVariable<T> : BlackboardVariable
    {
        [SerializeField] protected T m_Value;

        protected BlackboardVariable() { }
        protected BlackboardVariable(T value) { m_Value = value; }

        public T Value
        {
            get => m_Value;
            set
            {
                if (!Equals(m_Value, value))
                {
                    m_Value = value;
                    NotifyValueChanged();
                }
            }
        }

        public override Type   ValueType   => typeof(T);
        public override object ObjectValue { get => m_Value; set => Value = (T)value; }
    }

    // ── Concrete sealed types ─────────────────────────────────────────────────

    [Serializable] public sealed class BlackboardVariableBool : BlackboardVariable<bool>
    {
        public BlackboardVariableBool() { }
        public BlackboardVariableBool(bool v) : base(v) { }
        public override BlackboardVariable Clone() => new BlackboardVariableBool(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableInt : BlackboardVariable<int>
    {
        public BlackboardVariableInt() { }
        public BlackboardVariableInt(int v) : base(v) { }
        public override BlackboardVariable Clone() => new BlackboardVariableInt(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableFloat : BlackboardVariable<float>
    {
        public BlackboardVariableFloat() { }
        public BlackboardVariableFloat(float v) : base(v) { }
        public override BlackboardVariable Clone() => new BlackboardVariableFloat(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableString : BlackboardVariable<string>
    {
        public BlackboardVariableString() { }
        public BlackboardVariableString(string v) : base(v) { }
        public override BlackboardVariable Clone() => new BlackboardVariableString(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableVector2 : BlackboardVariable<Vector2>
    {
        public BlackboardVariableVector2() { }
        public BlackboardVariableVector2(Vector2 v) : base(v) { }
        public override BlackboardVariable Clone() => new BlackboardVariableVector2(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableVector3 : BlackboardVariable<Vector3>
    {
        public BlackboardVariableVector3() { }
        public BlackboardVariableVector3(Vector3 v) : base(v) { }
        public override BlackboardVariable Clone() => new BlackboardVariableVector3(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableColor : BlackboardVariable<Color>
    {
        public BlackboardVariableColor() { }
        public BlackboardVariableColor(Color v) : base(v) { }
        public override BlackboardVariable Clone() => new BlackboardVariableColor(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableGameObject : BlackboardVariable<GameObject>
    {
        public BlackboardVariableGameObject() { }
        public BlackboardVariableGameObject(GameObject v) : base(v) { }
        public override BlackboardVariable Clone() => new BlackboardVariableGameObject(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableTransform : BlackboardVariable<Transform>
    {
        public BlackboardVariableTransform() { }
        public BlackboardVariableTransform(Transform v) : base(v) { }
        public override BlackboardVariable Clone() => new BlackboardVariableTransform(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableSprite : BlackboardVariable<Sprite>
    {
        public BlackboardVariableSprite() { }
        public BlackboardVariableSprite(Sprite v) : base(v) { }
        public override BlackboardVariable Clone() => new BlackboardVariableSprite(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableAudioClip : BlackboardVariable<AudioClip>
    {
        public BlackboardVariableAudioClip() { }
        public BlackboardVariableAudioClip(AudioClip v) : base(v) { }
        public override BlackboardVariable Clone() => new BlackboardVariableAudioClip(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }
}
