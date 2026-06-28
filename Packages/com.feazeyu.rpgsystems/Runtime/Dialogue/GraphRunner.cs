using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Base MonoBehaviour that drives execution of a GraphAsset.
    ///
    /// Responsibilities:
    ///   • Builds the runtime blackboard once (reused across runs so non-shared
    ///     state persists; Shared variables are global via SharedBlackboardStore)
    ///   • Walks the graph along edges using independent <b>flow tokens</b>
    ///   • Dispatches each node to a registered IGraphNodeHandler
    ///   • Handles structural built-in nodes: Start, End, Condition,
    ///     SetVariable, RunSubgraph (no system-specific knowledge)
    ///
    /// ── Parallel flow ────────────────────────────────────────────────────
    /// Execution is carried by one or more <see cref="FlowToken"/>s, each an
    /// independent cursor with its own current node and context. An output
    /// port wired to several nodes <b>auto-forks</b>: one token per edge.
    /// A token ends when its node dead-ends or hits an End node; the graph
    /// ends when the last token finishes or a handler calls <c>ctx.End()</c>
    /// (the terminal path used by quest Complete/Fail nodes). Single-edge
    /// graphs (all existing dialogue/quest content) run as exactly one token,
    /// so behaviour is unchanged.
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

        /// <summary>Graph.</summary>
        [Tooltip("The graph asset to execute.")]
        public GraphAsset Graph;

        /// <summary>On graph started.</summary>
        [Header("Events")]
        public UnityEvent OnGraphStarted = new();
        /// <summary>On graph ended.</summary>
        public UnityEvent OnGraphEnded   = new();

        [SerializeReference, HideInInspector]
        private List<BlackboardVariable> m_Overrides = new List<BlackboardVariable>();


        /// <summary>Is running.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Runtime blackboard.</summary>
        protected Blackboard      m_RuntimeBlackboard;
        /// <summary>Context.</summary>
        protected GraphRunContext m_Context;

        private readonly Dictionary<string, IGraphNodeHandler> m_Handlers
            = new Dictionary<string, IGraphNodeHandler>();

        private int m_ActiveTokens;

        /// <summary>One independent cursor walking the graph.</summary>
        private sealed class FlowToken
        {
            /// <summary>Node.</summary>
            public NodeData        Node;
            /// <summary>Advanced.</summary>
            public bool            Advanced;
            /// <summary>Context.</summary>
            public GraphRunContext Context;
        }


        /// <summary>Awake.</summary>
        protected virtual void Awake()
        {
            foreach (var h in GetComponentsInChildren<IGraphNodeHandler>())
                RegisterHandler(h);
        }


        /// <summary>
        /// Registers a handler for a node type.  Call this in Awake() before
        /// StartGraph() is invoked.  Later registrations overwrite earlier ones.
        /// </summary>
        public void RegisterHandler(IGraphNodeHandler handler)
        {
            if (handler == null || string.IsNullOrEmpty(handler.NodeTypeId)) return;
            m_Handlers[handler.NodeTypeId] = handler;
        }


        /// <summary>Start graph.</summary>
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

            if (m_RuntimeBlackboard == null)
            {
                m_RuntimeBlackboard = Graph.Blackboard.CloneForRuntime();
                ApplyExposedOverrides();
            }

            m_Context = new GraphRunContext(this, Graph, m_RuntimeBlackboard);

            IsRunning      = true;
            m_ActiveTokens = 0;

            OnGraphStarted?.Invoke();
            OnGraphStart();

            var startNode = Graph.FindEntryNode();
            if (startNode == null)
            {
                Debug.LogWarning($"[GraphRunner] Graph '{Graph.name}' has no entry node.", this);
                EndGraph();
                return;
            }

            StartToken(startNode);
        }

        /// <summary>Stop graph.</summary>
        public void StopGraph()
        {
            if (!IsRunning) return;
            EndGraph();
        }

        /// <summary>
        /// Sets the value of a runtime blackboard variable by name. Used by scene
        /// components (e.g. ZoneFlag triggers) to feed world state into gates.
        /// No-op until the graph has started and built its runtime blackboard.
        /// </summary>
        public void SetBlackboardValue(string variableName, object value)
        {
            if (m_RuntimeBlackboard == null || string.IsNullOrEmpty(variableName)) return;
            foreach (var v in Graph.Blackboard.Variables)
            {
                if (v.Name != variableName) continue;
                var rt = m_RuntimeBlackboard.GetVariable(v.Guid);
                if (rt != null)
                {
                    try { rt.ObjectValue = value; }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[GraphRunner] SetBlackboardValue('{variableName}') failed: {e.Message}", this);
                    }
                }
                return;
            }
        }

        /// <summary>
        /// Applies this instance's per-runner overrides onto the freshly built
        /// runtime blackboard. Only Exposed, non-Shared variables are overridden:
        /// Shared variables resolve to a single global instance, so a per-instance
        /// override would stomp shared state. Stale entries (variable deleted,
        /// un-exposed, or made Shared after the override was authored) are skipped.
        /// </summary>
        private void ApplyExposedOverrides()
        {
            if (m_Overrides == null || m_RuntimeBlackboard == null) return;

            foreach (var ov in m_Overrides)
            {
                if (ov == null || string.IsNullOrEmpty(ov.Guid)) continue;

                var authored = Graph.Blackboard.GetVariable(ov.Guid);
                if (authored == null || !authored.Exposed || authored.Shared) continue;

                var target = m_RuntimeBlackboard.GetVariable(ov.Guid);
                if (target == null) continue;

                try { target.ObjectValue = ov.ObjectValue; }
                catch (Exception e)
                {
                    Debug.LogWarning(
                        $"[GraphRunner] Failed to apply exposed override for '{ov.Name}': {e.Message}", this);
                }
            }
        }


        /// <summary>Spawns and starts a new flow token at the given node.</summary>
        private void StartToken(NodeData node)
        {
            if (!IsRunning || node == null) return;

            var token = new FlowToken { Node = node };
            token.Context = new GraphRunContext(this, Graph, m_RuntimeBlackboard)
            {
                OnFollow = port => FollowPort(token, port),
                OnFork   = port => ForkPort(token, port),
                OnEnd    = EndGraph,
            };

            m_ActiveTokens++;
            StartCoroutine(RunToken(token));
        }

        private IEnumerator RunToken(FlowToken token)
        {
            yield return ProcessNode(token);

            m_ActiveTokens--;
            if (IsRunning && m_ActiveTokens <= 0)
                EndGraph();
        }

        /// <summary>Advance a token along a port (one-shot; auto-forks on multi-edge).</summary>
        private void FollowPort(FlowToken token, string portName)
        {
            if (token.Advanced) return;
            token.Advanced = true;
            SpawnSuccessors(token.Node, portName);
        }

        /// <summary>
        /// Spawn token(s) down a port without consuming the calling token. The
        /// caller keeps running (used by the auto-restarting Timer node).
        /// </summary>
        private void ForkPort(FlowToken token, string portName)
        {
            SpawnSuccessors(token.Node, portName);
        }

        /// <summary>Starts one token per edge leaving (node, portName).</summary>
        private void SpawnSuccessors(NodeData node, string portName)
        {
            if (!IsRunning) return;
            foreach (var edge in Graph.Edges)
            {
                if (edge.OutputNodeGuid != node.Guid || edge.OutputPortName != portName) continue;
                var next = Graph.GetNode(edge.InputNodeGuid);
                if (next != null) StartToken(next);
            }
        }

        private IEnumerator ProcessNode(FlowToken token)
        {
            var node = token.Node;

            switch (node.NodeType)
            {
                case NodeRegistry.TypeStart:
                    yield return null;
                    FollowPort(token, "Out");
                    yield break;

                case NodeRegistry.TypeEnd:
                    yield break;

                case NodeRegistry.TypeCondition:
                    ProcessCondition(token);
                    yield break;

                case NodeRegistry.TypeSetVariable:
                    ProcessSetVariable(node);
                    FollowPort(token, "Out");
                    yield break;

                case NodeRegistry.TypeRunSubgraph:
                    yield return ProcessRunSubgraph(token);
                    yield break;
            }

            if (m_Handlers.TryGetValue(node.NodeType, out var handler))
            {
                yield return handler.Execute(node, token.Context);
                yield break;
            }

            Debug.LogWarning($"[GraphRunner] No handler registered for node type '{node.NodeType}'. Skipping.");
            FollowPort(token, "Out");
        }


        private void ProcessCondition(FlowToken token)
        {
            var node         = token.Node;
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

            FollowPort(token, result ? "True" : "False");
        }

        private void ProcessSetVariable(NodeData node)
        {
            var variableGuid = m_Context.GetLinkedGuid(node, "Variable");
            if (string.IsNullOrEmpty(variableGuid)) {
                Debug.LogWarning($"[GraphRunner] Tried to set a null/empty variable");
                return;
            }

            var bbVar = m_RuntimeBlackboard.GetVariable(variableGuid);
            if (bbVar == null) {
                Debug.LogWarning($"[GraphRunner] Variable with guid: {variableGuid} was not found, and nothing was set.");
                return;
            }

            var valueStr = m_Context.ResolveString(node, "Value");

            try
            {
                bbVar.ObjectValue = ParseValueForType(valueStr, bbVar.ValueType);
                OnVariableChanged(bbVar.Name, valueStr);
            }
            catch
            {
                Debug.LogWarning($"[GraphRunner] Could not convert '{valueStr}' to {bbVar.ValueType} for variable '{bbVar.Name}'.");
            }
        }

        private static object ParseValueForType(string valueStr, Type type)
        {
            if (string.IsNullOrEmpty(valueStr)) return null;

            if (type == typeof(Vector2))
            {
                var p = valueStr.Split(',');
                float.TryParse(p.Length > 0 ? p[0].Trim() : "0", NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
                float.TryParse(p.Length > 1 ? p[1].Trim() : "0", NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
                return new Vector2(x, y);
            }
            if (type == typeof(Vector3))
            {
                var p = valueStr.Split(',');
                float.TryParse(p.Length > 0 ? p[0].Trim() : "0", NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
                float.TryParse(p.Length > 1 ? p[1].Trim() : "0", NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
                float.TryParse(p.Length > 2 ? p[2].Trim() : "0", NumberStyles.Float, CultureInfo.InvariantCulture, out float z);
                return new Vector3(x, y, z);
            }
            if (type == typeof(Color))
            {
                ColorUtility.TryParseHtmlString(valueStr, out Color c);
                return c;
            }

            return Convert.ChangeType(valueStr, type, CultureInfo.InvariantCulture);
        }

        private IEnumerator ProcessRunSubgraph(FlowToken token)
        {
            var node      = token.Node;
            var subAsset  = ResolveSubgraphAsset(node);
            if (subAsset == null)
            {
                Debug.LogWarning("[GraphRunner] RunSubgraph: could not resolve a graph asset. Skipping.");
                FollowPort(token, "Out");
                yield break;
            }

            var subGO     = new GameObject($"Subgraph:{subAsset.name}");
            var subRunner = CreateSubRunner(subGO);
            subRunner.Graph = subAsset;

            foreach (var kvp in m_Handlers)
                subRunner.RegisterHandler(kvp.Value);

            bool done = false;
            subRunner.OnGraphEnded.AddListener(() => done = true);
            subRunner.StartGraph();

            yield return new WaitUntil(() => done);

            Destroy(subGO);
            FollowPort(token, "Out");
        }

        /// <summary>
        /// Override to return the subgraph asset for a RunSubgraph node.
        /// Base implementation always returns null (no inline asset references yet).
        /// </summary>
        protected virtual GraphAsset ResolveSubgraphAsset(NodeData node) => null;

        /// <summary>
        /// Factory for the sub-runner used by RunSubgraph nodes.
        /// Override in subclasses to return a typed subclass (e.g. DialogueRunner).
        /// </summary>
        protected virtual GraphRunner CreateSubRunner(GameObject go)
            => go.AddComponent<GraphRunner>();


        /// <summary>Called after the graph starts and the first node is about to execute.</summary>
        protected virtual void OnGraphStart() { }

        /// <summary>Called after the graph ends.</summary>
        protected virtual void OnGraphStop() { }

        /// <summary>
        /// Called when a SetVariable node successfully changes a variable.
        /// Override to fire UI events or drive external systems.
        /// </summary>
        protected virtual void OnVariableChanged(string variableName, string newValueString) { }


        /// <summary>End graph.</summary>
        protected void EndGraph()
        {
            if (!IsRunning) return;
            IsRunning      = false;
            StopAllCoroutines();
            m_ActiveTokens = 0;
            OnGraphStop();
            OnGraphEnded?.Invoke();
        }


        /// <summary>Get node connected to output.</summary>
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
