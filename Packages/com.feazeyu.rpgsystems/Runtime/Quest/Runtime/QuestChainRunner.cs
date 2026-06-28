using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using Feazeyu.RPGSystems.Dialogue;

namespace QuestGraph.Runtime
{
    /// <summary>
    /// Per-chain-node lifecycle state for <see cref="QuestChainRunner"/>.
    /// Progression: Locked → Available → Active → Completed/Failed.
    /// Available entries form the frontier the UI exposes to the player.
    /// </summary>
    public enum QuestEntryState
    {
        Locked    = 0,
        Available = 1,
        Active    = 2,
        Completed = 3,
        Failed    = 4,
    }

    /// <summary>
    /// Which kind of quest asset a chain entry references.
    /// </summary>
    public enum QuestSource
    {
        /// <summary>No asset linked (link missing / wrong type). Entry is unusable.</summary>
        None  = 0,
        /// <summary>Full graph-based quest (<see cref="QuestGraphAsset"/>). Executed by <see cref="QuestRunner"/>.</summary>
        Graph = 1,
        /// <summary>Simple ScriptableObject quest (<see cref="QuestAsset"/>). Externally driven.</summary>
        Simple = 2,
    }

    /// <summary>
    /// A single quest reference inside a chain.
    ///
    /// <see cref="Source"/> indicates which of <see cref="GraphQuest"/>
    /// / <see cref="SimpleQuest"/> is populated — exactly one is
    /// non-null for a well-formed entry. <see cref="None"/> means the
    /// link was missing or pointed at an unsupported type.
    /// </summary>
    [Serializable]
    public struct QuestEntry
    {
        /// <summary>GUID of the RunSubgraph NodeData inside the chain asset.</summary>
        public string           ChainNodeGuid;
        /// <summary>Which kind of quest this entry references.</summary>
        public QuestSource      Source;
        /// <summary>Set when <see cref="Source"/> is <see cref="QuestSource.Graph"/>.</summary>
        public QuestGraphAsset  GraphQuest;
        /// <summary>Set when <see cref="Source"/> is <see cref="QuestSource.Simple"/>.</summary>
        public QuestAsset       SimpleQuest;
        /// <summary>Current lifecycle state.</summary>
        public QuestEntryState  State;

        /// <summary>
        /// Best-effort display name for UI. Prefers
        /// <see cref="QuestAsset.Title"/> on simple quests, falls back
        /// to the ScriptableObject's asset name.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (Source == QuestSource.Simple && SimpleQuest != null)
                    return string.IsNullOrEmpty(SimpleQuest.Title) ? SimpleQuest.name : SimpleQuest.Title;
                if (Source == QuestSource.Graph && GraphQuest != null)
                    return GraphQuest.name;
                return "(unresolved)";
            }
        }
    }

    /// <summary>
    /// Runtime driver for a <see cref="QuestKind.Chain"/> quest graph.
    ///
    /// The chain is a static dependency description, not a linear flow:
    /// every <c>RunSubgraph</c> node represents a quest; edges mean
    /// "prerequisite". The runner maintains completion state and
    /// exposes the topological frontier as the set of Available
    /// quests.
    ///
    /// ── Supported quest kinds ────────────────────────────────────
    /// Each Quest Reference node's "Quest" field must be linked to a
    /// blackboard variable. The variable type decides how the quest
    /// is driven:
    /// <list type="bullet">
    /// <item><description>Variable of type <b>QuestGraph</b> holding a
    /// <see cref="QuestGraphAsset"/>: <see cref="StartQuest"/> spawns
    /// a child GameObject with a <see cref="QuestRunner"/> that walks
    /// the quest's graph, and the chain observes its
    /// <c>OnQuestEnded</c> event.</description></item>
    /// <item><description>Variable of type <b>Quest</b> holding a
    /// <see cref="QuestAsset"/> (simple quest, no graph):
    /// <see cref="StartQuest"/> does not spawn a runner — it calls
    /// <see cref="QuestAsset.Start"/> on the asset and waits for
    /// external code to call
    /// <see cref="NotifyExternalQuestCompleted"/> (or
    /// <see cref="NotifyExternalQuestFailed"/>) when the player's
    /// condition is satisfied.</description></item>
    /// </list>
    /// Supporting both lets chains mix graph-based quests (with
    /// objectives and rewards) and simple flag quests without forcing
    /// every quest to be a full graph.
    /// </summary>
    public class QuestChainRunner : MonoBehaviour
    {

        /// <summary>Chain.</summary>
        [Tooltip("The chain asset to track. Must have Kind = Chain.")]
        public QuestGraphAsset Chain;

        /// <summary>Auto start.</summary>
        [Tooltip("If set, chain progress auto-starts in Awake.")]
        public bool AutoStart = false;

        /// <summary>Max active quests.</summary>
        [Tooltip("Maximum number of quests allowed to be active simultaneously. " +
                 "-1 = unlimited; otherwise the chain refuses to start another quest " +
                 "once this many are active. Default 1 (one quest at a time).")]
        public int MaxActiveQuests = 1;

        /// <summary>On available quests changed.</summary>
        [Header("Chain Events")]
        public ChainReadyEvent        OnAvailableQuestsChanged;
        /// <summary>On quest started.</summary>
        public ChainQuestEvent        OnQuestStarted;
        /// <summary>On quest completed.</summary>
        public ChainQuestEvent        OnQuestCompleted;
        /// <summary>On quest failed.</summary>
        public ChainQuestFailedEvent  OnQuestFailed;
        /// <summary>On quest aborted.</summary>
        [Tooltip("Fired when an active quest ends Aborted (e.g. its offer dialogue " +
                 "was declined) and returns to the Available frontier. No completion/" +
                 "failure occurred — listeners tracking the active quest should reset.")]
        public ChainQuestEvent        OnQuestAborted;
        /// <summary>On chain completed.</summary>
        public UnityEvent             OnChainCompleted;


        private readonly Dictionary<string, QuestEntry> m_Entries = new Dictionary<string, QuestEntry>();

        private readonly Dictionary<string, QuestRunner> m_ActiveRunners = new Dictionary<string, QuestRunner>();

        private Blackboard m_RuntimeBlackboard;
        private bool       m_Started;

        /// <summary>Is started.</summary>
        public bool IsStarted => m_Started;

        /// <summary>Number of quests currently in the Active state (graph + simple).</summary>
        public int ActiveQuestCount
        {
            get
            {
                int n = 0;
                foreach (var e in m_Entries.Values)
                    if (e.State == QuestEntryState.Active) n++;
                return n;
            }
        }

        /// <summary>The child runners of all currently-active graph quests.</summary>
        public IReadOnlyCollection<QuestRunner> ActiveRunners => m_ActiveRunners.Values;

        /// <summary>The runner for a specific active graph quest, or null (simple / not active).</summary>
        public QuestRunner GetActiveRunner(string chainNodeGuid)
            => m_ActiveRunners.TryGetValue(chainNodeGuid, out var r) ? r : null;


        private void Awake()
        {
            if (AutoStart) StartChain();
        }

        private void OnDestroy()
        {
            foreach (var r in m_ActiveRunners.Values)
                if (r != null) Destroy(r.gameObject);
            m_ActiveRunners.Clear();
        }


        /// <summary>Start chain.</summary>
        public void StartChain()
        {
            if (Chain == null)
            {
                Debug.LogWarning($"[QuestChainRunner] No chain assigned on '{name}'.", this);
                return;
            }
            if (Chain.Kind != QuestKind.Chain)
            {
                Debug.LogWarning(
                    $"[QuestChainRunner] Asset '{Chain.name}' has Kind={Chain.Kind}, " +
                    "expected Chain. Refusing to start.", this);
                return;
            }
            if (m_Started) return;

            m_RuntimeBlackboard = Chain.Blackboard.CloneForRuntime();
            BuildEntries();
            RecomputeFrontier();

            m_Started = true;
            OnAvailableQuestsChanged?.Invoke(GetAvailableQuests());
        }

        /// <summary>Returns the list of quest entries currently in the Available state.</summary>
        public List<QuestEntry> GetAvailableQuests()
        {
            var result = new List<QuestEntry>();
            foreach (var kv in m_Entries)
                if (kv.Value.State == QuestEntryState.Available)
                    result.Add(kv.Value);
            return result;
        }

        /// <summary>Returns every entry (any state) — useful for a quest-log UI.</summary>
        public IEnumerable<QuestEntry> GetAllEntries() => m_Entries.Values;

        /// <summary>
        /// Begin the given available quest. Behaviour depends on
        /// <see cref="QuestEntry.Source"/>:
        /// <list type="bullet">
        /// <item><description>Graph quest → spawn a child
        /// <see cref="QuestRunner"/> and hand over.</description></item>
        /// <item><description>Simple quest → transition the entry to
        /// Active and wait for external notification.</description></item>
        /// </list>
        /// Up to <see cref="MaxActiveQuests"/> quests may run in parallel
        /// (-1 = unlimited); the call is refused once that cap is reached.
        /// </summary>
        public void StartQuest(string chainNodeGuid)
        {
            if (!m_Started) StartChain();

            if (!m_Entries.TryGetValue(chainNodeGuid, out var entry))
            {
                Debug.LogWarning($"[QuestChainRunner] Unknown chain node '{chainNodeGuid}'.", this);
                return;
            }

            if (entry.State != QuestEntryState.Available)
            {
                Debug.LogWarning(
                    $"[QuestChainRunner] Quest '{chainNodeGuid}' is not available (state={entry.State}).",
                    this);
                return;
            }

            if (entry.Source == QuestSource.None)
            {
                Debug.LogWarning(
                    $"[QuestChainRunner] Chain node '{chainNodeGuid}' has no resolved Quest asset. " +
                    "Ensure the node's Quest field is linked to a blackboard variable of type " +
                    "QuestGraph or Quest.", this);
                return;
            }

            if (MaxActiveQuests >= 0 && ActiveQuestCount >= MaxActiveQuests)
            {
                Debug.LogWarning(
                    $"[QuestChainRunner] Active quest cap reached ({ActiveQuestCount}/{MaxActiveQuests}). " +
                    "Complete or abort an active quest before starting another.", this);
                return;
            }

            entry.State = QuestEntryState.Active;
            m_Entries[chainNodeGuid] = entry;

            ApplyEntrySetVariables(chainNodeGuid);

            QuestRunner spawned = null;
            switch (entry.Source)
            {
                case QuestSource.Graph:
                    spawned = SpawnGraphQuest(entry);
                    break;
                case QuestSource.Simple:
                    StartSimpleQuest(entry);
                    break;
            }

            OnQuestStarted?.Invoke(entry);

            spawned?.StartQuest();
        }

        private QuestRunner SpawnGraphQuest(QuestEntry entry)
        {
            var go = new GameObject($"Quest:{entry.GraphQuest.name}");
            go.transform.SetParent(transform, false);
            var runner = go.AddComponent<QuestRunner>();
            runner.Graph = entry.GraphQuest;

            var captured = entry.ChainNodeGuid;
            runner.OnQuestEnded.AddListener(r => OnGraphQuestEnded(captured, r));

            m_ActiveRunners[entry.ChainNodeGuid] = runner;
            return runner;
        }


        /// <summary>
        /// Executes the SetVariable nodes that feed into a quest node — i.e. the
        /// SetVariable nodes reachable by walking backward from it through flow
        /// edges, stopping at any other quest (RunSubgraph) node or the Start.
        /// This lets a Chain graph stage per-quest blackboard values (e.g. offer
        /// text on a Shared variable the quest's Run Dialogue reads) even though the
        /// chain is a dependency graph, not an executed flow.
        /// </summary>
        private void ApplyEntrySetVariables(string questNodeGuid)
        {
            if (m_RuntimeBlackboard == null) return;

            var setters = new List<NodeData>();
            var visited = new HashSet<string>();
            var stack   = new Stack<string>();

            foreach (var e in Chain.Edges)
                if (e.InputNodeGuid == questNodeGuid && IsEdgeLive(e)) stack.Push(e.OutputNodeGuid);

            while (stack.Count > 0)
            {
                var g = stack.Pop();
                if (!visited.Add(g)) continue;

                var n = Chain.GetNode(g);
                if (n == null || n.NodeType == QuestNodeRegistry.TypeRunSubgraph) continue;

                if (n.NodeType == NodeRegistry.TypeSetVariable) setters.Add(n);

                foreach (var e in Chain.Edges)
                    if (e.InputNodeGuid == g && IsEdgeLive(e)) stack.Push(e.OutputNodeGuid);
            }

            for (int i = setters.Count - 1; i >= 0; i--)
                ExecuteSetVariable(setters[i]);
        }

        private void ExecuteSetVariable(NodeData node)
        {
            var varField = node.Fields?.Find(f => f.FieldName == "Variable");
            var guid     = varField?.LinkedVariableGuid;
            if (string.IsNullOrEmpty(guid)) return;

            var v = m_RuntimeBlackboard.GetVariable(guid);
            if (v == null) return;

            var valueStr = node.Fields?.Find(f => f.FieldName == "Value")?.InlineValue ?? string.Empty;

            try
            {
                v.ObjectValue = v.ValueType == typeof(string)
                    ? valueStr
                    : Convert.ChangeType(valueStr, v.ValueType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[QuestChainRunner] Could not set '{v.Name}' to '{valueStr}': {ex.Message}", this);
            }
        }

        private void StartSimpleQuest(QuestEntry entry)
        {
            entry.SimpleQuest.Start();
        }

        /// <summary>
        /// Called by game code when a simple quest
        /// (<see cref="QuestSource.Simple"/>) has been completed. Has
        /// no effect on graph-based quests (they drive themselves
        /// through <see cref="QuestRunner.OnQuestEnded"/>). Safe to
        /// call even if the quest is not currently active — it will
        /// no-op.
        /// </summary>
        public void NotifyExternalQuestCompleted(string chainNodeGuid)
        {
            NotifyExternal(chainNodeGuid, QuestResult.Completed, reason: null);
        }

        /// <summary>Signal external failure of a simple quest.</summary>
        public void NotifyExternalQuestFailed(string chainNodeGuid, string reason = null)
        {
            NotifyExternal(chainNodeGuid, QuestResult.Failed, reason);
        }

        private void NotifyExternal(string chainNodeGuid, QuestResult result, string reason)
        {
            if (!m_Entries.TryGetValue(chainNodeGuid, out var entry)) return;
            if (entry.Source != QuestSource.Simple) return;
            if (entry.State != QuestEntryState.Active) return;

            if (result == QuestResult.Completed) entry.SimpleQuest.Complete();
            else                                  entry.SimpleQuest.Fail();

            ResolveEntry(chainNodeGuid, result, reason);
        }

        /// <summary>Abort a specific active quest by its chain node guid.</summary>
        public void AbortQuest(string chainNodeGuid)
        {
            if (!m_Entries.TryGetValue(chainNodeGuid, out var entry)) return;
            if (entry.State != QuestEntryState.Active) return;

            if (m_ActiveRunners.TryGetValue(chainNodeGuid, out var runner) && runner != null)
            {
                runner.AbortQuest();
                return;
            }
            NotifyExternalQuestFailed(chainNodeGuid, "aborted");
        }

        /// <summary>Abort every currently active quest.</summary>
        public void AbortActiveQuest()
        {
            var active = new List<string>();
            foreach (var kv in m_Entries)
                if (kv.Value.State == QuestEntryState.Active) active.Add(kv.Key);

            foreach (var guid in active) AbortQuest(guid);
        }


        private void OnGraphQuestEnded(string chainNodeGuid, QuestResult result)
        {
            if (m_ActiveRunners.TryGetValue(chainNodeGuid, out var runner) && runner != null)
            {
                m_ActiveRunners.Remove(chainNodeGuid);
                Destroy(runner.gameObject);
            }

            ResolveEntry(chainNodeGuid, result,
                reason: result == QuestResult.Failed ? "Quest failed" : null);
        }

        private void ResolveEntry(string chainNodeGuid, QuestResult result, string reason)
        {
            if (!m_Entries.TryGetValue(chainNodeGuid, out var entry)) return;

            if (result == QuestResult.Aborted)
            {
                entry.State       = QuestEntryState.Available;
                m_Entries[chainNodeGuid] = entry;
                OnQuestAborted?.Invoke(entry);
                OnAvailableQuestsChanged?.Invoke(GetAvailableQuests());
                return;
            }

            entry.State = result == QuestResult.Completed
                ? QuestEntryState.Completed
                : QuestEntryState.Failed;
            m_Entries[chainNodeGuid] = entry;

            if (result == QuestResult.Completed)
                OnQuestCompleted?.Invoke(entry);
            else
                OnQuestFailed?.Invoke(entry, reason ?? "failed");

            RecomputeFrontier();
            OnAvailableQuestsChanged?.Invoke(GetAvailableQuests());

            if (IsChainCompleted())
                OnChainCompleted?.Invoke();
        }


        private void BuildEntries()
        {
            m_Entries.Clear();

            foreach (var node in Chain.Nodes)
            {
                if (node.NodeType != QuestNodeRegistry.TypeRunSubgraph) continue;

                QuestSource     source      = QuestSource.None;
                QuestGraphAsset graphQuest  = null;
                QuestAsset      simpleQuest = null;

                var field = node.Fields?.Find(f => f.FieldName == "Quest");
                if (field != null && !string.IsNullOrEmpty(field.LinkedVariableGuid))
                {
                    var bbVar = m_RuntimeBlackboard.GetVariable(field.LinkedVariableGuid);
                    var obj   = bbVar?.ObjectValue;

                    if (obj is QuestGraphAsset qga)
                    {
                        if (qga.Kind != QuestKind.Single)
                        {
                            Debug.LogWarning(
                                $"[QuestChainRunner] Chain node '{node.DisplayName}' references " +
                                $"'{qga.name}', which is not a Single quest (Kind={qga.Kind}). " +
                                "Only Single graph quests can appear inside a chain.", this);
                        }
                        else
                        {
                            source     = QuestSource.Graph;
                            graphQuest = qga;
                        }
                    }
                    else if (obj is QuestAsset qa)
                    {
                        source      = QuestSource.Simple;
                        simpleQuest = qa;
                    }
                }

                m_Entries[node.Guid] = new QuestEntry
                {
                    ChainNodeGuid = node.Guid,
                    Source        = source,
                    GraphQuest    = graphQuest,
                    SimpleQuest   = simpleQuest,
                    State         = QuestEntryState.Locked,
                };
            }
        }

        /// <summary>
        /// Transition Locked entries that are currently unlockable into Available.
        /// Re-evaluated every state change, so live <see cref="NodeRegistry.TypeCondition"/>
        /// gates (which read the chain's runtime blackboard) can open or hold quests
        /// as the chain progresses. Idempotent; promotion only (never demotes).
        /// </summary>
        private void RecomputeFrontier()
        {
            bool changed;
            do
            {
                changed = false;
                foreach (var guid in new List<string>(m_Entries.Keys))
                {
                    var e = m_Entries[guid];
                    if (e.State != QuestEntryState.Locked) continue;
                    if (!IsUnlockable(guid)) continue;

                    e.State = QuestEntryState.Available;
                    m_Entries[guid] = e;
                    changed = true;
                }
            } while (changed);
        }

        /// <summary>
        /// Whether a quest node is currently unlockable: it must be <b>rooted</b>
        /// (reachable from Start, or from a prerequisite quest, along <b>live</b>
        /// edges) and every prerequisite quest on those live paths must be Completed.
        /// An edge leaving a Condition node is live only on the branch matching the
        /// gate's current evaluation, so a quest behind a not-yet-true Condition
        /// stays Locked. Walks backward, stopping at the first quest on each path
        /// (those are the prerequisites — AND semantics, as before).
        /// </summary>
        private bool IsUnlockable(string questNodeGuid)
        {
            var prereqs = new HashSet<string>();
            var visited = new HashSet<string>();
            var stack   = new Stack<string>();

            bool hasIncoming = false;
            foreach (var e in Chain.Edges)
                if (e.InputNodeGuid == questNodeGuid)
                {
                    hasIncoming = true;
                    if (IsEdgeLive(e)) stack.Push(e.OutputNodeGuid);
                }

            bool rooted = !hasIncoming;

            while (stack.Count > 0)
            {
                var g = stack.Pop();
                if (!visited.Add(g)) continue;

                var n = Chain.GetNode(g);
                if (n == null) continue;

                if (n.NodeType == QuestNodeRegistry.TypeRunSubgraph) { prereqs.Add(g); rooted = true; continue; }
                if (n.NodeType == NodeRegistry.TypeStart)            { rooted = true; continue; }

                foreach (var e in Chain.Edges)
                    if (e.InputNodeGuid == g && IsEdgeLive(e))
                        stack.Push(e.OutputNodeGuid);
            }

            if (!rooted) return false;

            foreach (var p in prereqs)
                if (!m_Entries.TryGetValue(p, out var pe) || pe.State != QuestEntryState.Completed)
                    return false;
            return true;
        }

        /// <summary>
        /// An edge is "live" unless it leaves a Condition node on the branch the
        /// gate does not currently take. Used by frontier and SetVariable walks so
        /// the chain honours Condition gates without otherwise executing the graph.
        /// </summary>
        private bool IsEdgeLive(EdgeData edge)
        {
            var src = Chain.GetNode(edge.OutputNodeGuid);
            if (src != null && src.NodeType == NodeRegistry.TypeCondition)
                return EvaluateGate(src) == (edge.OutputPortName == "True");
            return true;
        }

        /// <summary>Evaluates a chain Condition node against the runtime blackboard.</summary>
        private bool EvaluateGate(NodeData node)
        {
            var guid = node.Fields?.Find(f => f.FieldName == "Variable")?.LinkedVariableGuid;
            if (string.IsNullOrEmpty(guid)) return false;

            var v = m_RuntimeBlackboard?.GetVariable(guid);
            if (v?.ObjectValue == null) return false;

            var op  = node.Fields?.Find(f => f.FieldName == "Operator")?.InlineValue ?? "==";
            var val = node.Fields?.Find(f => f.FieldName == "Value")?.InlineValue ?? string.Empty;
            return CompareValue(v.ObjectValue, op, val);
        }

        private static bool CompareValue(object lhs, string op, string rhs)
        {
            if (double.TryParse(lhs.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out double ln) &&
                double.TryParse(rhs,            NumberStyles.Any, CultureInfo.InvariantCulture, out double rn))
                return op switch
                {
                    "==" => ln == rn, "!=" => ln != rn,
                    ">"  => ln >  rn, ">=" => ln >= rn,
                    "<"  => ln <  rn, "<=" => ln <= rn, _ => false,
                };

            if (lhs is bool lb && bool.TryParse(rhs, out bool rb))
                return op switch { "==" => lb == rb, "!=" => lb != rb, _ => false };

            return op switch
            {
                "==" => string.Equals(lhs.ToString(), rhs, StringComparison.Ordinal),
                "!=" => !string.Equals(lhs.ToString(), rhs, StringComparison.Ordinal),
                _    => false,
            };
        }

        private bool IsChainCompleted()
        {
            foreach (var e in m_Entries.Values)
                if (e.State == QuestEntryState.Locked ||
                    e.State == QuestEntryState.Available ||
                    e.State == QuestEntryState.Active)
                    return false;
            return true;
        }
    }


    [Serializable] public class ChainReadyEvent       : UnityEvent<List<QuestEntry>>  { }
    [Serializable] public class ChainQuestEvent       : UnityEvent<QuestEntry>        { }
    [Serializable] public class ChainQuestFailedEvent : UnityEvent<QuestEntry, string> { }
}
