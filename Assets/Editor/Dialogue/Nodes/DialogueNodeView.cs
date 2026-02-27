using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueGraph.Runtime;

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Visual representation of a single NodeData inside the GraphView canvas.
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
        // ── Events ────────────────────────────────────────────────────────────

        public Action          OnSelect;
        public Action          OnDeselected;
        public Action<Vector2> OnMoved;

        // ── Data ──────────────────────────────────────────────────────────────

        public NodeData Data { get; }

        private readonly DialogueGraphAsset m_Asset;
        private readonly DialogueNodeInfo   m_Info;
        private readonly Dictionary<string, Port> m_Ports = new Dictionary<string, Port>();

        // ── Construction ──────────────────────────────────────────────────────

        public DialogueNodeView(NodeData data, DialogueGraphAsset asset)
        {
            Data    = data;
            m_Asset = asset;
            m_Info  = NodeRegistry.Get(data.NodeType);

            SetPosition(new Rect(data.Position, Vector2.zero));
            userData = data.Guid;

            AddToClassList("dialogue-node");
            if (m_Info != null) AddToClassList("node-" + data.NodeType.ToLower());

            BuildVisuals();
        }

        // ── Build ─────────────────────────────────────────────────────────────

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
            // Accent colour stripe on the left edge.
            var accent = new VisualElement();
            accent.AddToClassList("node-accent-stripe");
            if (m_Info != null)
            {
                var c = m_Info.AccentColor;
                accent.style.backgroundColor = new StyleColor(c);
            }
            mainContainer.Insert(0, accent);

            // Clear the default title label, rebuild it with our style.
            titleContainer.Clear();

            var icon = new Label(m_Info?.Icon ?? "●");
            icon.AddToClassList("node-header-icon");
            if (m_Info != null)
                icon.style.color = new StyleColor(m_Info.AccentColor);
            titleContainer.Add(icon);

            var title = new Label(Data.DisplayName);
            title.AddToClassList("node-header-title");
            titleContainer.Add(title);

            // Type badge (top-right).
            var badge = new Label(Data.NodeType);
            badge.AddToClassList("node-type-badge");
            if (m_Info != null)
            {
                var c = m_Info.AccentColor;
                badge.style.color = new StyleColor(new Color(c.r, c.g, c.b, 0.8f));
            }
            titleContainer.Add(badge);

            // Allow renaming on double-click of the title.
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

                var port = InstantiatePort(Orientation.Vertical, dir, capacity, typeof(bool));
                port.portName     = portData.PortName;
                port.portColor    = m_Info != null ? m_Info.AccentColor : Color.gray;
                port.userData     = portData;

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

            if (Data.Fields == null || Data.Fields.Count == 0) return;

            var fieldsContainer = new VisualElement();
            fieldsContainer.AddToClassList("node-fields-container");

            foreach (var field in Data.Fields)
            {
                var row = BuildFieldRow(field);
                fieldsContainer.Add(row);
            }

            extensionContainer.Add(fieldsContainer);
        }

        private VisualElement BuildFieldRow(FieldData field)
        {
            var row = new VisualElement();
            row.AddToClassList("node-field-row");

            // Field name label.
            var nameLabel = new Label(field.FieldName);
            nameLabel.AddToClassList("node-field-name");
            row.Add(nameLabel);

            // Value / link indicator.
            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
            {
                // Linked to a blackboard variable — show the variable name.
                var bbVar = m_Asset.Blackboard.GetVariable(field.LinkedVariableGuid);
                var linkedLabel = new Label("⟵ " + (bbVar?.Name ?? "?"));
                linkedLabel.AddToClassList("node-field-linked");
                row.Add(linkedLabel);
            }
            else
            {
                // Inline value — show an editable field.
                var valueField = new TextField { value = field.InlineValue ?? "" };
                valueField.AddToClassList("node-field-value");
                valueField.RegisterValueChangedCallback(evt =>
                {
                    field.InlineValue = evt.newValue;
                    EditorUtilityHelper.SetDirty(m_Asset);
                });
                row.Add(valueField);
            }

            // Link indicator dot.
            var dot = new VisualElement();
            dot.AddToClassList("node-field-link-dot");
            dot.tooltip = string.IsNullOrEmpty(field.LinkedVariableGuid)
                ? "Not linked to Blackboard"
                : "Linked to Blackboard variable";
            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
                dot.AddToClassList("linked");
            row.Add(dot);

            return row;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public Port GetPort(string portName, Direction dir)
        {
            m_Ports.TryGetValue(portName, out var p);
            return p;
        }

        /// <summary>Rebuild the visual representation (call after data change).</summary>
        public void Refresh()
        {
            // Rebuild just the fields section.
            BuildFields();
            RefreshExpandedState();

            // Update the title label.
            var titleLabel = titleContainer.Q<Label>("node-header-title");
            if (titleLabel != null) titleLabel.text = Data.DisplayName;
        }

        // ── Selection ─────────────────────────────────────────────────────────

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
