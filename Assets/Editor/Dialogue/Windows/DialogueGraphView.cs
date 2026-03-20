using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using DialogueGraph.Runtime;

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Marker Edge subclass. Used so ConnectTo&lt;SoftEdge&gt; creates consistently
    /// typed edges; actual curve softness comes from Orientation.Vertical ports
    /// and the --edge-width USS variable rather than C# tangent manipulation
    /// (EdgeControl.controlPoints is read-only in current Unity versions).
    /// </summary>
    public class SoftEdge : Edge { }

    /// <summary>
    /// The main graph canvas. Extends Unity's GraphView so we get built-in:
    ///   • Node drag, multi-select, rubber-band selection
    ///   • Edge drawing (port-to-port)
    ///   • Pan (middle mouse / Alt+drag) and zoom (scroll wheel)
    ///   • Minimap support
    ///
    /// We override:
    ///   • BuildContextualMenu  → right-click "Add Node" menu
    ///   • GetCompatiblePorts   → port connection rules
    ///   • Populate / FlushViewState → asset serialisation
    /// </summary>
    public class DialogueGraphView : GraphView
    {
        // ── Events ────────────────────────────────────────────────────────────

        public Action<NodeData> OnNodeSelected;
        public Action           OnNodeDeselected;

        // Fired when a blackboard variable is dropped onto a node field.
        // Args: (fieldData, variableGuid)  — empty guid = unlink
        public Action<FieldData, string> OnVariableLinked;

        // ── Internal state ────────────────────────────────────────────────────

        private DialogueGraphAsset m_Asset;
        private DialogueGraphWindow m_Window;

        // Maps NodeData.Guid → the GraphView NodeView element.
        private readonly Dictionary<string, DialogueNodeView> m_NodeViews
            = new Dictionary<string, DialogueNodeView>();

        // ── Construction ──────────────────────────────────────────────────────

        public DialogueGraphView(DialogueGraphWindow window)
        {
            m_Window = window;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            // Grid background.
            var grid = new GridBackground();
            grid.name = "GraphGrid";
            Insert(0, grid);

            // Style grid and canvas background to match Unity Behavior's dark theme.
            styleSheets.Add(DialogueGraphStyleSheet.Get());
            AddToClassList("dialogue-graph-view");

            // Minimap (top-right corner, like Unity Behavior).
            var minimap = new MiniMap { anchored = true };
            minimap.SetPosition(new Rect(10, 30, 160, 100));
            Add(minimap);

            // React to selection changes from the GraphView's internal system.
            graphViewChanged += OnGraphViewChanged;
        }

        // ── GraphView overrides ───────────────────────────────────────────────

        /// <summary>
        /// Use SoftEdge instead of the default Edge so bezier tangents are gentler.
        /// </summary>
        public Edge CreateEdge() => new SoftEdge();

        /// <summary>
        /// Controls which ports can connect to which.
        /// Rule: Output→Input, matching direction, no self-loops.
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter adapter)
        {
            var compatible = new List<Port>();
            ports.ForEach(port =>
            {
                if (port == startPort) return;
                if (port.node == startPort.node) return;
                if (port.direction == startPort.direction) return;
                compatible.Add(port);
            });
            return compatible;
        }

        /// <summary>
        /// Right-click → contextual menu with grouped "Add Node" entries,
        /// plus Cut / Copy / Paste / Delete for selected elements.
        /// </summary>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // ── "Add Node" section ────────────────────────────────────────────
            var mousePos = contentViewContainer.WorldToLocal(evt.mousePosition);

            // Group by Category (mirrors Unity Behavior's category grouping).
            var byCategory = new Dictionary<string, List<DialogueNodeInfo>>();
            foreach (var kv in NodeRegistry.All)
            {
                var cat = kv.Value.Category;
                if (!byCategory.ContainsKey(cat))
                    byCategory[cat] = new List<DialogueNodeInfo>();
                byCategory[cat].Add(kv.Value);
            }

            foreach (var cat in byCategory.Keys)
            {
                foreach (var info in byCategory[cat])
                {
                    // Capture for closure.
                    var captured = info;
                    evt.menu.AppendAction(
                        $"Add Node/{cat}/{captured.Icon} {captured.DisplayName}",
                        _ => AddNodeAtPosition(captured, mousePos),
                        DropdownMenuAction.AlwaysEnabled);
                }
            }

            evt.menu.AppendSeparator();

            // ── Standard edit actions ─────────────────────────────────────────
            if (selection.Count > 0)
            {
                evt.menu.AppendAction("Delete", _ => DeleteSelectedElements(),
                    DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Duplicate", _ => DuplicateSelectedNodes(),
                    DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }

            evt.menu.AppendAction("Frame All  [A]", _ => FrameAll(),
                DropdownMenuAction.AlwaysEnabled);
            evt.menu.AppendAction("Frame Selection  [F]", _ => FrameSelection(),
                selection.Count > 0
                    ? DropdownMenuAction.AlwaysEnabled
                    : DropdownMenuAction.AlwaysDisabled);
        }

        // ── Populate from asset ────────────────────────────────────────────────

        public void Populate(DialogueGraphAsset asset)
        {
            m_Asset = asset;

            // Clear existing UI.
            graphElements.ForEach(e => RemoveElement(e));
            m_NodeViews.Clear();

            // Restore view transform (pan + zoom saved on the asset).
            UpdateViewTransform(
                new Vector3(asset.ViewTransform.x, asset.ViewTransform.y, 0),
                Vector3.one * asset.ViewTransform.z);

            // Add node views.
            foreach (var nodeData in asset.Nodes)
                CreateNodeView(nodeData);

            // Add edges.
            foreach (var edgeData in asset.Edges)
                CreateEdgeView(edgeData);
        }

        /// <summary>Writes the current pan/zoom back to the asset before saving.</summary>
        public void FlushViewState()
        {
            if (m_Asset == null) return;
            var t = viewTransform;
            m_Asset.ViewTransform = new Vector3(t.position.x, t.position.y, t.scale.x);
        }

        // ── Node creation ─────────────────────────────────────────────────────

        private void AddNodeAtPosition(DialogueNodeInfo info, Vector2 graphPos)
        {
            if (m_Asset == null) return;

            Undo.RecordObject(m_Asset, $"Add {info.DisplayName} Node");
            var nodeData = m_Asset.AddNode(info.TypeId, info.DisplayName, graphPos);

            // Copy default ports and fields from the registry.
            nodeData.Ports.AddRange(info.DefaultPorts);
            nodeData.Fields.AddRange(info.DefaultFields);

            EditorUtility.SetDirty(m_Asset);
            CreateNodeView(nodeData);
        }

        private DialogueNodeView CreateNodeView(NodeData data)
        {
            var view = new DialogueNodeView(data, m_Asset);
            view.OnSelect  = () => OnNodeSelected?.Invoke(data);
            view.OnDeselected = () => OnNodeDeselected?.Invoke();
            view.OnMoved      = pos =>
            {
                if (m_Asset == null) return;
                Undo.RecordObject(m_Asset, "Move Node");
                data.Position = pos;
                EditorUtility.SetDirty(m_Asset);
            };

            AddElement(view);
            m_NodeViews[data.Guid] = view;
            return view;
        }

        // ── Edge creation ─────────────────────────────────────────────────────

        private void CreateEdgeView(EdgeData edgeData)
        {
            if (!m_NodeViews.TryGetValue(edgeData.OutputNodeGuid, out var outView)) return;
            if (!m_NodeViews.TryGetValue(edgeData.InputNodeGuid,  out var inView))  return;

            var outPort = outView.GetPort(edgeData.OutputPortName, Direction.Output);
            var inPort  = inView.GetPort(edgeData.InputPortName,   Direction.Input);
            if (outPort == null || inPort == null) return;

            var edge = outPort.ConnectTo<SoftEdge>(inPort);
            edge.userData = edgeData.Guid;
            AddElement(edge);
        }

        // ── GraphView change callback ─────────────────────────────────────────

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (m_Asset == null) return change;

            // ── Edges created ─────────────────────────────────────────────────
            if (change.edgesToCreate != null)
            {
                Undo.RecordObject(m_Asset, "Create Edge");
                foreach (var edge in change.edgesToCreate)
                {
                    var outNode = (edge.output.node as DialogueNodeView)?.Data;
                    var inNode  = (edge.input.node  as DialogueNodeView)?.Data;
                    if (outNode == null || inNode == null) continue;

                    var edgeData = m_Asset.AddEdge(
                        outNode.Guid, edge.output.portName,
                        inNode.Guid,  edge.input.portName);
                    edge.userData = edgeData.Guid;
                }
                EditorUtility.SetDirty(m_Asset);
            }

            // ── Elements removed ──────────────────────────────────────────────
            if (change.elementsToRemove != null)
            {
                Undo.RecordObject(m_Asset, "Remove Graph Elements");
                foreach (var elem in change.elementsToRemove)
                {
                    if (elem is DialogueNodeView nv)
                    {
                        m_Asset.RemoveNode(nv.Data.Guid);
                        m_NodeViews.Remove(nv.Data.Guid);
                    }
                    else if (elem is Edge e && e.userData is string edgeGuid)
                    {
                        m_Asset.RemoveEdge(edgeGuid);
                    }
                }
                EditorUtility.SetDirty(m_Asset);
            }

            // ── Nodes moved ───────────────────────────────────────────────────
            if (change.movedElements != null)
            {
                Undo.RecordObject(m_Asset, "Move Nodes");
                foreach (var elem in change.movedElements)
                {
                    if (elem is DialogueNodeView nv)
                        nv.Data.Position = nv.GetPosition().position;
                }
                EditorUtility.SetDirty(m_Asset);
            }

            return change;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void DeleteSelectedElements()
        {
            DeleteSelection();
        }

        private void DuplicateSelectedNodes()
        {
            if (m_Asset == null) return;
            Undo.RecordObject(m_Asset, "Duplicate Nodes");

            var offset = new Vector2(30, 30);
            var newViews = new List<DialogueNodeView>();

            foreach (var sel in selection)
            {
                if (sel is not DialogueNodeView nv) continue;
                var src = nv.Data;
                var dup = m_Asset.AddNode(src.NodeType, src.DisplayName, src.Position + offset);
                foreach (var port in src.Ports)
                    dup.Ports.Add(new PortData { PortName = port.PortName, Direction = port.Direction, Capacity = port.Capacity });
                foreach (var field in src.Fields)
                    dup.Fields.Add(new FieldData { FieldName = field.FieldName, TypeName = field.TypeName, InlineValue = field.InlineValue, LinkedVariableGuid = field.LinkedVariableGuid });
                newViews.Add(CreateNodeView(dup));
            }

            EditorUtility.SetDirty(m_Asset);

            // Select the new nodes.
            ClearSelection();
            foreach (var v in newViews) AddToSelection(v);
        }

        // Public accessor so InspectorPanel can request a node view refresh.
        public DialogueNodeView GetNodeView(string guid)
            => m_NodeViews.TryGetValue(guid, out var v) ? v : null;

        public void RefreshNodeView(string guid)
        {
            if (m_NodeViews.TryGetValue(guid, out var v))
                v.Refresh();
        }
    }
}
