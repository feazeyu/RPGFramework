using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueGraph.Runtime
{
    // ── Port ────────────────────────────────────────────────────────────────────

    public enum PortDirection { Input, Output }
    public enum PortCapacity   { Single, Multi }

    [Serializable]
    public class PortData
    {
        public string        PortName;
        public PortDirection Direction;
        public PortCapacity  Capacity = PortCapacity.Multi;
    }

    // ── Field (node variable slot) ──────────────────────────────────────────────

    [Serializable]
    public class FieldData
    {
        public string FieldName;
        public string TypeName;        // e.g. "System.String", "UnityEngine.GameObject"
        public string InlineValue;     // serialised as string when not linked
        public string LinkedVariableGuid; // GUID of BlackboardVariable, or empty
    }

    // ── Node ────────────────────────────────────────────────────────────────────

    [Serializable]
    public class NodeData
    {
        public string          Guid;
        public string          NodeType;      // e.g. "DialogueLine", "ChoiceBranch"
        public string          DisplayName;
        public string          StoryText;     // human-readable "story" template
        public Vector2         Position;
        public Vector2         Size = Vector2.zero; // zero = auto (not yet user-resized)
        public List<PortData>  Ports  = new List<PortData>();
        public List<FieldData> Fields = new List<FieldData>();
    }

    // ── Edge ────────────────────────────────────────────────────────────────────

    [Serializable]
    public class EdgeData
    {
        public string Guid;
        public string OutputNodeGuid;
        public string OutputPortName;
        public string InputNodeGuid;
        public string InputPortName;
    }

    // ── Graph Asset ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The serialised ScriptableObject asset that stores the entire graph.
    /// Create via  Assets → Create → Dialogue → Dialogue Graph.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Dialogue/Dialogue Graph",
        fileName = "NewDialogueGraph",
        order    = 1)]
    public class DialogueGraphAsset : ScriptableObject
    {
        [SerializeField] private List<NodeData> m_Nodes = new List<NodeData>();
        [SerializeField] private List<EdgeData> m_Edges = new List<EdgeData>();
        [SerializeField] private Blackboard     m_Blackboard = new Blackboard();

        // Editor-only view state (panning / zoom).
        [SerializeField] public Vector3 ViewTransform      = Vector3.zero; // xy = pan, z = scale
        [SerializeField] public Vector2 BlackboardPosition = new Vector2(10, 60);

        public IReadOnlyList<NodeData> Nodes      => m_Nodes;
        public IReadOnlyList<EdgeData> Edges      => m_Edges;
        public Blackboard              Blackboard => m_Blackboard;

        // ── Node CRUD ───────────────────────────────────────────────────────

        public NodeData AddNode(string nodeType, string displayName, Vector2 position)
        {
            var node = new NodeData
            {
                Guid        = System.Guid.NewGuid().ToString(),
                NodeType    = nodeType,
                DisplayName = displayName,
                Position    = position,
            };
            m_Nodes.Add(node);
            return node;
        }

        public bool RemoveNode(string guid)
        {
            int idx = m_Nodes.FindIndex(n => n.Guid == guid);
            if (idx < 0) return false;
            m_Nodes.RemoveAt(idx);
            // Clean up all connected edges.
            m_Edges.RemoveAll(e => e.OutputNodeGuid == guid || e.InputNodeGuid == guid);
            return true;
        }

        public NodeData GetNode(string guid) => m_Nodes.Find(n => n.Guid == guid);

        // ── Edge CRUD ───────────────────────────────────────────────────────

        public EdgeData AddEdge(string outputGuid, string outputPort,
                                string inputGuid,  string inputPort)
        {
            var edge = new EdgeData
            {
                Guid           = System.Guid.NewGuid().ToString(),
                OutputNodeGuid = outputGuid,
                OutputPortName = outputPort,
                InputNodeGuid  = inputGuid,
                InputPortName  = inputPort,
            };
            m_Edges.Add(edge);
            return edge;
        }

        public bool RemoveEdge(string guid)
        {
            int idx = m_Edges.FindIndex(e => e.Guid == guid);
            if (idx < 0) return false;
            m_Edges.RemoveAt(idx);
            return true;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Finds the entry / Start node (first node with no incoming edges).
        /// </summary>
        public NodeData FindEntryNode()
        {
            var hasIncoming = new HashSet<string>();
            foreach (var e in m_Edges) hasIncoming.Add(e.InputNodeGuid);
            return m_Nodes.Find(n => !hasIncoming.Contains(n.Guid));
        }
    }
}
