using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Feazeyu.RPGSystems.Dialogue;

namespace Feazeyu.RPGSystems.EditorTools
{
    /// <summary>
    /// Visual representation of a single <see cref="NodeData"/> inside the
    /// GraphView canvas. Agnostic to which graph system (dialogue / quest)
    /// owns the node — the palette lookup goes through an injected
    /// registry so the same class renders both.
    ///
    /// Layout (mirrors Unity Behavior node anatomy):
    ///   ┌──────────────────────────────────┐
    ///   │ ▶ [Icon] [DisplayName]    [type] │  ← header (accent-coloured left stripe)
    ///   ├──────────────────────────────────┤
    ///   │ [field 1] ───○ [bb var name]     │  ← field rows with link indicator
    ///   │ [field 2] ───○                   │
    ///   ├──────────────────────────────────┤
    ///   │       ● Input port               │  ← in port at top of node
    ///   │       ● Output port(s)           │  ← out ports at bottom
    ///   └──────────────────────────────────┘
    /// </summary>
    public class GraphNodeView : Node
    {
        // ── Events ───────────────────────────────────────────────────────────

        public Action          OnSelect;
        public Action          OnDeselected;
        public Action<Vector2> OnMoved;
        // Fired when the node's field data changes from the canvas (e.g. a
        // blackboard variable linked/unlinked via drag-drop or the link dot),
        // so an open inspector showing this node can re-render.
        public Action          OnFieldsChanged;

        // ── Data ─────────────────────────────────────────────────────────────

        public NodeData Data { get; }

        private readonly GraphAsset                                   m_Asset;
        private readonly IReadOnlyDictionary<string, NodeInfo> m_NodeRegistry;
        private readonly NodeInfo                             m_Info;
        private readonly Dictionary<string, Port>                     m_Ports = new Dictionary<string, Port>();

        // ── Construction ─────────────────────────────────────────────────────

        public  GraphNodeView(
            NodeData data,
            GraphAsset asset,
            IReadOnlyDictionary<string, NodeInfo> nodeRegistry)
        {
            Data           = data;
            m_Asset        = asset;
            m_NodeRegistry = nodeRegistry;
            m_Info         = (nodeRegistry != null && nodeRegistry.TryGetValue(data.NodeType, out var info))
                             ? info : null;

            SetPosition(new Rect(data.Position, Vector2.zero));
            userData = data.Guid;

            AddToClassList("dialogue-node");
            if (m_Info != null) AddToClassList("node-" + data.NodeType.ToLower());

            BuildVisuals();

            // Restore saved size (zero = not yet user-resized → leave auto).
            if (data.Size.x > 0 && data.Size.y > 0)
            {
                style.width  = data.Size.x;
                style.height = data.Size.y;
            }

            this.AddManipulator(new NodeResizer(newSize =>
            {
                data.Size = newSize;
                EditorUtilityHelper.SetDirty(m_Asset);
            }));
        }

        // ── Build ────────────────────────────────────────────────────────────

        private void BuildVisuals()
        {
            BuildHeader();
            BuildPorts();
            BuildFields();
            RefreshExpandedState();
            RefreshPorts();
        }

        private void BuildHeader()
        {
            var accent = new VisualElement();
            accent.AddToClassList("node-accent-stripe");
            if (m_Info != null)
            {
                var c = m_Info.AccentColor;
                accent.style.backgroundColor = new StyleColor(c);
            }
            mainContainer.Insert(0, accent);

            titleContainer.Clear();

            var icon = new Label(m_Info?.Icon ?? "●");
            icon.AddToClassList("node-header-icon");
            if (m_Info != null)
                icon.style.color = new StyleColor(m_Info.AccentColor);
            titleContainer.Add(icon);

            var title = new Label(Data.DisplayName);
            title.AddToClassList("node-header-title");
            titleContainer.Add(title);

            var badge = new Label(Data.NodeType);
            badge.AddToClassList("node-type-badge");
            if (m_Info != null)
            {
                var c = m_Info.AccentColor;
                badge.style.color = new StyleColor(new Color(c.r, c.g, c.b, 0.8f));
            }
            titleContainer.Add(badge);

            title.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.clickCount == 2) BeginRename(title);
            });
        }

        private void BuildPorts()
        {
            foreach (var portData in Data.Ports)
                AddPort(portData);
        }

        /// <summary>Instantiates a single port, registers it in <see cref="m_Ports"/>, and appends it to its container.</summary>
        private Port AddPort(PortData portData)
        {
            var dir      = portData.Direction == PortDirection.Input ? Direction.Input : Direction.Output;
            var capacity = portData.Capacity  == PortCapacity.Single ? Port.Capacity.Single : Port.Capacity.Multi;

            // Horizontal orientation: inputs on the left edge of the
            // card, outputs on the right. Unity's EdgeControl derives
            // bezier tangents from the port orientation — horizontal
            // tangents produce smooth left-to-right curves regardless
            // of vertical offset between connected nodes. Vertical
            // ports (what this used to be) produce a characteristic
            // steep-ends / flat-middle S-curve whenever the two nodes
            // have any horizontal offset, because the short vertical
            // tangents don't reach far enough to keep the curve round.
            var port = InstantiatePort(Orientation.Horizontal, dir, capacity, typeof(bool));
            port.portName  = portData.PortName;
            port.portColor = m_Info != null ? m_Info.AccentColor : Color.gray;
            port.userData  = portData;

            port.AddToClassList("dialogue-port");

            if (dir == Direction.Input)
            {
                port.AddToClassList("input-port");
                inputContainer.Add(port);
            }
            else
            {
                port.AddToClassList("output-port");
                outputContainer.Add(port);
            }

            m_Ports[portData.PortName] = port;
            return port;
        }

        private void BuildFields()
        {
            extensionContainer.Clear();

            var hasDecorator = NodeViewDecoratorRegistry.Get(Data.NodeType) != null;

            if ((Data.Fields == null || Data.Fields.Count == 0) && !hasDecorator) return;

            var fieldsContainer = new VisualElement();
            fieldsContainer.AddToClassList("node-fields-container");

            if (Data.Fields != null)
            {
                foreach (var field in Data.Fields)
                    fieldsContainer.Add(BuildFieldRow(field));
            }

            NodeViewDecoratorRegistry.Get(Data.NodeType)
                ?.Invoke(fieldsContainer, Data, m_Asset, RebuildPortsAndFields);

            extensionContainer.Add(fieldsContainer);
        }

        /// <summary>
        /// Rebuilds ports and fields in-place after the node's data changes
        /// (e.g. a decorator added or removed an output port).
        ///
        /// Ports are diffed against <see cref="NodeData.Ports"/> rather than cleared and
        /// rebuilt wholesale. GraphView Edge elements hold direct references to Port
        /// VisualElements, so a full <c>Clear()</c> would detach every port and strand the
        /// edges anchored to them. By keeping surviving ports' VisualElement identity, their
        /// edges stay connected with no bookkeeping: we only remove ports that disappeared
        /// (deleting their edges) and instantiate ports that newly appeared, then reorder the
        /// containers to match the authored order.
        /// </summary>
        private void RebuildPortsAndFields()
        {
            var graphView = GetFirstAncestorOfType<UnityEditor.Experimental.GraphView.GraphView>();

            var desired = new HashSet<string>(Data.Ports.Select(p => p.PortName));

            // Remove ports that no longer exist, deleting any edges anchored to them.
            foreach (var name in m_Ports.Keys.Where(n => !desired.Contains(n)).ToList())
            {
                var port = m_Ports[name];
                foreach (var edge in port.connections.ToList())
                {
                    edge.output?.Disconnect(edge);
                    edge.input?.Disconnect(edge);
                    graphView?.RemoveElement(edge);
                }
                port.RemoveFromHierarchy();
                m_Ports.Remove(name);
            }

            // Instantiate ports that appeared since the last build.
            foreach (var portData in Data.Ports)
                if (!m_Ports.ContainsKey(portData.PortName))
                    AddPort(portData);

            // Reorder each container to match the authored port order — a decorator may
            // insert a port anywhere in the list, not just append. Re-adding an element that
            // is already parented to the container simply moves it, leaving its edges intact.
            foreach (var portData in Data.Ports)
            {
                if (!m_Ports.TryGetValue(portData.PortName, out var port)) continue;
                if (portData.Direction == PortDirection.Input) inputContainer.Add(port);
                else                                            outputContainer.Add(port);
            }

            BuildFields();
            RefreshExpandedState();
            RefreshPorts();
        }

        private VisualElement BuildFieldRow(FieldData field)
        {
            var row = new VisualElement();
            row.AddToClassList("node-field-row");
            // Tag the row with its field name so field decorators (e.g.
            // ChoiceBranchDecorator) can find and replace specific base-built
            // rows instead of rendering duplicates alongside them.
            row.userData = field.FieldName;

            var nameLabel = new Label(field.FieldName);
            nameLabel.AddToClassList("node-field-name");
            row.Add(nameLabel);

            row.Add(BuildFieldValue(field));
            WireFieldLinking(row, field);

            return row;
        }

        /// <summary>
        /// Picks the value control for a field: a read-only label when linked to a
        /// blackboard variable, an operator/choice dropdown for enumerated fields,
        /// a type-appropriate inline control for typed "Value" fields, or a plain
        /// text field otherwise.
        /// </summary>
        private VisualElement BuildFieldValue(FieldData field)
        {
            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
            {
                var bbVar = m_Asset.Blackboard.GetVariable(field.LinkedVariableGuid);
                var linkedLabel = new Label("⟵ " + (bbVar?.Name ?? "?"));
                linkedLabel.AddToClassList("node-field-linked");
                return linkedLabel;
            }

            VisualElement value;
            if (IsOperatorField(field))
                value = BuildDropdown(field, ConditionalOperators);
            else if (TryGetChoiceOptions(Data, field, out var choices))
                value = BuildDropdown(field, choices);
            else if (IsTypedValueField(Data, field))
                value = BuildTypedInlineControl(field, GetLinkedVariableType(Data, m_Asset),
                    () => EditorUtilityHelper.SetDirty(m_Asset));
            else
            {
                var tf = new TextField { value = field.InlineValue ?? "" };
                tf.RegisterValueChangedCallback(evt =>
                {
                    field.InlineValue = evt.newValue;
                    EditorUtilityHelper.SetDirty(m_Asset);
                });
                value = tf;
            }

            value.AddToClassList("node-field-value");
            return value;
        }

        /// <summary>Dropdown over a fixed option set, defaulting to the first when the stored value is unknown.</summary>
        private DropdownField BuildDropdown(FieldData field, List<string> options)
        {
            var current = options.Contains(field.InlineValue) ? field.InlineValue : options[0];
            var dropdown = new DropdownField(options, current);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                field.InlineValue = evt.newValue;
                EditorUtilityHelper.SetDirty(m_Asset);
            });
            return dropdown;
        }

        /// <summary>
        /// Adds the link indicator dot and the blackboard drag-and-drop behaviour
        /// shared by every field row: click the dot to unlink, drop a variable to link.
        /// </summary>
        private void WireFieldLinking(VisualElement row, FieldData field)
        {
            bool linked = !string.IsNullOrEmpty(field.LinkedVariableGuid);

            var dot = new VisualElement();
            dot.AddToClassList("node-field-link-dot");
            if (linked) dot.AddToClassList("linked");
            dot.tooltip = linked
                ? "Linked — click to unlink"
                : "Drag a Blackboard variable here to link";
            row.Add(dot);

            dot.RegisterCallback<ClickEvent>(evt =>
            {
                if (string.IsNullOrEmpty(field.LinkedVariableGuid)) return;
                field.LinkedVariableGuid = null;
                EditorUtilityHelper.SetDirty(m_Asset);
                Refresh();
                OnFieldsChanged?.Invoke();
                evt.StopPropagation();
            });

            // ── Drag & Drop ──────────────────────────────────────────────────
            row.RegisterCallback<DragEnterEvent>(_ =>
            {
                if (DragAndDrop.GetGenericData("BlackboardVariableGuid") is string)
                    row.AddToClassList("drag-over");
            });
            row.RegisterCallback<DragLeaveEvent>(_ => row.RemoveFromClassList("drag-over"));
            row.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (DragAndDrop.GetGenericData("BlackboardVariableGuid") is string)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    evt.StopPropagation();
                }
            });
            row.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (DragAndDrop.GetGenericData("BlackboardVariableGuid") is not string guid) return;

                DragAndDrop.AcceptDrag();
                row.RemoveFromClassList("drag-over");

                field.LinkedVariableGuid = guid;
                EditorUtilityHelper.SetDirty(m_Asset);
                Refresh();
                OnFieldsChanged?.Invoke();
                evt.StopPropagation();
            });
            row.RegisterCallback<DragExitedEvent>(_ => row.RemoveFromClassList("drag-over"));
        }

        // ── Operator field helpers ───────────────────────────────────────────

        internal static readonly List<string> ConditionalOperators
            = new List<string> { "==", "!=", ">", ">=", "<", "<=" };

        internal static bool IsOperatorField(FieldData field)
            => field.TypeName == "conditional_operator" || field.FieldName == "Operator";

        // ── Choice (enum-like) field helpers ─────────────────────────────────

        /// <summary>
        /// Returns the fixed set of options for a field that should render as a
        /// dropdown of predefined string values instead of a free-text field.
        /// Keyed by node type + field name; returns false for free-form fields.
        /// Add new entries here as more nodes gain enumerated fields.
        /// </summary>
        internal static bool TryGetChoiceOptions(NodeData node, FieldData field, out List<string> options)
        {
            options = null;
            if (node == null || field == null) return false;

            if (node.NodeType == NodeRegistry.TypeFindObject && field.FieldName == "Mode")
            {
                options = new List<string> { "ByName", "ByTag" };
                return true;
            }

            return false;
        }

        // ── Typed "Value" input helpers (SetVariable / Condition / Requirement) ──

        /// <summary>
        /// True when <paramref name="field"/> is the "Value" field of a node whose value
        /// is interpreted against a linked blackboard variable's type — SetVariable (the
        /// assigned value) and Condition/Requirement (the right-hand side of the comparison).
        /// These render a type-appropriate inline control instead of a raw text field.
        /// </summary>
        internal static bool IsTypedValueField(NodeData node, FieldData field)
            => field?.FieldName == "Value"
               && (node?.NodeType == NodeRegistry.TypeSetVariable
                || node?.NodeType == NodeRegistry.TypeCondition
                || node?.NodeType == DialogueNodeRegistry.TypeRequirement);

        /// <summary>
        /// Returns the ValueType of the blackboard variable linked to the "Variable"
        /// field of the node, or null if not yet linked. Used to choose the inline
        /// control type for a SetVariable assignment or a Condition/Requirement comparison.
        /// </summary>
        internal static Type GetLinkedVariableType(NodeData node, GraphAsset asset)
        {
            if (node == null || asset == null) return null;
            var varField = node.Fields?.Find(f => f.FieldName == "Variable");
            if (varField == null || string.IsNullOrEmpty(varField.LinkedVariableGuid)) return null;
            return asset.Blackboard.GetVariable(varField.LinkedVariableGuid)?.ValueType;
        }

        /// <summary>
        /// Returns a type-appropriate inline control for a field.
        /// Handles bool, int, float, Vector2, Vector3, Color, and string.
        /// UnityObject types return a hint label (must be blackboard-linked).
        /// </summary>
        internal static VisualElement BuildTypedInlineControl(
            FieldData field, Type type, Action onChanged)
        {
            if (type == typeof(bool))
                return Bind(new Toggle(),       Inline.Bool(field.InlineValue),  Inline.Str, field, onChanged);
            if (type == typeof(int))
                return Bind(new IntegerField(), Inline.Int(field.InlineValue),   Inline.Str, field, onChanged);
            if (type == typeof(float))
                return Bind(new FloatField(),   Inline.Float(field.InlineValue), Inline.Str, field, onChanged);
            if (type == typeof(Vector2))
                return Bind(new Vector2Field(), Inline.Vec2(field.InlineValue),  Inline.Str, field, onChanged);
            if (type == typeof(Vector3))
                return Bind(new Vector3Field(), Inline.Vec3(field.InlineValue),  Inline.Str, field, onChanged);
            if (type == typeof(Color))
                return Bind(new ColorField(),   Inline.Col(field.InlineValue),   Inline.Str, field, onChanged);

            if (type != null && (type == typeof(GameObject) || type == typeof(Transform)
                || type == typeof(Sprite) || type == typeof(AudioClip)))
            {
                var hint = new Label("← link a blackboard variable");
                hint.AddToClassList("node-field-hint");
                return hint;
            }

            // string / unknown / null → default TextField
            return Bind(new TextField(), field.InlineValue ?? "", s => s, field, onChanged);
        }

        /// <summary>
        /// Seeds a typed UIElements control with <paramref name="initial"/> and writes the
        /// serialized form back to <see cref="FieldData.InlineValue"/> on every change.
        /// Removes the repeated parse→create→callback boilerplate across inline controls.
        /// </summary>
        private static TCtrl Bind<TCtrl, TVal>(
            TCtrl ctrl, TVal initial, Func<TVal, string> serialize,
            FieldData field, Action onChanged)
            where TCtrl : VisualElement, INotifyValueChanged<TVal>
        {
            ctrl.SetValueWithoutNotify(initial);
            ctrl.RegisterValueChangedCallback(evt =>
            {
                field.InlineValue = serialize(evt.newValue);
                onChanged?.Invoke();
            });
            return ctrl;
        }

        /// <summary>
        /// Invariant-culture conversion between <see cref="FieldData.InlineValue"/> (a single
        /// serialized string) and the typed values its inline controls expect. Centralised here
        /// so the culture handling lives in one place rather than per control branch.
        /// </summary>
        private static class Inline
        {
            private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

            public static bool  Bool(string s)  { bool.TryParse(s, out var v); return v; }
            public static int   Int(string s)   { int.TryParse(s, out var v); return v; }
            public static float Float(string s) { float.TryParse(s, NumberStyles.Float, Inv, out var v); return v; }
            public static Vector2 Vec2(string s) { var p = Split(s, 2); return new Vector2(Float(p[0]), Float(p[1])); }
            public static Vector3 Vec3(string s) { var p = Split(s, 3); return new Vector3(Float(p[0]), Float(p[1]), Float(p[2])); }
            public static Color Col(string s)
                => ColorUtility.TryParseHtmlString(s ?? "", out var c) ? c : UnityEngine.Color.white;

            public static string Str(bool v)    => v.ToString();
            public static string Str(int v)     => v.ToString(Inv);
            public static string Str(float v)   => v.ToString(Inv);
            public static string Str(Vector2 v) => $"{v.x.ToString(Inv)},{v.y.ToString(Inv)}";
            public static string Str(Vector3 v) => $"{v.x.ToString(Inv)},{v.y.ToString(Inv)},{v.z.ToString(Inv)}";
            public static string Str(Color v)   => "#" + ColorUtility.ToHtmlStringRGBA(v);

            // Splits the comma-separated value into exactly <paramref name="count"/> trimmed
            // components, padding with empty strings so callers can index without bounds checks.
            private static string[] Split(string s, int count)
            {
                var src = (s ?? "").Split(',');
                var outp = new string[count];
                for (int i = 0; i < count; i++)
                    outp[i] = i < src.Length ? src[i].Trim() : "";
                return outp;
            }
        }

        // ── Public API ───────────────────────────────────────────────────────

        public Port GetPort(string portName, Direction dir)
        {
            m_Ports.TryGetValue(portName, out var p);
            return p;
        }

        /// <summary>Rebuild the visual representation (call after data change).</summary>
        public void Refresh()
        {
            RebuildPortsAndFields();
            var titleLabel = titleContainer.Q<Label>(className: "node-header-title");
            if (titleLabel != null) titleLabel.text = Data.DisplayName;
        }

        // ── Selection ────────────────────────────────────────────────────────

        public override void OnSelected()
        {
            base.OnSelected();
            AddToClassList("selected");
            OnSelect?.Invoke();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            RemoveFromClassList("selected");
            OnDeselected?.Invoke();
        }

        // ── Rename ───────────────────────────────────────────────────────────

        private void BeginRename(Label titleLabel)
        {
            var tf = new TextField { value = Data.DisplayName };
            tf.AddToClassList("node-rename-field");
            titleLabel.parent.Add(tf);
            titleLabel.style.display = DisplayStyle.None;
            tf.Q(TextField.textInputUssName).Focus();
            tf.SelectAll();

            void Commit()
            {
                var newName = tf.value.Trim();
                if (!string.IsNullOrEmpty(newName))
                {
                    Data.DisplayName = newName;
                    titleLabel.text  = newName;
                    EditorUtilityHelper.SetDirty(m_Asset);
                }
                titleLabel.style.display = DisplayStyle.Flex;
                tf.RemoveFromHierarchy();
            }

            tf.RegisterCallback<FocusOutEvent>(_ => Commit());
            tf.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) Commit();
                if (evt.keyCode == KeyCode.Escape) { titleLabel.style.display = DisplayStyle.Flex; tf.RemoveFromHierarchy(); }
            });
        }
    }

    // Thin shim so Editor code doesn't depend on UnityEditor directly in this file.
    internal static class EditorUtilityHelper
    {
        public static void SetDirty(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(obj);
#endif
        }
    }
}
