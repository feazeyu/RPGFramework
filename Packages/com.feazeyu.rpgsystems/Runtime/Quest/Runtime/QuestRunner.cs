using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Feazeyu.RPGSystems.Dialogue;
using QuestGraph.Nodes;
using Feazeyu.RPGSystems.Inventory;

namespace QuestGraph.Runtime
{
    public enum QuestResult
    {
        InProgress = 0,
        Completed  = 1,
        Failed     = 2,
        Aborted    = 3,
    }

    [Serializable]
    public struct ObjectiveInfo
    {
        public string NodeGuid;
        public string Title;
        public string Description;
        public bool   Optional;
    }

    [Serializable]
    public struct RewardInfo
    {
        public int                xp;
        public int                currency;
        public ScriptableObject   item;
        public int                quantity;
    }

    /// <summary>
    /// Executes a Single quest graph. Supports any number of simultaneously
    /// active objectives (needed for continuous-mode parallel objectives).
    ///
    /// Objective node handlers call RegisterObjective / UnregisterObjective
    /// directly; external code still calls CompleteObjective / FailObjective
    /// by GUID as before.
    /// </summary>
    public class QuestRunner : GraphRunner
    {
        // ── Inspector events ──────────────────────────────────────────────────

        [Header("Quest Events")]
        public ObjectiveEvent  OnObjectiveStarted;
        public ObjectiveEvent  OnObjectiveCompleted;
        public ObjectiveEvent  OnObjectiveFailed;
        public RewardEvent     OnRewardGranted;
        public UnityEvent      OnQuestCompleted;
        public FailedEvent     OnQuestFailed;
        public ResultEvent     OnQuestEnded;

        // ── Public state ──────────────────────────────────────────────────────

        public QuestResult Result        { get; private set; } = QuestResult.InProgress;
        public string      FailureReason { get; private set; }

        // Backward-compat: returns the first active objective if any.
        public ObjectiveInfo? ActiveObjective
        {
            get
            {
                foreach (var v in m_ActiveObjectives.Values) return v;
                return null;
            }
        }

        public IReadOnlyCollection<ObjectiveInfo> ActiveObjectives => m_ActiveObjectives.Values;

        // ── Internal state ────────────────────────────────────────────────────

        // nodeGuid → info for every currently-active objective
        private readonly Dictionary<string, ObjectiveInfo> m_ActiveObjectives  = new();
        // nodeGuid → null (pending) | true (complete) | false (failed)
        private readonly Dictionary<string, bool?>         m_ObjectiveOutcomes = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            RegisterHandler(new ObjectiveHandler(this));
            RegisterHandler(new RewardHandler(this));
            RegisterHandler(new CompleteQuestHandler(this));
            RegisterHandler(new FailQuestHandler(this));
            RegisterHandler(new KillCountObjectiveHandler());
            RegisterHandler(new ReachLocationObjectiveHandler());
            RegisterHandler(new CollectItemObjectiveHandler());
            RegisterHandler(new DeliverItemObjectiveHandler());
            RegisterHandler(new SpawnItemHandler());
            RegisterHandler(new DebugLogNodeHandler());
            RegisterHandler(new FindObjectNodeHandler());
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void StartQuest()
        {
            if (Graph is QuestGraphAsset qga && qga.Kind == QuestKind.Chain)
            {
                Debug.LogWarning(
                    $"[QuestRunner] Asset '{qga.name}' is a Chain graph. " +
                    "Use QuestChainRunner instead.", this);
                return;
            }

            Result        = QuestResult.InProgress;
            FailureReason = null;
            m_ActiveObjectives.Clear();
            m_ObjectiveOutcomes.Clear();

            StartGraph();
        }

        /// <summary>Signal that the named objective was satisfied.</summary>
        public void CompleteObjective(string nodeGuid)
        {
            if (!m_ActiveObjectives.ContainsKey(nodeGuid)) return;
            m_ObjectiveOutcomes[nodeGuid] = true;
        }

        /// <summary>Signal that the named objective has failed.</summary>
        public void FailObjective(string nodeGuid)
        {
            if (!m_ActiveObjectives.ContainsKey(nodeGuid)) return;
            m_ObjectiveOutcomes[nodeGuid] = false;
        }

        /// <summary>
        /// Immediately fail the quest (e.g., a continuous guard lost its condition).
        /// Safe to call from any coroutine running on this runner.
        /// </summary>
        public void ForceFailQuest(string reason)
        {
            if (!IsRunning) return;
            Result        = QuestResult.Failed;
            FailureReason = reason;
            OnQuestFailed?.Invoke(reason);
            StopGraph();
        }

        public void AbortQuest()
        {
            if (!IsRunning) return;
            Result = QuestResult.Aborted;
            StopGraph();
        }

        // ── Methods for objective node handlers ───────────────────────────────

        /// <summary>
        /// Called by objective node handlers at the start of execution.
        /// Fires OnObjectiveStarted so the HUD can show the new objective.
        /// </summary>
        public void RegisterObjective(ObjectiveInfo info)
        {
            m_ActiveObjectives[info.NodeGuid]  = info;
            m_ObjectiveOutcomes[info.NodeGuid] = null;
            OnObjectiveStarted?.Invoke(info);
        }

        /// <summary>
        /// Called by objective node handlers when they conclude (complete, fail,
        /// or continuous monitor ends). Fires the appropriate event.
        /// </summary>
        public void UnregisterObjective(string nodeGuid, bool? outcome = null)
        {
            if (!m_ActiveObjectives.TryGetValue(nodeGuid, out var info)) return;
            m_ActiveObjectives.Remove(nodeGuid);
            m_ObjectiveOutcomes.Remove(nodeGuid);
            if (outcome == true)  OnObjectiveCompleted?.Invoke(info);
            if (outcome == false) OnObjectiveFailed?.Invoke(info);
        }

        /// <summary>Returns the pending outcome for an active objective, or null if still waiting.</summary>
        public bool? GetObjectiveOutcome(string nodeGuid)
            => m_ObjectiveOutcomes.TryGetValue(nodeGuid, out var v) ? v : null;

        // ── GraphRunner overrides ─────────────────────────────────────────────

        protected override void OnGraphStop()
        {
            if (Result == QuestResult.InProgress)
                Result = QuestResult.Aborted;

            m_ActiveObjectives.Clear();
            m_ObjectiveOutcomes.Clear();

            OnQuestEnded?.Invoke(Result);
        }

        // ── Built-in node handlers ────────────────────────────────────────────

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

                m_R.RegisterObjective(info);

                yield return new WaitUntil(() =>
                    m_R.m_ObjectiveOutcomes.TryGetValue(info.NodeGuid, out var v) && v.HasValue);

                bool completed = m_R.m_ObjectiveOutcomes[info.NodeGuid].Value;
                m_R.m_ActiveObjectives.Remove(info.NodeGuid);
                m_R.m_ObjectiveOutcomes.Remove(info.NodeGuid);

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

            private static bool ParseBool(string s) => bool.TryParse(s, out var b) && b;
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
                var reason       = ctx.ResolveString(node, "Reason");
                m_R.Result       = QuestResult.Failed;
                m_R.FailureReason = reason;
                m_R.OnQuestFailed?.Invoke(reason);
                ctx.End();
                yield break;
            }
        }

        private class SpawnItemHandler : IGraphNodeHandler
        {
            public string NodeTypeId => QuestNodeRegistry.TypeSpawnItem;

            public IEnumerator Execute(NodeData node, GraphRunContext ctx)
            {
                int.TryParse(ctx.ResolveString(node, "ItemId"), out int itemId);
                var itemPrefab = InventoryManager.Instance?.GetItemById(itemId);
                Debug.Log($"[SpawnItemHandler] itemId={itemId}, itemPrefab={itemPrefab}");

                GameObject target = null;
                var targetField = ctx.GetField(node, "Target");
                if (targetField != null && !string.IsNullOrEmpty(targetField.LinkedVariableGuid))
                {
                    var v = ctx.RuntimeBlackboard.GetVariable(targetField.LinkedVariableGuid);
                    target = v?.ObjectValue as GameObject;
                }

                bool success = false;
                if (itemPrefab != null && target != null)
                {
                    var container = target.GetComponentInChildren<IItemContainer>();
                    if (container != null)
                    {
                        var instance = Instantiate(itemPrefab);
                        success = container.PutItem(instance);
                        if (!success)
                            Destroy(instance);
                    }
                }

                ctx.Follow(success ? "Success" : "Failure");
                yield break;
            }
        }
    }

    // ── UnityEvent types ──────────────────────────────────────────────────────

    [Serializable] public class ObjectiveEvent : UnityEvent<ObjectiveInfo> { }
    [Serializable] public class RewardEvent    : UnityEvent<RewardInfo>    { }
    [Serializable] public class ResultEvent    : UnityEvent<QuestResult>   { }
    [Serializable] public class FailedEvent    : UnityEvent<string>        { }
}
