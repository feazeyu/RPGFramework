using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueGraph.Runtime;

namespace DialogueGraph.Editor
{
    public class InspectorPanel : VisualElement
    {
        private GraphAsset m_Asset;
        private NodeData           m_Node;
        private Action<string>     m_RefreshNodeView;

        private ScrollView    m_Scroll;
        private VisualElement m_Content;

        public InspectorPanel()
        {
            AddToClassList("inspector-panel");
            BuildHeader();
            BuildScrollArea();
            ShowEmpty();
        }

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("inspector-header");

            var icon = new Label("☰");
            icon.AddToClassList("inspector-header-icon");
            header.Add(icon);

            var title = new Label("Inspector");
            title.AddToClassList("inspector-header-title");
            header.Add(title);

            Add(header);
        }

        private void BuildScrollArea()
        {
            m_Scroll = new ScrollView(ScrollViewMode.Vertical);
            m_Scroll.style.flexGrow = 1;
            m_Content = m_Scroll.contentContainer;
            m_Content.style.flexDirection = FlexDirection.Column;
            Add(m_Scroll);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public new void Clear()
        {
            m_Node  = null;
            m_Asset = null;
            ShowEmpty();
        }

        public void InspectAsset(GraphAsset asset)
        {
            m_Asset = asset;
            m_Node  = null;
            if (asset == null) { ShowEmpty(); return; }
            ShowAssetView(asset);
        }

        public void InspectNode(NodeData node, GraphAsset asset, Action<string> refreshNodeView)
        {
            m_Node            = node;
            m_Asset           = asset;
            m_RefreshNodeView = refreshNodeView;
            ShowNodeView(node, asset);
        }

        /// <summary>
        /// Guid of the currently-inspected node, or empty if the inspector
        /// is showing the asset view / empty state. Used by the window to
        /// decide whether a node-level change (e.g. blackboard variable
        /// deletion cascading into a field) affects what the inspector
        /// is showing.
        /// </summary>
        public string CurrentNodeGuid => m_Node?.Guid ?? string.Empty;

        /// <summary>
        /// Re-render the currently-inspected node from its (possibly
        /// mutated) <see cref="NodeData"/>. No-op when the inspector
        /// isn't showing a node.
        /// </summary>
        public void RefreshCurrent()
        {
            if (m_Node != null && m_Asset != null) ShowNodeView(m_Node, m_Asset);
        }

        // ── Views ─────────────────────────────────────────────────────────────

        private void ShowEmpty()
        {
            m_Content.Clear();
            var msg = new Label("Select a node to\ninspect its properties.");
            msg.AddToClassList("inspector-empty");
            m_Content.Add(msg);
        }

        private void ShowAssetView(GraphAsset asset)
        {
            m_Content.Clear();
            var section = MakeSection("Graph Asset");

            section.Add(MakeFieldLabel("Name"));
            var nameField = new TextField { value = asset.name, isReadOnly = true };
            nameField.AddToClassList("inspector-text-field");
            section.Add(nameField);

            section.Add(MakeFieldLabel("Nodes"));
            section.Add(MakeValueLabel(asset.Nodes.Count.ToString()));

            section.Add(MakeFieldLabel("Blackboard Variables"));
            section.Add(MakeValueLabel(asset.Blackboard.Variables.Count.ToString()));

            m_Content.Add(section);
        }

        private void ShowNodeView(NodeData node, GraphAsset asset)
        {
            m_Content.Clear();
            var info = NodeRegistry.Get(node.NodeType);

            // Node header card — tinted with accent color (stays inline, data-driven).
            var headerCard = new VisualElement();
            headerCard.AddToClassList("inspector-node-header");
            headerCard.style.backgroundColor = info != null
                ? new StyleColor(new Color(info.AccentColor.r, info.AccentColor.g, info.AccentColor.b, 0.10f))
                : new StyleColor(new Color(0.16f, 0.18f, 0.24f));

            var titleRow = new VisualElement();
            titleRow.AddToClassList("inspector-node-title-row");

            var iconLabel = new Label(info?.Icon ?? "●");
            iconLabel.AddToClassList("inspector-node-icon");
            iconLabel.style.color = info != null
                ? new StyleColor(info.AccentColor)
                : new StyleColor(Color.gray);
            titleRow.Add(iconLabel);

            var nodeNameLabel = new Label(node.DisplayName);
            nodeNameLabel.AddToClassList("inspector-node-name");
            titleRow.Add(nodeNameLabel);
            headerCard.Add(titleRow);

            var badge = new Label((info?.Category ?? "") + "  /  " + (info?.DisplayName ?? node.NodeType));
            badge.AddToClassList("inspector-node-badge");
            headerCard.Add(badge);

            if (info != null && !string.IsNullOrEmpty(info.Description))
            {
                var desc = new Label(info.Description);
                desc.AddToClassList("inspector-node-desc");
                headerCard.Add(desc);
            }

            m_Content.Add(headerCard);

            // Node section (display name + GUID)
            var nameSection = MakeSection("Node");

            nameSection.Add(MakeFieldLabel("Display Name"));
            var nameField = new TextField { value = node.DisplayName };
            nameField.AddToClassList("inspector-text-field");
            nameField.RegisterValueChangedCallback(evt =>
            {
                node.DisplayName   = evt.newValue;
                nodeNameLabel.text = evt.newValue;
                m_RefreshNodeView?.Invoke(node.Guid);
                if (asset) EditorUtility.SetDirty(asset);
            });
            nameSection.Add(nameField);

            nameSection.Add(MakeFieldLabel("GUID"));
            var guidField = new TextField { value = node.Guid, isReadOnly = true };
            guidField.AddToClassList("inspector-text-field");
            guidField.AddToClassList("inspector-text-field-small");
            nameSection.Add(guidField);

            m_Content.Add(nameSection);

            // Fields section
            if (node.Fields != null && node.Fields.Count > 0)
            {
                var fieldsSection = MakeSection("Fields");
                foreach (var field in node.Fields)
                    fieldsSection.Add(BuildFieldEditor(field, asset));
                m_Content.Add(fieldsSection);
            }

            // Ports section
            if (node.Ports != null && node.Ports.Count > 0)
            {
                var portsSection = MakeSection("Ports");
                foreach (var port in node.Ports)
                {
                    var portRow = new VisualElement();
                    portRow.AddToClassList("inspector-port-row");

                    var dirLabel = new Label(port.Direction == PortDirection.Input ? "↓" : "↑");
                    dirLabel.AddToClassList("inspector-port-dir");
                    dirLabel.AddToClassList(port.Direction == PortDirection.Input ? "input" : "output");
                    portRow.Add(dirLabel);

                    var portLabel = new Label(port.PortName);
                    portLabel.AddToClassList("inspector-port-name");
                    portRow.Add(portLabel);

                    var capLabel = new Label(port.Capacity.ToString());
                    capLabel.AddToClassList("inspector-port-cap");
                    portRow.Add(capLabel);

                    portsSection.Add(portRow);
                }
                m_Content.Add(portsSection);
            }
        }

        // ── Field editor ──────────────────────────────────────────────────────

        private VisualElement BuildFieldEditor(FieldData field, GraphAsset asset)
        {
            var container = new VisualElement();
            container.AddToClassList("inspector-field-editor");

            var nameRow = new VisualElement();
            nameRow.AddToClassList("inspector-field-name-row");

            var fieldName = new Label(field.FieldName);
            fieldName.AddToClassList("inspector-field-name");
            nameRow.Add(fieldName);

            var typePill = new Label(ShortType(field.TypeName));
            typePill.AddToClassList("inspector-field-type-pill");
            nameRow.Add(typePill);

            container.Add(nameRow);

            var valueRow = new VisualElement();
            valueRow.AddToClassList("inspector-value-row");

            bool linked = !string.IsNullOrEmpty(field.LinkedVariableGuid);

            if (linked)
            {
                var bbVar     = asset?.Blackboard.GetVariable(field.LinkedVariableGuid);
                var linkLabel = new Label("⟵  " + (bbVar?.Name ?? "?  (missing)"));
                linkLabel.AddToClassList("inspector-link-label");
                valueRow.Add(linkLabel);
            }
            else
            {
                var valueField = new TextField { value = field.InlineValue ?? "" };
                valueField.AddToClassList("inspector-text-field");
                valueField.style.flexGrow = 1;
                valueField.RegisterValueChangedCallback(evt =>
                {
                    field.InlineValue = evt.newValue;
                    if (asset) EditorUtility.SetDirty(asset);
                    m_RefreshNodeView?.Invoke(m_Node?.Guid ?? "");
                });
                valueRow.Add(valueField);
            }

            var linkBtn = new Button(() => ShowLinkMenu(field, asset, valueRow)) { text = linked ? "◉" : "◯" };
            linkBtn.AddToClassList("inspector-link-btn");
            if (linked) linkBtn.AddToClassList("linked");
            linkBtn.tooltip = linked ? "Unlink from Blackboard variable" : "Link to a Blackboard variable";
            valueRow.Add(linkBtn);

            container.Add(valueRow);
            return container;
        }

        private void ShowLinkMenu(FieldData field, GraphAsset asset, VisualElement valueRow)
        {
            if (asset == null) return;

            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
            {
                Undo.RecordObject(asset, "Unlink Field");
                field.LinkedVariableGuid = null;
                EditorUtility.SetDirty(asset);
                // Also refresh the canvas node view — otherwise the node
                // card keeps rendering the field as linked ("⟵ ...") even
                // though the data now says it's inline.
                m_RefreshNodeView?.Invoke(m_Node?.Guid ?? "");
                ShowNodeView(m_Node, asset);
                return;
            }

            var menu = new GenericMenu();
            bool any = false;
            foreach (var variable in asset.Blackboard.Variables)
            {
                var captured = variable;
                menu.AddItem(
                    new GUIContent($"{variable.Name}  [{ShortType(variable.ValueType?.Name)}]"),
                    false,
                    () =>
                    {
                        Undo.RecordObject(asset, "Link Field to Blackboard");
                        field.LinkedVariableGuid = captured.Guid;
                        field.InlineValue        = null;
                        EditorUtility.SetDirty(asset);
                        m_RefreshNodeView?.Invoke(m_Node?.Guid ?? "");
                        ShowNodeView(m_Node, asset);
                    });
                any = true;
            }

            if (!any)
                menu.AddDisabledItem(new GUIContent("No Blackboard variables defined"));

            menu.ShowAsContext();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement();
            section.AddToClassList("inspector-section");

            var titleLabel = new Label(title.ToUpper());
            titleLabel.AddToClassList("inspector-section-title");
            section.Add(titleLabel);

            return section;
        }

        private static Label MakeFieldLabel(string text)
        {
            var l = new Label(text);
            l.AddToClassList("inspector-field-label");
            return l;
        }

        private static Label MakeValueLabel(string text)
        {
            var l = new Label(text);
            l.AddToClassList("inspector-value-label");
            return l;
        }

        private static string ShortType(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName)) return "?";
            var parts = fullTypeName.Split('.');
            return parts[parts.Length - 1];
        }
    }
}
