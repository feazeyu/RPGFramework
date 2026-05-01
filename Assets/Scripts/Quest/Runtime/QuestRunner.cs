using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DialogueGraph.Runtime;

namespace QuestGraph.Runtime
{
    /// <summary>
    /// Outcome of a <see cref="QuestRunner"/> run. Set by the terminal
    /// node (<c>CompleteQuest</c> / <c>FailQuest</c>) and readable after
    /// <c>OnQuestEnded</c> fires. <see cref="InProgress"/> while the graph
    /// is still running.
    /// </summary>
    public enum QuestResult
    {
        InProgress = 0,
        Completed  = 1,
        Failed     = 2,
        Aborted    = 3,
    }

    /// <summary>
    /// Snapshot of an active objective, passed to UI via
    /// <see cref="QuestRunner.OnObjectiveStarted"/>.
    /// </summary>
    [Serializable]
    public struct ObjectiveInfo
    {
        public string NodeGuid;
        public string Title;
        public string Description;
        public bool   Optional;
    }

    /// <summary>
    /// Payload fired when a <c>Reward</c> node executes. All fields
    /// come from the node's Reward fields, resolved through the context
    /// (so blackboard-linked values work too).
    /// </summary>
    [Serializable]
    public struct RewardInfo
    {
        public int                xp;
        public int                currency;
        public ScriptableObject   item;
        public int                quantity;
    }

    /// <summary>
    /// Subclass of <see cref="GraphRunner"/> that executes a Single
    /// quest graph. Handles:
    ///   • Objective — fires <see cref="OnObjectiveStarted"/>, holds
    ///     until <see cref="CompleteObjective"/> / <see cref="FailObjective"/>,
    ///     follows "Completed" or "Failed".
    ///   • Reward — fires <see cref="OnRewardGranted"/>, follows "Out".
    ///   • CompleteQuest — sets <see cref="Result"/>=Completed and ends.
    ///   • FailQuest — sets Result=Failed with reason, ends.
    ///
    /// Shared flow nodes (Start/End/Condition/SetVariable/Sequence/
    /// Selector) are handled by the base <see cref="GraphRunner"/>.
    ///
    /// The runner refuses to start a graph whose asset is a
    /// <see cref="QuestKind.Chain"/> (chain graphs are owned by
    /// <see cref="QuestChainRunner"/>, which doesn't walk them).
    ///
    /// ── Minimal setup ────────────────────────────────────────────────
    ///   1. Add this component to a persistent quest-state GameObject.
    ///   2. Assign a Single <see cref="QuestGraphAsset"/> to Graph.
    ///   3. Wire UI:
    ///        OnObjectiveStarted → show HUD entry, capture NodeGuid
    ///        OnRewardGranted    → award to player inventory
    ///        OnQuestEnded       → check Result, update log
    ///   4. When the player satisfies an objective, call
    ///        runner.CompleteObjective(nodeGuid)
    ///      (or FailObjective(nodeGuid) on failure).
    ///   5. Call runner.StartQuest() to begin.
    /// </summary>
    public class QuestRunner : GraphRunner
    {
        // ── Inspector events ─────────────────────────────────────────────────

        [Header("Quest Events")]
        public ObjectiveEvent       OnObjectiveStarted;
        public ObjectiveEvent       OnObjectiveCompleted;
        public ObjectiveEvent       OnObjectiveFailed;
        public RewardEvent          OnRewardGranted;
        public UnityEvent           OnQuestCompleted;
        public FailedEvent          OnQuestFailed;
        /// <summary>Fires on any end — Completed, Failed, or Aborted.</summary>
        public ResultEvent          OnQuestEnded;

        // ── Public state ─────────────────────────────────────────────────────

        public QuestResult Result        { get; private set; } = QuestResult.InProgress;
        public string      FailureReason { get; private set; }

        /// <summary>The currently awaiting objective, if any — null otherwise.</summary>
        public ObjectiveInfo? ActiveObjective => m_ActiveObjective;

        // ── Internal state ───────────────────────────────────────────────────

        private ObjectiveInfo? m_ActiveObjective;
        // Outcome signal flipped by CompleteObjective / FailObjective
        // so the coroutine in ObjectiveHandler can resume.
        // null = still awaiting, true = complete, false = fail.
        private bool?          m_ObjectiveOutcome;

        // ── Lifecycle ────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            RegisterHandler(new ObjectiveHandler(this));
            RegisterHandler(new RewardHandler(this));
            RegisterHandler(new CompleteQuestHandler(this));
            RegisterHandler(new FailQuestHandler(this));
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void StartQuest()
        {
            if (Graph is QuestGraphAsset qga && qga.Kind == QuestKind.Chain)
            {
                Debug.LogWarning(
                    $"[QuestRunner] Asset '{qga.name}' is a Chain graph. " +
                    "Use QuestChainRunner instead.", this);
                return;
            }

            Result         = QuestResult.InProgress;
            FailureReason  = null;
            m_ActiveObjective  = null;
            m_ObjectiveOutcome = null;

            StartGraph();
        }

        /// <summary>
        /// Signal that the currently active objective is satisfied.
        /// Matched by <see cref="ObjectiveInfo.NodeGuid"/>; calls with
        /// a mismatched guid are ignored (stale UI callback after the
        /// active objective already changed).
        /// </summary>
        public void CompleteObjective(string nodeGuid)
        {
            if (m_ActiveObjective == null) return;
            if (m_ActiveObjective.Value.NodeGuid != nodeGuid) return;
            m_ObjectiveOutcome = true;
        }

        /// <summary>Signal that the currently active objective has failed.</summary>
        public void FailObjective(string nodeGuid)
        {
            if (m_ActiveObjective == null) return;
            if (m_ActiveObjective.Value.NodeGuid != nodeGuid) return;
            m_ObjectiveOutcome = false;
        }

        /// <summary>Force-terminate the quest as aborted (e.g. player quits).</summary>
        public void AbortQuest()
        {
            if (!IsRunning) return;
            Result = QuestResult.Aborted;
            StopGraph();
        }

        // ── GraphRunner overrides ────────────────────────────────────────────

        protected override void OnGraphStop()
        {
            // If the graph ended without hitting a terminal node, report
            // as Aborted so downstream listeners always see a definitive
            // result rather than InProgress.
            if (Result == QuestResult.InProgress)
                Result = QuestResult.Aborted;

            m_ActiveObjective  = null;
            m_ObjectiveOutcome = null;

            OnQuestEnded?.Invoke(Result);
        }

        // ── Node handlers ────────────────────────────────────────────────────

        private class ObjectiveHandler : IGraphNodeHandler
        {
            private readonly QuestRunner m_R;
            public string NodeTypeId => QuestNodeRegistry.TypeObjective;
            public ObjectiveHandler(QuestRunner r) => m_R = r;

            public IEnumerator Execute(NodeData node, GraphRunContext ctx)
            {
                var info = new ObjectiveInfo
                {
                    NodeGuid    = node.Guid,
                    Title       = ctx.ResolveString(node, "Title"),
                    Description = ctx.ResolveString(node, "Description"),
                    Optional    = ParseBool(ctx.ResolveString(node, "Optional")),
                };

                m_R.m_ActiveObjective  = info;
                m_R.m_ObjectiveOutcome = null;
                m_R.OnObjectiveStarted?.Invoke(info);

                yield return new WaitUntil(() => m_R.m_ObjectiveOutcome.HasValue);

                bool completed = m_R.m_ObjectiveOutcome.Value;
                m_R.m_ActiveObjective  = null;
                m_R.m_ObjectiveOutcome = null;

                if (completed)
                {
                    m_R.OnObjectiveCompleted?.Invoke(info);
                    ctx.Follow("Completed");
                }
                else
                {
                    m_R.OnObjectiveFailed?.Invoke(info);
                    ctx.Follow("Failed");
                }
            }

            private static bool ParseBool(string s)
                => bool.TryParse(s, out var b) && b;
        }

        private class RewardHandler : IGraphNodeHandler
        {
            private readonly QuestRunner m_R;
            public string NodeTypeId => QuestNodeRegistry.TypeReward;
            public RewardHandler(QuestRunner r) => m_R = r;

            public IEnumerator Execute(NodeData node, GraphRunContext ctx)
            {
                int.TryParse(ctx.ResolveString(node, "XP"),       out var xp);
                int.TryParse(ctx.ResolveString(node, "Currency"), out var currency);
                int.TryParse(ctx.ResolveString(node, "Quantity"), out var quantity);
                if (quantity <= 0) quantity = 1;

                // Inline Item field can't hold a ScriptableObject reference
                // directly — it must be linked to a blackboard variable of
                // type UnityEngine.ScriptableObject. Try that route.
                ScriptableObject item = null;
                var itemGuid = ctx.GetLinkedGuid(node, "Item");
                if (!string.IsNullOrEmpty(itemGuid))
                {
                    var v = ctx.RuntimeBlackboard.GetVariable(itemGuid);
                    item = v?.ObjectValue as ScriptableObject;
                }

                m_R.OnRewardGranted?.Invoke(new RewardInfo
                {
                    xp       = xp,
                    currency = currency,
                    item     = item,
                    quantity = quantity,
                });

                ctx.Follow("Out");
                yield break;
            }
        }

        private class CompleteQuestHandler : IGraphNodeHandler
        {
            private readonly QuestRunner m_R;
            public string NodeTypeId => QuestNodeRegistry.TypeCompleteQuest;
            public CompleteQuestHandler(QuestRunner r) => m_R = r;

            public IEnumerator Execute(NodeData node, GraphRunContext ctx)
            {
                m_R.Result = QuestResult.Completed;
                m_R.OnQuestCompleted?.Invoke();
                ctx.End();
                yield break;
            }
        }

        private class FailQuestHandler : IGraphNodeHandler
        {
            private readonly QuestRunner m_R;
            public string NodeTypeId => QuestNodeRegistry.TypeFailQuest;
            public FailQuestHandler(QuestRunner r) => m_R = r;

            public IEnumerator Execute(NodeData node, GraphRunContext ctx)
            {
                var reason = ctx.ResolveString(node, "Reason");
                m_R.Result        = QuestResult.Failed;
                m_R.FailureReason = reason;
                m_R.OnQuestFailed?.Invoke(reason);
                ctx.End();
                yield break;
            }
        }
    }

    // ── UnityEvent types ─────────────────────────────────────────────────────

    [Serializable] public class ObjectiveEvent : UnityEvent<ObjectiveInfo> { }
    [Serializable] public class RewardEvent    : UnityEvent<RewardInfo>    { }
    [Serializable] public class ResultEvent    : UnityEvent<QuestResult>   { }
    [Serializable] public class FailedEvent    : UnityEvent<string>        { }
}
