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

            // Restore saved size (zero = not yet user-resized → leave auto).
            if (data.Size.x > 0 && data.Size.y > 0)
            {
                style.width  = data.Size.x;
                style.height = data.Size.y;
            }

            // Attach resize handles. Save the new size into data so it persists.
            this.AddManipulator(new NodeResizer(newSize =>
            {
                data.Size = newSize;
                EditorUtilityHelper.SetDirty(m_Asset);
            }));
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

            bool isChoice = Data.NodeType == NodeRegistry.TypeChoiceBranch;

            if ((Data.Fields == null || Data.Fields.Count == 0) && !isChoice) return;

            var fieldsContainer = new VisualElement();
            fieldsContainer.AddToClassList("node-fields-container");

            // Count how many choice outputs exist so we can show/hide remove buttons.
            int choiceCount = isChoice
                ? Data.Ports.FindAll(p => p.Direction == PortDirection.Output).Count
                : 0;

            if (Data.Fields != null)
            {
                foreach (var field in Data.Fields)
                {
                    // For ChoiceBranch, pair each "Choice N Text" field with a remove button.
                    if (isChoice && field.FieldName.StartsWith("Choice ") && field.FieldName.EndsWith(" Text"))
                    {
                        var row = BuildChoiceFieldRow(field, choiceCount);
                        fieldsContainer.Add(row);
                    }
                    else
                    {
                        fieldsContainer.Add(BuildFieldRow(field));
                    }
                }
            }

            // "+ Add Choice" button — only on ChoiceBranch nodes.
            if (isChoice)
            {
                var addBtn = new Button(AddChoice) { text = "+ Add Choice" };
                addBtn.AddToClassList("node-add-choice-btn");
                addBtn.style.marginTop    = 6;
                addBtn.style.marginBottom = 2;
                addBtn.style.fontSize     = 9;
                addBtn.style.color        = new StyleColor(new Color(0.29f, 0.61f, 0.78f));
                addBtn.style.backgroundColor = new StyleColor(new Color(0.10f, 0.18f, 0.26f));
                addBtn.style.borderTopWidth = addBtn.style.borderBottomWidth =
                addBtn.style.borderLeftWidth = addBtn.style.borderRightWidth = 1;
                addBtn.style.borderTopColor = addBtn.style.borderBottomColor =
                addBtn.style.borderLeftColor = addBtn.style.borderRightColor =
                    new StyleColor(new Color(0.20f, 0.36f, 0.52f));
                addBtn.style.borderTopLeftRadius = addBtn.style.borderTopRightRadius =
                addBtn.style.borderBottomLeftRadius = addBtn.style.borderBottomRightRadius = 3;
                addBtn.style.paddingTop = addBtn.style.paddingBottom = 3;
                fieldsContainer.Add(addBtn);
            }

            extensionContainer.Add(fieldsContainer);
        }

        /// <summary>
        /// Builds a field row for a ChoiceBranch choice, with a "×" remove button.
        /// The remove button is hidden when only one choice remains.
        /// </summary>
        private VisualElement BuildChoiceFieldRow(FieldData field, int totalChoices)
        {
            var row = new VisualElement();
            row.AddToClassList("node-field-row");

            // "×" remove button — shown only when 2+ choices exist.
            var removeBtn = new Button(() => RemoveChoice(field)) { text = "×" };
            removeBtn.style.display       = totalChoices > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            removeBtn.style.fontSize      = 10;
            removeBtn.style.color         = new StyleColor(new Color(0.80f, 0.30f, 0.30f));
            removeBtn.style.backgroundColor = new StyleColor(new Color(0.20f, 0.10f, 0.10f));
            removeBtn.style.borderTopWidth = removeBtn.style.borderBottomWidth =
            removeBtn.style.borderLeftWidth = removeBtn.style.borderRightWidth = 1;
            removeBtn.style.borderTopColor = removeBtn.style.borderBottomColor =
            removeBtn.style.borderLeftColor = removeBtn.style.borderRightColor =
                new StyleColor(new Color(0.36f, 0.14f, 0.14f));
            removeBtn.style.borderTopLeftRadius = removeBtn.style.borderTopRightRadius =
            removeBtn.style.borderBottomLeftRadius = removeBtn.style.borderBottomRightRadius = 3;
            removeBtn.style.width         = 16;
            removeBtn.style.height        = 16;
            removeBtn.style.paddingTop = removeBtn.style.paddingBottom =
            removeBtn.style.paddingLeft = removeBtn.style.paddingRight = 0;
            removeBtn.style.marginRight   = 4;
            removeBtn.style.flexShrink    = 0;
            row.Add(removeBtn);

            // Field name label (e.g. "Choice 1 Text").
            var nameLabel = new Label(field.FieldName);
            nameLabel.AddToClassList("node-field-name");
            row.Add(nameLabel);

            // Value / link display.
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

            // Link dot.
            var dot = new VisualElement();
            dot.AddToClassList("node-field-link-dot");
            dot.tooltip = string.IsNullOrEmpty(field.LinkedVariableGuid)
                ? "Drag a Blackboard variable here to link"
                : "Linked — click to unlink";
            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
                dot.AddToClassList("linked");
            dot.RegisterCallback<ClickEvent>(evt =>
            {
                if (string.IsNullOrEmpty(field.LinkedVariableGuid)) return;
                field.LinkedVariableGuid = null;
                EditorUtilityHelper.SetDirty(m_Asset);
                Refresh();
                evt.StopPropagation();
            });
            row.Add(dot);

            // Drag & drop.
            row.RegisterCallback<DragEnterEvent>(_ =>
            {
                if (DragAndDrop.GetGenericData("BlackboardVariableGuid") is string)
                    row.style.backgroundColor = new StyleColor(new Color(0.18f, 0.30f, 0.45f));
            });
            row.RegisterCallback<DragLeaveEvent>(_ => { row.style.backgroundColor = StyleKeyword.Null; });
            row.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (DragAndDrop.GetGenericData("BlackboardVariableGuid") is string)
                { DragAndDrop.visualMode = DragAndDropVisualMode.Link; evt.StopPropagation(); }
            });
            row.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (DragAndDrop.GetGenericData("BlackboardVariableGuid") is not string guid) return;
                DragAndDrop.AcceptDrag();
                row.style.backgroundColor = StyleKeyword.Null;
                field.LinkedVariableGuid = guid;
                EditorUtilityHelper.SetDirty(m_Asset);
                Refresh();
                evt.StopPropagation();
            });
            row.RegisterCallback<DragExitedEvent>(_ => { row.style.backgroundColor = StyleKeyword.Null; });

            return row;
        }

        /// <summary>
        /// Adds a new choice to a ChoiceBranch node:
        ///   • One Output port ("Choice N")
        ///   • One field ("Choice N Text")
        /// Then rebuilds visuals and ports.
        /// </summary>
        private void AddChoice()
        {
            // Find the highest existing choice number to name the new one.
            int next = 1;
            foreach (var p in Data.Ports)
            {
                if (p.Direction == PortDirection.Output &&
                    p.PortName.StartsWith("Choice ") &&
                    int.TryParse(p.PortName.Substring(7), out int n))
                    next = Mathf.Max(next, n + 1);
            }

            Undo.RecordObject(m_Asset, "Add Choice");

            Data.Ports.Add(new PortData
            {
                PortName  = $"Choice {next}",
                Direction = PortDirection.Output,
                Capacity  = PortCapacity.Single,
            });
            Data.Fields.Add(new FieldData
            {
                FieldName = $"Choice {next} Text",
                TypeName  = "System.String",
            });

            EditorUtilityHelper.SetDirty(m_Asset);
            RebuildPortsAndFields();
        }

        /// <summary>
        /// Removes the choice paired with the given field.
        /// Derives the port name from the field name ("Choice N Text" → "Choice N").
        /// Disconnects any live edges on that port before removing.
        /// </summary>
        private void RemoveChoice(FieldData field)
        {
            // Derive port name: "Choice N Text" → "Choice N"
            var portName = field.FieldName.EndsWith(" Text")
                ? field.FieldName.Substring(0, field.FieldName.Length - 5)
                : null;

            Undo.RecordObject(m_Asset, "Remove Choice");

            // Remove the port and its field from the data.
            if (portName != null)
                Data.Ports.RemoveAll(p => p.PortName == portName && p.Direction == PortDirection.Output);
            Data.Fields.Remove(field);

            // Remove any saved edges that reference this port.
            if (portName != null)
            {
                var edgesToRemove = new List<string>();
                foreach (var e in m_Asset.Edges)
                    if (e.OutputNodeGuid == Data.Guid && e.OutputPortName == portName)
                        edgesToRemove.Add(e.Guid);
                foreach (var guid in edgesToRemove)
                    m_Asset.RemoveEdge(guid);
            }

            EditorUtilityHelper.SetDirty(m_Asset);
            RebuildPortsAndFields();
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

            // Build sets of surviving port names for each direction.
            var survivingOut = new System.Collections.Generic.HashSet<string>();
            var survivingIn  = new System.Collections.Generic.HashSet<string>();
            foreach (var pd in Data.Ports)
            {
                if (pd.Direction == PortDirection.Output) survivingOut.Add(pd.PortName);
                else                                       survivingIn.Add(pd.PortName);
            }

            // portName → edges to re-attach after rebuild
            var outEdgesByPort = new Dictionary<string, List<UnityEditor.Experimental.GraphView.Edge>>();
            var inEdgesByPort  = new Dictionary<string, List<UnityEditor.Experimental.GraphView.Edge>>();
            var edgesToDelete  = new List<UnityEditor.Experimental.GraphView.Edge>();

            if (graphView != null)
            {
                graphView.edges.ForEach(edge =>
                {
                    // ── Output side (this node drives the edge) ──────────────
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

                    // ── Input side (this node receives the edge) ─────────────
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

                // Fully remove edges whose port no longer exists.
                foreach (var edge in edgesToDelete)
                {
                    edge.output?.Disconnect(edge);
                    edge.input?.Disconnect(edge);
                    graphView.RemoveElement(edge);
                }

                // Pre-disconnect surviving edges from the old port objects.
                foreach (var kvp in outEdgesByPort)
                    foreach (var edge in kvp.Value)
                        edge.output?.Disconnect(edge);

                foreach (var kvp in inEdgesByPort)
                    foreach (var edge in kvp.Value)
                        edge.input?.Disconnect(edge);
            }

            // Rebuild ports and fields from data.
            inputContainer.Clear();
            outputContainer.Clear();
            m_Ports.Clear();
            BuildPorts();

            // Re-attach surviving edges to the new port objects.
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

            // Field name label.
            var nameLabel = new Label(field.FieldName);
            nameLabel.AddToClassList("node-field-name");
            row.Add(nameLabel);

            // Value / link display.
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
                EditorUtilityHelper.SetDirty(m_Asset);
                Refresh();
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
                EditorUtilityHelper.SetDirty(m_Asset);
                Refresh();
                evt.StopPropagation();
            });

            row.RegisterCallback<DragExitedEvent>(_ =>
            {
                row.style.backgroundColor = StyleKeyword.Null;
            });

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
            RebuildPortsAndFields();

            // Update the title label.
            var titleLabel = titleContainer.Q<Label>(className: "node-header-title");
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
