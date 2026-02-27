using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueGraph.Runtime;

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Right-hand Inspector panel. Mirrors Unity Behavior's Node Inspector.
    ///
    /// Shows three modes:
    ///   1. Empty   — "Select a node to inspect"
    ///   2. Asset   — shows graph-level properties (name, description)
    ///   3. Node    — shows full node properties with field editors and BB linking
    ///
    /// Field linking allows wiring a node field directly to a Blackboard variable
    /// by clicking the ◯ link button next to each field.
    /// </summary>
    public class InspectorPanel : VisualElement
    {
        // ── Colours ───────────────────────────────────────────────────────────

        private static readonly Color BgPanel    = new Color(0.13f, 0.14f, 0.18f);
        private static readonly Color BgHeader   = new Color(0.11f, 0.12f, 0.16f);
        private static readonly Color BgSection  = new Color(0.10f, 0.11f, 0.15f);
        private static readonly Color ColText    = new Color(0.82f, 0.86f, 0.95f);
        private static readonly Color ColMuted   = new Color(0.50f, 0.55f, 0.65f);
        private static readonly Color ColDivider = new Color(0.10f, 0.11f, 0.15f);

        // ── State ─────────────────────────────────────────────────────────────

        private DialogueGraphAsset m_Asset;
        private NodeData           m_Node;
        private DialogueGraphView  m_GraphView;

        private ScrollView         m_Scroll;
        private VisualElement      m_Content;

        // ── Construction ──────────────────────────────────────────────────────

        public InspectorPanel()
        {
            style.backgroundColor = new StyleColor(BgPanel);
            style.flexDirection   = FlexDirection.Column;

            BuildHeader();
            BuildScrollArea();
            ShowEmpty();
        }

        // ── Layout ────────────────────────────────────────────────────────────

        private void BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection   = FlexDirection.Row;
            header.style.alignItems      = Align.Center;
            header.style.paddingTop      = 8;
            header.style.paddingBottom   = 8;
            header.style.paddingLeft     = 10;
            header.style.paddingRight    = 10;
            header.style.backgroundColor = new StyleColor(BgHeader);
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new StyleColor(ColDivider);

            var icon = new Label("☰");
            icon.style.fontSize   = 12;
            icon.style.color      = new StyleColor(new Color(0.94f, 0.65f, 0.20f));
            icon.style.marginRight = 6;
            header.Add(icon);

            var title = new Label("Inspector");
            title.style.fontSize = 11;
            title.style.color    = new StyleColor(ColText);
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

        public void InspectAsset(DialogueGraphAsset asset)
        {
            m_Asset = asset;
            m_Node  = null;
            if (asset == null) { ShowEmpty(); return; }
            ShowAssetView(asset);
        }

        public void InspectNode(NodeData node, DialogueGraphAsset asset, DialogueGraphView graphView)
        {
            m_Node      = node;
            m_Asset     = asset;
            m_GraphView = graphView;
            ShowNodeView(node, asset);
        }

        // ── View modes ────────────────────────────────────────────────────────

        private void ShowEmpty()
        {
            m_Content.Clear();
            var msg = new Label("Select a node to\ninspect its properties.");
            msg.style.color            = new StyleColor(ColMuted);
            msg.style.fontSize         = 11;
            msg.style.unityTextAlign   = TextAnchor.MiddleCenter;
            msg.style.whiteSpace       = WhiteSpace.Normal;
            msg.style.marginTop        = 40;
            msg.style.paddingLeft      = 20;
            msg.style.paddingRight     = 20;
            m_Content.Add(msg);
        }

        // ── Asset view ────────────────────────────────────────────────────────

        private void ShowAssetView(DialogueGraphAsset asset)
        {
            m_Content.Clear();

            var section = MakeSection("Graph Asset");

            // Name (read-only, from asset filename).
            section.Add(MakeFieldLabel("Name"));
            var nameField = new TextField { value = asset.name, isReadOnly = true };
            StyleTextField(nameField);
            section.Add(nameField);

            // Node count.
            section.Add(MakeFieldLabel("Nodes"));
            var countLabel = new Label(asset.Nodes.Count.ToString());
            countLabel.style.fontSize = 11;
            countLabel.style.color    = new StyleColor(ColText);
            section.Add(countLabel);

            // Variable count.
            section.Add(MakeFieldLabel("Blackboard Variables"));
            var bbLabel = new Label(asset.Blackboard.Variables.Count.ToString());
            bbLabel.style.fontSize = 11;
            bbLabel.style.color    = new StyleColor(ColText);
            section.Add(bbLabel);

            m_Content.Add(section);
        }

        // ── Node view ─────────────────────────────────────────────────────────

        private void ShowNodeView(NodeData node, DialogueGraphAsset asset)
        {
            m_Content.Clear();

            var info = NodeRegistry.Get(node.NodeType);

            // ── Node header card ──────────────────────────────────────────────
            var headerCard = new VisualElement();
            headerCard.style.backgroundColor = info != null
                ? new StyleColor(new Color(info.AccentColor.r, info.AccentColor.g, info.AccentColor.b, 0.10f))
                : new StyleColor(new Color(0.16f, 0.18f, 0.24f));
            headerCard.style.borderBottomWidth = 1;
            headerCard.style.borderBottomColor = new StyleColor(ColDivider);
            headerCard.style.paddingLeft   = 12;
            headerCard.style.paddingRight  = 12;
            headerCard.style.paddingTop    = 12;
            headerCard.style.paddingBottom = 12;

            // Icon + display name row.
            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems    = Align.Center;

            var iconLabel = new Label(info?.Icon ?? "●");
            iconLabel.style.fontSize   = 16;
            iconLabel.style.color      = info != null
                ? new StyleColor(info.AccentColor)
                : new StyleColor(Color.gray);
            iconLabel.style.marginRight = 8;
            titleRow.Add(iconLabel);

            var nodeNameLabel = new Label(node.DisplayName);
            nodeNameLabel.style.fontSize    = 14;
            nodeNameLabel.style.color       = new StyleColor(ColText);
            nodeNameLabel.style.flexGrow    = 1;
            titleRow.Add(nodeNameLabel);
            headerCard.Add(titleRow);

            // Type badge.
            var badge = new Label((info?.Category ?? "") + "  /  " + (info?.DisplayName ?? node.NodeType));
            badge.style.fontSize   = 9;
            badge.style.color      = new StyleColor(ColMuted);
            badge.style.marginTop  = 5;
            headerCard.Add(badge);

            // Description.
            if (info != null && !string.IsNullOrEmpty(info.Description))
            {
                var desc = new Label(info.Description);
                desc.style.fontSize    = 10;
                desc.style.color       = new StyleColor(new Color(0.60f, 0.65f, 0.75f));
                desc.style.marginTop   = 6;
                desc.style.whiteSpace  = WhiteSpace.Normal;
                headerCard.Add(desc);
            }

            m_Content.Add(headerCard);

            // ── Display Name edit ─────────────────────────────────────────────
            var nameSection = MakeSection("Node");

            nameSection.Add(MakeFieldLabel("Display Name"));
            var nameField = new TextField { value = node.DisplayName };
            StyleTextField(nameField);
            nameField.RegisterValueChangedCallback(evt =>
            {
                node.DisplayName = evt.newValue;
                nodeNameLabel.text = evt.newValue;
                m_GraphView?.RefreshNodeView(node.Guid);
                if (asset) EditorUtility.SetDirty(asset);
            });
            nameSection.Add(nameField);

            // GUID (read-only).
            nameSection.Add(MakeFieldLabel("GUID"));
            var guidField = new TextField { value = node.Guid, isReadOnly = true };
            guidField.style.fontSize = 8;
            StyleTextField(guidField);
            nameSection.Add(guidField);

            m_Content.Add(nameSection);

            // ── Fields section ────────────────────────────────────────────────
            if (node.Fields != null && node.Fields.Count > 0)
            {
                var fieldsSection = MakeSection("Fields");
                foreach (var field in node.Fields)
                    fieldsSection.Add(BuildFieldEditor(field, asset));
                m_Content.Add(fieldsSection);
            }

            // ── Ports section (read-only overview) ────────────────────────────
            if (node.Ports != null && node.Ports.Count > 0)
            {
                var portsSection = MakeSection("Ports");
                foreach (var port in node.Ports)
                {
                    var portRow = new VisualElement();
                    portRow.style.flexDirection = FlexDirection.Row;
                    portRow.style.marginTop     = 3;

                    var dirIcon = port.Direction == PortDirection.Input ? "↓" : "↑";
                    var dirColor = port.Direction == PortDirection.Input
                        ? new Color(0.29f, 0.61f, 0.78f)
                        : new Color(0.34f, 0.78f, 0.34f);

                    var dirLabel = new Label(dirIcon);
                    dirLabel.style.fontSize = 10;
                    dirLabel.style.color    = new StyleColor(dirColor);
                    dirLabel.style.width    = 16;
                    portRow.Add(dirLabel);

                    var portLabel = new Label(port.PortName);
                    portLabel.style.fontSize = 10;
                    portLabel.style.color    = new StyleColor(ColText);
                    portLabel.style.flexGrow = 1;
                    portRow.Add(portLabel);

                    var capLabel = new Label(port.Capacity.ToString());
                    capLabel.style.fontSize = 9;
                    capLabel.style.color    = new StyleColor(ColMuted);
                    portRow.Add(capLabel);

                    portsSection.Add(portRow);
                }
                m_Content.Add(portsSection);
            }
        }

        // ── Field editor with BB link button ──────────────────────────────────

        private VisualElement BuildFieldEditor(FieldData field, DialogueGraphAsset asset)
        {
            var container = new VisualElement();
            container.style.marginTop     = 8;
            container.style.marginBottom  = 2;

            // Field name + type row.
            var labelRow = new VisualElement();
            labelRow.style.flexDirection = FlexDirection.Row;
            labelRow.style.alignItems    = Align.Center;

            var fieldName = new Label(field.FieldName);
            fieldName.style.fontSize = 10;
            fieldName.style.color    = new StyleColor(ColMuted);
            fieldName.style.flexGrow = 1;
            labelRow.Add(fieldName);

            var typePill = new Label(ShortType(field.TypeName));
            typePill.style.fontSize        = 8;
            typePill.style.color           = new StyleColor(new Color(0.50f, 0.55f, 0.65f));
            typePill.style.backgroundColor = new StyleColor(new Color(0.15f, 0.17f, 0.22f));
            typePill.style.paddingLeft     = 4;
            typePill.style.paddingRight    = 4;
            typePill.style.borderTopLeftRadius = typePill.style.borderTopRightRadius =
            typePill.style.borderBottomLeftRadius = typePill.style.borderBottomRightRadius = 3;
            labelRow.Add(typePill);

            container.Add(labelRow);

            // Value or linked-variable row.
            var valueRow = new VisualElement();
            valueRow.style.flexDirection = FlexDirection.Row;
            valueRow.style.alignItems    = Align.Center;
            valueRow.style.marginTop     = 3;

            bool linked = !string.IsNullOrEmpty(field.LinkedVariableGuid);

            if (linked)
            {
                // Show the name of the linked Blackboard variable.
                var bbVar = asset?.Blackboard.GetVariable(field.LinkedVariableGuid);
                var linkLabel = new Label("⟵  " + (bbVar?.Name ?? "?  (missing)"));
                linkLabel.style.fontSize        = 10;
                linkLabel.style.color           = new StyleColor(new Color(0.29f, 0.61f, 0.78f));
                linkLabel.style.backgroundColor = new StyleColor(new Color(0.10f, 0.18f, 0.28f));
                linkLabel.style.borderTopLeftRadius = linkLabel.style.borderTopRightRadius =
                linkLabel.style.borderBottomLeftRadius = linkLabel.style.borderBottomRightRadius = 4;
                linkLabel.style.paddingLeft    = 6;
                linkLabel.style.paddingRight   = 6;
                linkLabel.style.paddingTop     = 3;
                linkLabel.style.paddingBottom  = 3;
                linkLabel.style.flexGrow       = 1;
                valueRow.Add(linkLabel);
            }
            else
            {
                // Inline text field.
                var valueField = new TextField { value = field.InlineValue ?? "" };
                StyleTextField(valueField);
                valueField.style.flexGrow = 1;
                valueField.RegisterValueChangedCallback(evt =>
                {
                    field.InlineValue = evt.newValue;
                    if (asset) EditorUtility.SetDirty(asset);
                    m_GraphView?.RefreshNodeView(m_Node?.Guid ?? "");
                });
                valueRow.Add(valueField);
            }

            // Link / Unlink button.
            var linkBtn = new Button(() => ShowLinkMenu(field, asset, valueRow)) { text = linked ? "◉" : "◯" };
            linkBtn.tooltip = linked ? "Unlink from Blackboard variable" : "Link to a Blackboard variable";
            linkBtn.style.fontSize   = 11;
            linkBtn.style.color      = linked
                ? new StyleColor(new Color(0.29f, 0.61f, 0.78f))
                : new StyleColor(new Color(0.35f, 0.40f, 0.52f));
            linkBtn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.14f, 0.20f));
            linkBtn.style.marginLeft      = 4;
            linkBtn.style.paddingLeft = linkBtn.style.paddingRight = 5;
            linkBtn.style.paddingTop  = linkBtn.style.paddingBottom = 2;
            linkBtn.style.borderTopWidth = linkBtn.style.borderBottomWidth =
            linkBtn.style.borderLeftWidth = linkBtn.style.borderRightWidth = 1;
            linkBtn.style.borderTopColor = linkBtn.style.borderBottomColor =
            linkBtn.style.borderLeftColor = linkBtn.style.borderRightColor =
                new StyleColor(new Color(0.22f, 0.26f, 0.36f));
            linkBtn.style.borderTopLeftRadius = linkBtn.style.borderTopRightRadius =
            linkBtn.style.borderBottomLeftRadius = linkBtn.style.borderBottomRightRadius = 4;
            valueRow.Add(linkBtn);

            container.Add(valueRow);
            return container;
        }

        // ── BB link popup ─────────────────────────────────────────────────────

        private void ShowLinkMenu(FieldData field, DialogueGraphAsset asset, VisualElement valueRow)
        {
            if (asset == null) return;

            // Already linked → unlink immediately.
            if (!string.IsNullOrEmpty(field.LinkedVariableGuid))
            {
                Undo.RecordObject(asset, "Unlink Field");
                field.LinkedVariableGuid = null;
                EditorUtility.SetDirty(asset);
                // Refresh the inspector to reflect the change.
                ShowNodeView(m_Node, asset);
                return;
            }

            // Build a GenericMenu of Blackboard variables.
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
                        m_GraphView?.RefreshNodeView(m_Node?.Guid ?? "");
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
            section.style.marginTop          = 0;
            section.style.paddingLeft        = 12;
            section.style.paddingRight       = 12;
            section.style.paddingTop         = 10;
            section.style.paddingBottom      = 12;
            section.style.borderBottomWidth  = 1;
            section.style.borderBottomColor  = new StyleColor(new Color(0.10f, 0.11f, 0.15f));

            var titleLabel = new Label(title.ToUpper());
            titleLabel.style.fontSize    = 9;
            titleLabel.style.color       = new StyleColor(new Color(0.42f, 0.48f, 0.60f));
            titleLabel.style.letterSpacing = 1.2f;
            titleLabel.style.marginBottom = 6;
            section.Add(titleLabel);

            return section;
        }

        private static Label MakeFieldLabel(string text)
        {
            var l = new Label(text);
            l.style.fontSize    = 9;
            l.style.color       = new StyleColor(new Color(0.50f, 0.55f, 0.65f));
            l.style.marginTop   = 7;
            l.style.marginBottom = 3;
            return l;
        }

        private static void StyleTextField(TextField tf)
        {
            tf.style.fontSize        = 10;
            tf.style.color           = new StyleColor(new Color(0.82f, 0.86f, 0.95f));
            tf.style.backgroundColor = new StyleColor(new Color(0.10f, 0.11f, 0.16f));
            tf.style.borderTopWidth = tf.style.borderBottomWidth =
            tf.style.borderLeftWidth = tf.style.borderRightWidth = 1;
            tf.style.borderTopColor = tf.style.borderBottomColor =
            tf.style.borderLeftColor = tf.style.borderRightColor =
                new StyleColor(new Color(0.20f, 0.22f, 0.30f));
            tf.style.borderTopLeftRadius = tf.style.borderTopRightRadius =
            tf.style.borderBottomLeftRadius = tf.style.borderBottomRightRadius = 3;
            tf.style.paddingLeft   = 5;
            tf.style.paddingRight  = 5;
            tf.style.paddingTop    = 3;
            tf.style.paddingBottom = 3;
        }

        private static string ShortType(string fullTypeName)
        {
            if (string.IsNullOrEmpty(fullTypeName)) return "?";
            var parts = fullTypeName.Split('.');
            return parts[parts.Length - 1];
        }
    }
}
