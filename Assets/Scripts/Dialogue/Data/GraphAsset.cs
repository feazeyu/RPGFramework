using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueGraph.Runtime
{
    // ── Port / Field / Node / Edge ──────────────────────────────────────────────
    // These data types were previously declared in DialogueGraphAsset.cs.
    // They live here now so the Quest system (and anything else built on
    // GraphAsset) can reference them without depending on the dialogue asset.

    public enum PortDirection { Input, Output }
    public enum PortCapacity  { Single, Multi }

    [Serializable]
    public class PortData
    {
        public string        PortName;
        public PortDirection Direction;
        public PortCapacity  Capacity = PortCapacity.Multi;
    }

    [Serializable]
    public class FieldData
    {
        public string FieldName;
        public string TypeName;           // e.g. "System.String", "UnityEngine.GameObject"
        public string InlineValue;        // serialised as string when not linked
        public string LinkedVariableGuid; // GUID of a BlackboardVariable, or empty
    }

    [Serializable]
    public class NodeData
    {
        public string          Guid;
        public string          NodeType;           // e.g. "DialogueLine", "Objective"
        public string          DisplayName;
        public string          StoryText;          // human-readable template
        public Vector2         Position;
        public Vector2         Size   = Vector2.zero; // zero = auto (not yet user-resized)
        public List<PortData>  Ports  = new List<PortData>();
        public List<FieldData> Fields = new List<FieldData>();
    }

    [Serializable]
    public class EdgeData
    {
        public string Guid;
        public string OutputNodeGuid;
        public string OutputPortName;
        public string InputNodeGuid;
        public string InputPortName;
    }

    // ── Graph Asset base ────────────────────────────────────────────────────────

    /// <summary>
    /// Common serialised data and CRUD for every graph-based system
    /// (Dialogue, Quest, and any future graph editor built on top of
    /// <see cref="DialogueGraph.Editor"/>'s window pipeline).
    ///
    /// Concrete subclasses (<see cref="DialogueGraphAsset"/>,
    /// <see cref="QuestGraphAsset"/>) exist only to carry a
    /// <c>[CreateAssetMenu]</c> attribute so each system gets its own
    /// entry under <b>Assets → Create</b>.
    ///
    /// Serialisation layout — <b>do not change</b> without a migration step.
    /// <see cref="DialogueGraph.Editor.BlackboardPropertyBridge"/> walks
    /// <c>m_Blackboard.m_Variables</c> via absolute SerializedProperty paths;
    /// any rename here silently breaks the inspector's bound fields.
    /// </summary>
    public abstract class GraphAsset : ScriptableObject
    {
        [SerializeField] private   List<NodeData> m_Nodes = new List<NodeData>();
        [SerializeField] private   List<EdgeData> m_Edges = new List<EdgeData>();
        [SerializeField] private   Blackboard     m_Blackboard = new Blackboard();

        // Editor-only view state (panning / zoom). Public fields so the editor
        // can write to them directly via SerializedObject-free paths.
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
