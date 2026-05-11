using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueGraph.Runtime;
using UnityEditor;

namespace DialogueGraph.Editor
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
    public class DialogueNodeView : Node
    {
        // ── Events ───────────────────────────────────────────────────────────

        public Action          OnSelect;
        public Action          OnDeselected;
        public Action<Vector2> OnMoved;

        // ── Data ─────────────────────────────────────────────────────────────

        public NodeData Data { get; }

        private readonly GraphAsset                                   m_Asset;
        private readonly IReadOnlyDictionary<string, DialogueNodeInfo> m_NodeRegistry;
        private readonly DialogueNodeInfo                             m_Info;
        private readonly Dictionary<string, Port>                     m_Ports = new Dictionary<string, Port>();

        // ── Construction ─────────────────────────────────────────────────────

        public DialogueNodeView(
            NodeData data,
            GraphAsset asset,
            IReadOnlyDictionary<string, DialogueNodeInfo> nodeRegistry)
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
            }
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
        /// Rebuilds ports and fields in-place.
        ///
        /// The tricky part: GraphView Edge elements hold direct references to Port
        /// VisualElements. When we call outputContainer.Clear() those port objects are
        /// detached from the hierarchy, so every edge that was connected to ANY port on
        /// this node loses its visual anchor — not just the deleted one.
        ///
        /// Strategy:
        ///   1. Before clearing, snapshot all edges connected to this node's outputs,
        ///      keyed by port name.
        ///   2. Identify which port names are being deleted; fully disconnect those edges.
        ///   3. Clear and rebuild ports/fields.
        ///   4. Re-connect surviving edges to their corresponding new port objects.
        /// </summary>
        private void RebuildPortsAndFields()
        {
            var graphView = GetFirstAncestorOfType<UnityEditor.Experimental.GraphView.GraphView>();

            var survivingOut = new HashSet<string>();
            var survivingIn  = new HashSet<string>();
            foreach (var pd in Data.Ports)
            {
                if (pd.Direction == PortDirection.Output) survivingOut.Add(pd.PortName);
                else                                       survivingIn.Add(pd.PortName);
            }

            var outEdgesByPort = new Dictionary<string, List<UnityEditor.Experimental.GraphView.Edge>>();
            var inEdgesByPort  = new Dictionary<string, List<UnityEditor.Experimental.GraphView.Edge>>();
            var edgesToDelete  = new List<UnityEditor.Experimental.GraphView.Edge>();

            if (graphView != null)
            {
                graphView.edges.ForEach(edge =>
                {
                    if (edge.output?.node == this)
                    {
                        var pName = edge.output.portName;
                        if (survivingOut.Contains(pName))
                        {
                            if (!outEdgesByPort.ContainsKey(pName))
                                outEdgesByPort[pName] = new List<UnityEditor.Experimental.GraphView.Edge>();
                            outEdgesByPort[pName].Add(edge);
                        }
                        else
                        {
                            edgesToDelete.Add(edge);
                        }
                        return;
                    }

                    if (edge.input?.node == this)
                    {
                        var pName = edge.input.portName;
                        if (survivingIn.Contains(pName))
                        {
                            if (!inEdgesByPort.ContainsKey(pName))
                                inEdgesByPort[pName] = new List<UnityEditor.Experimental.GraphView.Edge>();
                            inEdgesByPort[pName].Add(edge);
                        }
                        else
                        {
                            edgesToDelete.Add(edge);
                        }
                    }
                });

                foreach (var edge in edgesToDelete)
                {
                    edge.output?.Disconnect(edge);
                    edge.input?.Disconnect(edge);
                    graphView.RemoveElement(edge);
                }

                foreach (var kvp in outEdgesByPort)
                    foreach (var edge in kvp.Value)
                        edge.output?.Disconnect(edge);

                foreach (var kvp in inEdgesByPort)
                    foreach (var edge in kvp.Value)
                        edge.input?.Disconnect(edge);
            }

            inputContainer.Clear();
            outputContainer.Clear();
            m_Ports.Clear();
            BuildPorts();

            if (graphView != null)
            {
                foreach (var kvp in outEdgesByPort)
                {
                    if (!m_Ports.TryGetValue(kvp.Key, out var newPort)) continue;
                    foreach (var edge in kvp.Value)
                    {
                        edge.output = newPort;
                        newPort.Connect(edge);
                    }
                }

                foreach (var kvp in inEdgesByPort)
                {
                    if (!m_Ports.TryGetValue(kvp.Key, out var newPort)) continue;
                    foreach (var edge in kvp.Value)
                    {
                        edge.input = newPort;
                        newPort.Connect(edge);
                    }
                }
            }

            BuildFields();
            RefreshExpandedState();
            RefreshPorts();
        }

        private VisualElement BuildFieldRow(FieldData field)
        {
            var row = new VisualElement();
            row.AddToClassList("node-field-row");

            var nameLabel = new Label(field.FieldName);
            nameLabel.AddToClassList("node-field-name");
            row.Add(nameLabel);

            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
            {
                var bbVar = m_Asset.Blackboard.GetVariable(field.LinkedVariableGuid);
                var linkedLabel = new Label("⟵ " + (bbVar?.Name ?? "?"));
                linkedLabel.AddToClassList("node-field-linked");
                row.Add(linkedLabel);
            }
            else
            {
                var valueField = new TextField { value = field.InlineValue ?? "" };
                valueField.AddToClassList("node-field-value");
                valueField.RegisterValueChangedCallback(evt =>
                {
                    field.InlineValue = evt.newValue;
                    EditorUtilityHelper.SetDirty(m_Asset);
                });
                row.Add(valueField);
            }

            var dot = new VisualElement();
            dot.AddToClassList("node-field-link-dot");
            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
                dot.AddToClassList("linked");
            row.Add(dot);

            dot.tooltip = string.IsNullOrEmpty(field.LinkedVariableGuid)
                ? "Drag a Blackboard variable here to link"
                : "Linked — click to unlink";
            dot.RegisterCallback<ClickEvent>(evt =>
            {
                if (string.IsNullOrEmpty(field.LinkedVariableGuid)) return;
                field.LinkedVariableGuid = null;
                EditorUtilityHelper.SetDirty(m_Asset);
                Refresh();
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
                evt.StopPropagation();
            });
            row.RegisterCallback<DragExitedEvent>(_ => row.RemoveFromClassList("drag-over"));

            return row;
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
