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

        /// <summary>Name.</summary>
        public string Name    { get => m_Name;    set => m_Name    = value; }
        /// <summary>Guid.</summary>
        public string Guid    { get => m_Guid;    set => m_Guid    = value; }
        /// <summary>Exposed.</summary>
        public bool   Exposed { get => m_Exposed; set => m_Exposed = value; }
        /// <summary>Shared.</summary>
        public bool   Shared  { get => m_Shared;  set => m_Shared  = value; }

        /// <summary>On value changed.</summary>
        public event Action OnValueChanged;
        /// <summary>Notify value changed.</summary>
        protected void NotifyValueChanged() => OnValueChanged?.Invoke();
        /// <summary>Invoke value changed.</summary>
        public void InvokeValueChanged() => OnValueChanged?.Invoke();

        /// <summary>Value type.</summary>
        public abstract Type   ValueType   { get; }
        /// <summary>Object value.</summary>
        public abstract object ObjectValue { get; set; }
        /// <inheritdoc/>
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

        /// <summary>Initializes a new instance of the <see cref="BlackboardVariable"/> class.</summary>
        protected BlackboardVariable() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariable"/> class.</summary>
        protected BlackboardVariable(T value) { m_Value = value; }

        /// <summary>Value.</summary>
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

        /// <inheritdoc/>
        public override Type   ValueType   => typeof(T);
        /// <inheritdoc/>
        public override object ObjectValue { get => m_Value; set => Value = (T)value; }
    }


    [Serializable] public sealed class BlackboardVariableBool : BlackboardVariable<bool>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableBool"/> class.</summary>
        public BlackboardVariableBool() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableBool"/> class.</summary>
        public BlackboardVariableBool(bool v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableBool(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableInt : BlackboardVariable<int>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableInt"/> class.</summary>
        public BlackboardVariableInt() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableInt"/> class.</summary>
        public BlackboardVariableInt(int v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableInt(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableFloat : BlackboardVariable<float>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableFloat"/> class.</summary>
        public BlackboardVariableFloat() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableFloat"/> class.</summary>
        public BlackboardVariableFloat(float v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableFloat(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableString : BlackboardVariable<string>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableString"/> class.</summary>
        public BlackboardVariableString() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableString"/> class.</summary>
        public BlackboardVariableString(string v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableString(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableVector2 : BlackboardVariable<Vector2>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableVector2"/> class.</summary>
        public BlackboardVariableVector2() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableVector2"/> class.</summary>
        public BlackboardVariableVector2(Vector2 v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableVector2(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableVector3 : BlackboardVariable<Vector3>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableVector3"/> class.</summary>
        public BlackboardVariableVector3() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableVector3"/> class.</summary>
        public BlackboardVariableVector3(Vector3 v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableVector3(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableColor : BlackboardVariable<Color>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableColor"/> class.</summary>
        public BlackboardVariableColor() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableColor"/> class.</summary>
        public BlackboardVariableColor(Color v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableColor(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableGameObject : BlackboardVariable<GameObject>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableGameObject"/> class.</summary>
        public BlackboardVariableGameObject() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableGameObject"/> class.</summary>
        public BlackboardVariableGameObject(GameObject v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableGameObject(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableTransform : BlackboardVariable<Transform>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableTransform"/> class.</summary>
        public BlackboardVariableTransform() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableTransform"/> class.</summary>
        public BlackboardVariableTransform(Transform v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableTransform(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableSprite : BlackboardVariable<Sprite>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableSprite"/> class.</summary>
        public BlackboardVariableSprite() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableSprite"/> class.</summary>
        public BlackboardVariableSprite(Sprite v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableSprite(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    [Serializable] public sealed class BlackboardVariableAudioClip : BlackboardVariable<AudioClip>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableAudioClip"/> class.</summary>
        public BlackboardVariableAudioClip() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableAudioClip"/> class.</summary>
        public BlackboardVariableAudioClip(AudioClip v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableAudioClip(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }

    /// <summary>
    /// Holds a reference to a <see cref="DialogueGraphAsset"/> so a dialogue
    /// graph can store another graph as a variable — e.g. linked to a Run
    /// Subgraph node's "Graph" field (asset references can't be stored inline,
    /// so this is the only way to feed a graph to that node). Mirrors the
    /// QuestGraph variable in the quest system.
    /// </summary>
    [Serializable] public sealed class BlackboardVariableDialogueGraph : BlackboardVariable<DialogueGraphAsset>
    {
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableDialogueGraph"/> class.</summary>
        public BlackboardVariableDialogueGraph() { }
        /// <summary>Initializes a new instance of the <see cref="BlackboardVariableDialogueGraph"/> class.</summary>
        public BlackboardVariableDialogueGraph(DialogueGraphAsset v) : base(v) { }
        /// <inheritdoc/>
        public override BlackboardVariable Clone() => new BlackboardVariableDialogueGraph(m_Value) { Name = Name, Guid = Guid, Exposed = Exposed, Shared = Shared };
    }
}
