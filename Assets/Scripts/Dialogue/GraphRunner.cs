using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DialogueGraph.Runtime
{
    /// <summary>
    /// Base MonoBehaviour that drives execution of a DialogueGraphAsset.
    ///
    /// Responsibilities:
    ///   • Clones the blackboard at start for isolated per-run state
    ///   • Walks the graph along edges
    ///   • Dispatches each node to a registered IGraphNodeHandler
    ///   • Handles structural built-in nodes: Start, End, Condition,
    ///     SetVariable, Sequence, Selector (no system-specific knowledge)
    ///
    /// To add a new graph-based system (quest, cutscene, …):
    ///   1. Subclass GraphRunner.
    ///   2. In Awake() call RegisterHandler() for each node type you own.
    ///   3. Alternatively, place GraphNodeBehaviour subclasses on the same
    ///      GameObject — they are discovered automatically.
    ///
    /// Node types without a registered handler are skipped with a warning,
    /// following the "Out" port if one exists.
    /// </summary>
    public class GraphRunner : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("The graph asset to execute.")]
        public DialogueGraphAsset Graph;

        [Header("Events")]
        public UnityEvent OnGraphStarted;
        public UnityEvent OnGraphEnded;

        // ── State ─────────────────────────────────────────────────────────────

        public bool IsRunning { get; private set; }

        protected Blackboard      m_RuntimeBlackboard;
        protected NodeData        m_CurrentNode;
        protected GraphRunContext m_Context;

        private readonly Dictionary<string, IGraphNodeHandler> m_Handlers
            = new Dictionary<string, IGraphNodeHandler>();

        // Prevents FollowOutputPort being called more than once per node step.
        private bool m_Advancing;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected virtual void Awake()
        {
            // Auto-discover GraphNodeBehaviour components on this GameObject
            // and its children so scene-object-backed nodes work without code.
            foreach (var h in GetComponentsInChildren<IGraphNodeHandler>())
                RegisterHandler(h);
        }

        // ── Handler registration ──────────────────────────────────────────────

        /// <summary>
        /// Registers a handler for a node type.  Call this in Awake() before
        /// StartGraph() is invoked.  Later registrations overwrite earlier ones.
        /// </summary>
        public void RegisterHandler(IGraphNodeHandler handler)
        {
            if (handler == null || string.IsNullOrEmpty(handler.NodeTypeId)) return;
            m_Handlers[handler.NodeTypeId] = handler;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void StartGraph()
        {
            if (Graph == null)
            {
                Debug.LogWarning($"[GraphRunner] No graph assigned on '{name}'.", this);
                return;
            }
            if (IsRunning)
            {
                Debug.LogWarning($"[GraphRunner] Already running on '{name}'.", this);
                return;
            }

            m_RuntimeBlackboard = Graph.Blackboard.Clone(Graph.Blackboard);

            m_Context           = new GraphRunContext(this, Graph, m_RuntimeBlackboard);
            m_Context.OnFollow  = portName => FollowOutputPort(m_CurrentNode, portName);
            m_Context.OnEnd     = EndGraph;

            IsRunning   = true;
            m_Advancing = false;

            OnGraphStarted?.Invoke();
            OnGraphStart();

            var startNode = Graph.FindEntryNode();
            if (startNode == null)
            {
                Debug.LogWarning($"[GraphRunner] Graph '{Graph.name}' has no entry node.", this);
                EndGraph();
                return;
            }

            AdvanceToNode(startNode);
        }

        public void StopGraph()
        {
            if (!IsRunning) return;
            StopAllCoroutines();
            EndGraph();
        }

        // ── Traversal ─────────────────────────────────────────────────────────

        protected void AdvanceToNode(NodeData node)
        {
            m_CurrentNode = node;
            m_Advancing   = false;
            StartCoroutine(ProcessNode(node));
        }

        protected void FollowOutputPort(NodeData node, string portName)
        {
            if (m_Advancing) return;   // guard against double-advance
            m_Advancing = true;

            var next = GetNodeConnectedToOutput(node, portName);
            if (next != null)
                AdvanceToNode(next);
            else
                EndGraph();
        }

        private IEnumerator ProcessNode(NodeData node)
        {
            // ── Built-in structural nodes ─────────────────────────────────────
            switch (node.NodeType)
            {
                case NodeRegistry.TypeStart:
                    yield return null;
                    FollowOutputPort(node, "Out");
                    yield break;

                case NodeRegistry.TypeEnd:
                    EndGraph();
                    yield break;

                case NodeRegistry.TypeCondition:
                    ProcessCondition(node);
                    yield break;

                case NodeRegistry.TypeSetVariable:
                    ProcessSetVariable(node);
                    FollowOutputPort(node, "Out");
                    yield break;

                case NodeRegistry.TypeSequence:
                case NodeRegistry.TypeSelector:
                    FollowOutputPort(node, "Out");
                    yield break;

                case NodeRegistry.TypeRunSubgraph:
                    yield return ProcessRunSubgraph(node);
                    yield break;
            }

            // ── Registered handlers ───────────────────────────────────────────
            if (m_Handlers.TryGetValue(node.NodeType, out var handler))
            {
                yield return handler.Execute(node, m_Context);
                yield break;
            }

            // ── Unknown node — warn and skip forward ──────────────────────────
            Debug.LogWarning($"[GraphRunner] No handler registered for node type '{node.NodeType}'. Skipping.");
            FollowOutputPort(node, "Out");
        }

        // ── Built-in node logic ───────────────────────────────────────────────

        private void ProcessCondition(NodeData node)
        {
            var variableGuid = m_Context.GetLinkedGuid(node, "Variable");
            bool result      = false;

            if (!string.IsNullOrEmpty(variableGuid))
            {
                var bbVar = m_RuntimeBlackboard.GetVariable(variableGuid);
                if (bbVar != null)
                {
                    var op  = m_Context.ResolveString(node, "Operator");
                    var val = m_Context.ResolveString(node, "Value");
                    result  = EvaluateCondition(bbVar.ObjectValue, op, val);
                }
            }

            FollowOutputPort(node, result ? "True" : "False");
        }

        private void ProcessSetVariable(NodeData node)
        {
            var variableGuid = m_Context.GetLinkedGuid(node, "Variable");
            if (string.IsNullOrEmpty(variableGuid)) return;

            var bbVar = m_RuntimeBlackboard.GetVariable(variableGuid);
            if (bbVar == null) return;

            var valueStr = m_Context.ResolveString(node, "Value");

            try
            {
                bbVar.ObjectValue = Convert.ChangeType(valueStr, bbVar.ValueType);
                OnVariableChanged(bbVar.Name, valueStr);
            }
            catch
            {
                Debug.LogWarning($"[GraphRunner] Could not convert '{valueStr}' to {bbVar.ValueType} for variable '{bbVar.Name}'.");
            }
        }

        private IEnumerator ProcessRunSubgraph(NodeData node)
        {
            var subAsset = ResolveSubgraphAsset(node);
            if (subAsset == null)
            {
                Debug.LogWarning("[GraphRunner] RunSubgraph: could not resolve a graph asset. Skipping.");
                FollowOutputPort(node, "Out");
                yield break;
            }

            var subGO     = new GameObject($"Subgraph:{subAsset.name}");
            var subRunner = CreateSubRunner(subGO);
            subRunner.Graph = subAsset;

            // Forward any handlers the parent has to the child.
            foreach (var kvp in m_Handlers)
                subRunner.RegisterHandler(kvp.Value);

            bool done = false;
            subRunner.OnGraphEnded.AddListener(() => done = true);
            subRunner.StartGraph();

            yield return new WaitUntil(() => done);

            Destroy(subGO);
            FollowOutputPort(node, "Out");
        }

        /// <summary>
        /// Override to return the subgraph asset for a RunSubgraph node.
        /// Base implementation always returns null (no inline asset references yet).
        /// </summary>
        protected virtual DialogueGraphAsset ResolveSubgraphAsset(NodeData node) => null;

        /// <summary>
        /// Factory for the sub-runner used by RunSubgraph nodes.
        /// Override in subclasses to return a typed subclass (e.g. DialogueRunner).
        /// </summary>
        protected virtual GraphRunner CreateSubRunner(GameObject go)
            => go.AddComponent<GraphRunner>();

        // ── Virtual hooks for subclasses ──────────────────────────────────────

        /// <summary>Called after the graph starts and the first node is about to execute.</summary>
        protected virtual void OnGraphStart() { }

        /// <summary>Called after the graph ends.</summary>
        protected virtual void OnGraphStop() { }

        /// <summary>
        /// Called when a SetVariable node successfully changes a variable.
        /// Override to fire UI events or drive external systems.
        /// </summary>
        protected virtual void OnVariableChanged(string variableName, string newValueString) { }

        // ── End ───────────────────────────────────────────────────────────────

        protected void EndGraph()
        {
            if (!IsRunning) return;
            IsRunning     = false;
            m_Advancing   = false;
            m_CurrentNode = null;
            OnGraphStop();
            OnGraphEnded?.Invoke();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        protected NodeData GetNodeConnectedToOutput(NodeData node, string portName)
        {
            foreach (var edge in Graph.Edges)
                if (edge.OutputNodeGuid == node.Guid && edge.OutputPortName == portName)
                    return Graph.GetNode(edge.InputNodeGuid);
            return null;
        }

        private static bool EvaluateCondition(object lhs, string op, string rhsStr)
        {
            if (lhs == null) return false;

            if (double.TryParse(lhs.ToString(), out double lhsN) &&
                double.TryParse(rhsStr,          out double rhsN))
            {
                return op switch
                {
                    "==" => lhsN == rhsN,
                    "!=" => lhsN != rhsN,
                    ">"  => lhsN >  rhsN,
                    ">=" => lhsN >= rhsN,
                    "<"  => lhsN <  rhsN,
                    "<=" => lhsN <= rhsN,
                    _    => false,
                };
            }

            if (lhs is bool lhsB && bool.TryParse(rhsStr, out bool rhsB))
            {
                return op switch
                {
                    "==" => lhsB == rhsB,
                    "!=" => lhsB != rhsB,
                    _    => false,
                };
            }

            return op switch
            {
                "==" => string.Equals(lhs.ToString(), rhsStr, StringComparison.Ordinal),
                "!=" => !string.Equals(lhs.ToString(), rhsStr, StringComparison.Ordinal),
                _    => false,
            };
        }
    }
}
