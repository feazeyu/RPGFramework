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

            // Value / link display — rebuilt by RefreshFieldRow when link changes.
            var valueSlot = new VisualElement();
            valueSlot.style.flexGrow = 1;
            row.Add(valueSlot);
            RefreshValueSlot(valueSlot, field);

            // Link indicator dot — also the drop target for blackboard variables.
            var dot = new VisualElement();
            dot.AddToClassList("node-field-link-dot");
            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
                dot.AddToClassList("linked");
            row.Add(dot);

            // Clicking the dot unlinks the field.
            dot.tooltip = string.IsNullOrEmpty(field.LinkedVariableGuid)
                ? "Drag a Blackboard variable here to link"
                : "Linked — click to unlink";
            dot.RegisterCallback<ClickEvent>(evt =>
            {
                if (string.IsNullOrEmpty(field.LinkedVariableGuid)) return;
                field.LinkedVariableGuid = null;
                RefreshValueSlot(valueSlot, field);
                dot.RemoveFromClassList("linked");
                dot.tooltip = "Drag a Blackboard variable here to link";
                EditorUtilityHelper.SetDirty(m_Asset);
                evt.StopPropagation();
            });

            // ── Drag & Drop ──────────────────────────────────────────────────
            // The entire row is a drop target for blackboard variable GUIDs.
            // We highlight on DragEnter/DragUpdated and commit on DragPerform.

            row.RegisterCallback<DragEnterEvent>(_ =>
            {
                if (DragAndDrop.GetGenericData("BlackboardVariableGuid") is string)
                    row.style.backgroundColor = new StyleColor(new Color(0.18f, 0.30f, 0.45f));
            });

            row.RegisterCallback<DragLeaveEvent>(_ =>
            {
                row.style.backgroundColor = StyleKeyword.Null;
            });

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
                row.style.backgroundColor = StyleKeyword.Null;

                field.LinkedVariableGuid = guid;
                RefreshValueSlot(valueSlot, field);
                dot.AddToClassList("linked");
                dot.tooltip = "Linked — click to unlink";
                EditorUtilityHelper.SetDirty(m_Asset);
                evt.StopPropagation();
            });

            row.RegisterCallback<DragExitedEvent>(_ =>
            {
                row.style.backgroundColor = StyleKeyword.Null;
            });

            return row;
        }

        private void RefreshValueSlot(VisualElement slot, FieldData field)
        {
            slot.Clear();
            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
            {
                var bbVar = m_Asset.Blackboard.GetVariable(field.LinkedVariableGuid);
                var linkedLabel = new Label("⟵ " + (bbVar?.Name ?? "?"));
                linkedLabel.AddToClassList("node-field-linked");
                slot.Add(linkedLabel);
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
                slot.Add(valueField);
            }
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
