using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DialogueGraph.Runtime;

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
        // ── Inspector ────────────────────────────────────────────────────────

        [Tooltip("The chain asset to track. Must have Kind = Chain.")]
        public QuestGraphAsset Chain;

        [Tooltip("If set, chain progress auto-starts in Awake.")]
        public bool AutoStart = false;

        [Header("Chain Events")]
        public ChainReadyEvent        OnAvailableQuestsChanged;
        public ChainQuestEvent        OnQuestStarted;
        public ChainQuestEvent        OnQuestCompleted;
        public ChainQuestFailedEvent  OnQuestFailed;
        public UnityEvent             OnChainCompleted;

        // ── State ────────────────────────────────────────────────────────────

        private readonly Dictionary<string, QuestEntry> m_Entries = new Dictionary<string, QuestEntry>();
        private readonly Dictionary<string, HashSet<string>> m_Prereqs
            = new Dictionary<string, HashSet<string>>();

        private Blackboard  m_RuntimeBlackboard;
        private bool        m_Started;
        private QuestRunner m_ActiveRunner;
        private string      m_ActiveEntryGuid;

        public bool        IsStarted     => m_Started;
        public QuestRunner ActiveRunner  => m_ActiveRunner;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (AutoStart) StartChain();
        }

        private void OnDestroy()
        {
            if (m_ActiveRunner != null) Destroy(m_ActiveRunner.gameObject);
        }

        // ── Public API ───────────────────────────────────────────────────────

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

            m_RuntimeBlackboard = Chain.Blackboard.Clone(Chain.Blackboard);
            BuildEntries();
            BuildPrerequisiteMap();
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
        /// Only one quest runs at a time per chain.
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

            if (m_ActiveRunner != null || !string.IsNullOrEmpty(m_ActiveEntryGuid))
            {
                Debug.LogWarning(
                    $"[QuestChainRunner] Quest '{m_ActiveEntryGuid}' is already active. " +
                    "Complete or abort it before starting another.", this);
                return;
            }

            // Transition state before firing events so listeners see a consistent world.
            entry.State = QuestEntryState.Active;
            m_Entries[chainNodeGuid] = entry;
            m_ActiveEntryGuid = chainNodeGuid;

            OnQuestStarted?.Invoke(entry);

            switch (entry.Source)
            {
                case QuestSource.Graph:
                    StartGraphQuest(entry);
                    break;
                case QuestSource.Simple:
                    StartSimpleQuest(entry);
                    break;
            }
        }

        private void StartGraphQuest(QuestEntry entry)
        {
            var go = new GameObject($"Quest:{entry.GraphQuest.name}");
            go.transform.SetParent(transform, false);
            var runner = go.AddComponent<QuestRunner>();
            runner.Graph = entry.GraphQuest;

            var captured = entry.ChainNodeGuid;
            runner.OnQuestEnded.AddListener(r => OnGraphQuestEnded(captured, r));

            m_ActiveRunner = runner;
            runner.StartQuest();
        }

        private void StartSimpleQuest(QuestEntry entry)
        {
            // Simple quests are externally driven — no runner is spawned.
            // Just call Start() on the asset so its OnStarted fires (UI
            // may be wired to that), then wait for external code to call
            // NotifyExternalQuestCompleted / Failed.
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

            // Reflect the outcome on the asset so any listeners wired to
            // the asset's own events fire too.
            if (result == QuestResult.Completed) entry.SimpleQuest.Complete();
            else                                  entry.SimpleQuest.Fail();

            ResolveEntry(chainNodeGuid, result, reason);
        }

        /// <summary>Abort the currently active quest (if any).</summary>
        public void AbortActiveQuest()
        {
            if (m_ActiveRunner != null) { m_ActiveRunner.AbortQuest(); return; }
            if (!string.IsNullOrEmpty(m_ActiveEntryGuid))
                NotifyExternalQuestFailed(m_ActiveEntryGuid, "aborted");
        }

        // ── Internal: child runner callback ──────────────────────────────────

        private void OnGraphQuestEnded(string chainNodeGuid, QuestResult result)
        {
            // Tear down the child GameObject next frame so we don't
            // destroy it mid-event-invocation.
            if (m_ActiveRunner != null)
            {
                var toDestroy = m_ActiveRunner.gameObject;
                m_ActiveRunner = null;
                Destroy(toDestroy);
            }

            ResolveEntry(chainNodeGuid, result,
                reason: result == QuestResult.Failed ? "Quest failed" : null);
        }

        private void ResolveEntry(string chainNodeGuid, QuestResult result, string reason)
        {
            if (!m_Entries.TryGetValue(chainNodeGuid, out var entry)) return;

            entry.State = result == QuestResult.Completed
                ? QuestEntryState.Completed
                : QuestEntryState.Failed;
            m_Entries[chainNodeGuid] = entry;

            m_ActiveEntryGuid = null;

            if (result == QuestResult.Completed)
                OnQuestCompleted?.Invoke(entry);
            else
                OnQuestFailed?.Invoke(entry, reason ?? "failed");

            RecomputeFrontier();
            OnAvailableQuestsChanged?.Invoke(GetAvailableQuests());

            if (IsChainCompleted())
                OnChainCompleted?.Invoke();
        }

        // ── Internal: graph analysis ─────────────────────────────────────────

        private void BuildEntries()
        {
            m_Entries.Clear();

            foreach (var node in Chain.Nodes)
            {
                if (node.NodeType != QuestNodeRegistry.TypeRunSubgraph) continue;

                // The "Quest" field must be linked to a blackboard variable
                // (inline FieldData can't hold asset references). The variable
                // type decides which Source we record.
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
        /// For each RunSubgraph node X, collect the set of RunSubgraph
        /// nodes reachable by walking backward along edges (transitively
        /// through non-RunSubgraph flow nodes — Condition, Start, etc.
        /// don't count as prerequisites, only the upstream quests do).
        /// </summary>
        private void BuildPrerequisiteMap()
        {
            m_Prereqs.Clear();
            foreach (var entryGuid in m_Entries.Keys)
                m_Prereqs[entryGuid] = new HashSet<string>();

            var incoming = new Dictionary<string, List<string>>();
            foreach (var edge in Chain.Edges)
            {
                if (!incoming.TryGetValue(edge.InputNodeGuid, out var list))
                    incoming[edge.InputNodeGuid] = list = new List<string>();
                list.Add(edge.OutputNodeGuid);
            }

            foreach (var entry in m_Entries.Values)
            {
                var prereqs = m_Prereqs[entry.ChainNodeGuid];
                var visited = new HashSet<string>();
                var queue   = new Queue<string>();

                if (incoming.TryGetValue(entry.ChainNodeGuid, out var seeds))
                    foreach (var s in seeds) queue.Enqueue(s);

                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    if (!visited.Add(cur)) continue;

                    var curNode = Chain.GetNode(cur);
                    if (curNode == null) continue;

                    if (curNode.NodeType == QuestNodeRegistry.TypeRunSubgraph)
                    {
                        prereqs.Add(cur);
                        continue;
                    }

                    if (incoming.TryGetValue(cur, out var ups))
                        foreach (var u in ups) queue.Enqueue(u);
                }
            }
        }

        /// <summary>
        /// Transition Locked entries whose prereqs are all Completed
        /// into Available. Idempotent; called after every state change.
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
                    if (!PrereqsSatisfied(guid)) continue;

                    e.State = QuestEntryState.Available;
                    m_Entries[guid] = e;
                    changed = true;
                }
            } while (changed);
        }

        private bool PrereqsSatisfied(string entryGuid)
        {
            if (!m_Prereqs.TryGetValue(entryGuid, out var prereqs)) return true;
            foreach (var p in prereqs)
            {
                if (!m_Entries.TryGetValue(p, out var pe)) return false;
                if (pe.State != QuestEntryState.Completed) return false;
            }
            return true;
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

    // ── UnityEvent types ─────────────────────────────────────────────────────

    [Serializable] public class ChainReadyEvent       : UnityEvent<List<QuestEntry>>  { }
    [Serializable] public class ChainQuestEvent       : UnityEvent<QuestEntry>        { }
    [Serializable] public class ChainQuestFailedEvent : UnityEvent<QuestEntry, string> { }
}
