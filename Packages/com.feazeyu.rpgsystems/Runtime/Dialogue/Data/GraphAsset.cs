using System;
using System.Collections.Generic;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{

    /// <summary>Whether a port receives (Input) or emits (Output) edges.</summary>
    public enum PortDirection { Input, Output }

    /// <summary>Whether a port accepts a single edge or many.</summary>
    public enum PortCapacity  { Single, Multi }

    /// <summary>Serialized definition of one node port.</summary>
    [Serializable]
    public class PortData
    {
        /// <summary>Port identifier, unique within the node.</summary>
        public string        PortName;
        /// <summary>Whether the port is an input or an output.</summary>
        public PortDirection Direction;
        /// <summary>How many edges the port may hold.</summary>
        public PortCapacity  Capacity = PortCapacity.Multi;
    }

    /// <summary>Serialized definition of one editable node field, either an inline value or a blackboard link.</summary>
    [Serializable]
    public class FieldData
    {
        /// <summary>Field identifier, unique within the node.</summary>
        public string FieldName;
        /// <summary>Fully qualified value type, e.g. "System.String", "UnityEngine.GameObject".</summary>
        public string TypeName;
        /// <summary>Literal value, serialized as a string, used when not linked.</summary>
        public string InlineValue;
        /// <summary>GUID of a linked <see cref="BlackboardVariable"/>, or empty for an inline value.</summary>
        public string LinkedVariableGuid;
    }

    /// <summary>Serialized data for one graph node: identity, type, layout, ports, and fields.</summary>
    [Serializable]
    public class NodeData
    {
        /// <summary>Stable unique identifier for the node.</summary>
        public string          Guid;
        /// <summary>Node type id, e.g. "DialogueLine", "Objective".</summary>
        public string          NodeType;
        /// <summary>Display name shown on the node.</summary>
        public string          DisplayName;
        /// <summary>Human-readable template/body text.</summary>
        public string          StoryText;
        /// <summary>Editor canvas position.</summary>
        public Vector2         Position;
        /// <summary>Editor size; zero means auto (not user-resized).</summary>
        public Vector2         Size   = Vector2.zero;
        /// <summary>The node's ports.</summary>
        public List<PortData>  Ports  = new List<PortData>();
        /// <summary>The node's editable fields.</summary>
        public List<FieldData> Fields = new List<FieldData>();
    }

    /// <summary>Serialized connection between an output port and an input port.</summary>
    [Serializable]
    public class EdgeData
    {
        /// <summary>Stable unique identifier for the edge.</summary>
        public string Guid;
        /// <summary>GUID of the source node.</summary>
        public string OutputNodeGuid;
        /// <summary>Output port name on the source node.</summary>
        public string OutputPortName;
        /// <summary>GUID of the destination node.</summary>
        public string InputNodeGuid;
        /// <summary>Input port name on the destination node.</summary>
        public string InputPortName;
    }


    /// <summary>
    /// Common serialised data and CRUD for every graph-based system
    /// (Dialogue, Quest, and any future graph editor built on top of
    /// <see cref="Feazeyu.RPGSystems.Editor"/>'s window pipeline).
    ///
    /// Concrete subclasses (<see cref="DialogueGraphAsset"/>,
    /// <see cref="QuestGraphAsset"/>) exist only to carry a
    /// <c>[CreateAssetMenu]</c> attribute so each system gets its own
    /// entry under <b>Assets → Create</b>.
    ///
    /// Serialisation layout — <b>do not change</b> without a migration step.
    /// <see cref="Feazeyu.RPGSystems.EditorTools.BlackboardPropertyBridge"/> walks
    /// <c>m_Blackboard.m_Variables</c> via absolute SerializedProperty paths;
    /// any rename here silently breaks the inspector's bound fields.
    /// </summary>
    public abstract class GraphAsset : ScriptableObject
    {
        [SerializeField] private   List<NodeData> m_Nodes = new List<NodeData>();
        [SerializeField] private   List<EdgeData> m_Edges = new List<EdgeData>();
        [SerializeField] private   Blackboard     m_Blackboard = new Blackboard();

        [SerializeField] public Vector3 ViewTransform      = Vector3.zero;
        [SerializeField] public Vector2 BlackboardPosition = new Vector2(10, 60);

        /// <summary>Nodes.</summary>
        public IReadOnlyList<NodeData> Nodes      => m_Nodes;
        /// <summary>Edges.</summary>
        public IReadOnlyList<EdgeData> Edges      => m_Edges;
        /// <summary>Blackboard.</summary>
        public Blackboard              Blackboard => m_Blackboard;


        /// <summary>Add node.</summary>
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

        /// <summary>Remove node.</summary>
        public bool RemoveNode(string guid)
        {
            int idx = m_Nodes.FindIndex(n => n.Guid == guid);
            if (idx < 0) return false;
            m_Nodes.RemoveAt(idx);
            m_Edges.RemoveAll(e => e.OutputNodeGuid == guid || e.InputNodeGuid == guid);
            return true;
        }

        /// <summary>Get node.</summary>
        public NodeData GetNode(string guid) => m_Nodes.Find(n => n.Guid == guid);


        /// <summary>Add edge.</summary>
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

        /// <summary>Remove edge.</summary>
        public bool RemoveEdge(string guid)
        {
            int idx = m_Edges.FindIndex(e => e.Guid == guid);
            if (idx < 0) return false;
            m_Edges.RemoveAt(idx);
            return true;
        }


        /// <summary>
        /// Finds the entry / Start node. Prefers an explicit "Start"-typed node
        /// so nodes with no incoming edges (e.g. Requirement nodes) are not
        /// mistaken for the graph entry point.
        /// </summary>
        public NodeData FindEntryNode()
        {
            var startNode = m_Nodes.Find(n => n.NodeType == "Start");
            if (startNode != null) return startNode;

            var hasIncoming = new HashSet<string>();
            foreach (var e in m_Edges) hasIncoming.Add(e.InputNodeGuid);
            return m_Nodes.Find(n => !hasIncoming.Contains(n.Guid));
        }
    }
}
