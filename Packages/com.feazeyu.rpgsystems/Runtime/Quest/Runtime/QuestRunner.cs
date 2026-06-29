using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Feazeyu.RPGSystems.Dialogue;
using Feazeyu.RPGSystems.Quest.Nodes;
using Feazeyu.RPGSystems.Inventory;

namespace Feazeyu.RPGSystems.Quest
{
    /// <summary>Outcome of a quest run.</summary>
    public enum QuestResult
    {
        /// <summary>The quest is still running.</summary>
        InProgress = 0,
        /// <summary>The quest completed successfully.</summary>
        Completed  = 1,
        /// <summary>The quest failed.</summary>
        Failed     = 2,
        /// <summary>The quest was aborted (e.g. offer declined or externally cancelled).</summary>
        Aborted    = 3,
    }

    /// <summary>
    /// Describes an active objective to listeners (HUD/quest log). Carried through the
    /// runner's objective events; <see cref="NodeGuid"/> keys it to its graph node.
    /// </summary>
    [Serializable]
    public struct ObjectiveInfo
    {
        /// <summary>GUID of the objective's graph node.</summary>
        public string NodeGuid;
        /// <summary>Display title.</summary>
        public string Title;
        /// <summary>Display description.</summary>
        public string Description;
        /// <summary>Whether the objective is optional (not required to complete the quest).</summary>
        public bool   Optional;
    }

    /// <summary>Describes a reward granted by a Reward node.</summary>
    [Serializable]
    public struct RewardInfo
    {
        /// <summary>Experience points granted.</summary>
        public int                xp;
        /// <summary>Currency granted.</summary>
        public int                currency;
        /// <summary>Item asset granted, if any.</summary>
        public ScriptableObject   item;
        /// <summary>Quantity of <see cref="item"/> granted.</summary>
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

        /// <summary>On objective started.</summary>
        [Header("Quest Events")]
        public ObjectiveEvent  OnObjectiveStarted  = new();
        /// <summary>On objective completed.</summary>
        public ObjectiveEvent  OnObjectiveCompleted = new();
        /// <summary>On objective failed.</summary>
        public ObjectiveEvent  OnObjectiveFailed   = new();
        /// <summary>On reward granted.</summary>
        public RewardEvent     OnRewardGranted     = new();
        /// <summary>On quest completed.</summary>
        public UnityEvent      OnQuestCompleted    = new();
        /// <summary>On quest failed.</summary>
        public FailedEvent     OnQuestFailed       = new();
        /// <summary>On quest ended.</summary>
        public ResultEvent     OnQuestEnded        = new();

        /// <summary>On timer timeout.</summary>
        [Tooltip("Fired each time any Timer node in the graph times out.")]
        public UnityEvent      OnTimerTimeout      = new();

        /// <summary>On dialogue started.</summary>
        [Tooltip("Fired when a Run Dialogue node spawns a DialogueRunner. " +
                 "Wire a DialogueUI to bind to it (the demo HUD does this).")]
        public DialogueRunnerEvent OnDialogueStarted = new();


        /// <summary>Result.</summary>
        public QuestResult Result        { get; private set; } = QuestResult.InProgress;
        /// <summary>Failure reason.</summary>
        public string      FailureReason { get; private set; }

        /// <summary>Active objective.</summary>
        public ObjectiveInfo? ActiveObjective
        {
            get
            {
                foreach (var v in m_ActiveObjectives.Values) return v;
                return null;
            }
        }

        /// <summary>Active objectives.</summary>
        public IReadOnlyCollection<ObjectiveInfo> ActiveObjectives => m_ActiveObjectives.Values;


        private readonly Dictionary<string, ObjectiveInfo> m_ActiveObjectives  = new();
        private readonly Dictionary<string, bool?>         m_ObjectiveOutcomes = new();
        private readonly Dictionary<string, ObjectiveProgress> m_Progress     = new();

        private float m_TimerEndTime = -1f;


        /// <inheritdoc/>
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
            RegisterHandler(new AccumulateItemObjectiveHandler());
            RegisterHandler(new DeliverItemObjectiveHandler());
            RegisterHandler(new SpawnItemHandler());
            RegisterHandler(new RunDialogueHandler(this));
            RegisterHandler(new DebugLogNodeHandler());
            RegisterHandler(new FindObjectNodeHandler());
            RegisterHandler(new SpawnPrefabNodeHandler());
            RegisterHandler(new TimerNodeHandler());
            RegisterHandler(new ResetProgressNodeHandler());
        }


        /// <summary>Start quest.</summary>
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
            m_Progress.Clear();

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

        /// <summary>Abort quest.</summary>
        public void AbortQuest()
        {
            if (!IsRunning) return;
            Result = QuestResult.Aborted;
            StopGraph();
        }


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


        /// <summary>
        /// Creates (or replaces) the resettable progress counter for an objective
        /// node. Objective handlers call this at activation, then attach a composed
        /// gate predicate. Timer/Reset nodes act on the same counter by node guid.
        /// </summary>
        public ObjectiveProgress RegisterProgress(string nodeGuid, int required)
        {
            var p = new ObjectiveProgress { NodeGuid = nodeGuid, Required = required };
            m_Progress[nodeGuid] = p;
            return p;
        }

        /// <summary>Unregister progress.</summary>
        public void UnregisterProgress(string nodeGuid) => m_Progress.Remove(nodeGuid);

        /// <summary>Get progress.</summary>
        public ObjectiveProgress GetProgress(string nodeGuid)
            => m_Progress.TryGetValue(nodeGuid, out var p) ? p : null;

        /// <summary>Wipe an objective's accumulated progress (used by Reset Progress nodes).</summary>
        public void ResetObjectiveProgress(string nodeGuid)
        {
            if (m_Progress.TryGetValue(nodeGuid, out var p)) p.Reset();
        }

        /// <summary>Fires <see cref="OnTimerTimeout"/> (called by Timer node handlers).</summary>
        public void RaiseTimerTimeout() => OnTimerTimeout?.Invoke();


        /// <summary>True while a Timer node window is armed and the quest is running.</summary>
        public bool HasActiveTimer => IsRunning && m_TimerEndTime >= 0f;

        /// <summary>Seconds left in the current Timer window (0 once elapsed).</summary>
        public float TimerRemaining => Mathf.Max(0f, m_TimerEndTime - Time.time);

        /// <summary>
        /// Called by a Timer node each time it (re)arms, so the HUD can show a
        /// live countdown. The latest call wins (single shared window display).
        /// </summary>
        public void SetTimerWindow(float seconds)
            => m_TimerEndTime = Time.time + Mathf.Max(0f, seconds);


        /// <inheritdoc/>
        protected override void OnGraphStop()
        {
            if (Result == QuestResult.InProgress)
                Result = QuestResult.Aborted;

            m_ActiveObjectives.Clear();
            m_ObjectiveOutcomes.Clear();
            m_Progress.Clear();
            m_TimerEndTime = -1f;

            OnQuestEnded?.Invoke(Result);
        }


        private class ObjectiveHandler : IGraphNodeHandler
        {
            private readonly QuestRunner m_R;
            /// <inheritdoc/>
            public string NodeTypeId => QuestNodeRegistry.TypeObjective;
            /// <summary>Initializes a new instance of the <see cref="ObjectiveHandler"/> class.</summary>
            public ObjectiveHandler(QuestRunner r) => m_R = r;

            /// <inheritdoc/>
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
            /// <inheritdoc/>
            public string NodeTypeId => QuestNodeRegistry.TypeReward;
            /// <summary>Initializes a new instance of the <see cref="RewardHandler"/> class.</summary>
            public RewardHandler(QuestRunner r) => m_R = r;

            /// <inheritdoc/>
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
            /// <inheritdoc/>
            public string NodeTypeId => QuestNodeRegistry.TypeCompleteQuest;
            /// <summary>Initializes a new instance of the <see cref="CompleteQuestHandler"/> class.</summary>
            public CompleteQuestHandler(QuestRunner r) => m_R = r;

            /// <inheritdoc/>
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
            /// <inheritdoc/>
            public string NodeTypeId => QuestNodeRegistry.TypeFailQuest;
            /// <summary>Initializes a new instance of the <see cref="FailQuestHandler"/> class.</summary>
            public FailQuestHandler(QuestRunner r) => m_R = r;

            /// <inheritdoc/>
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
            /// <inheritdoc/>
            public string NodeTypeId => QuestNodeRegistry.TypeSpawnItem;

            /// <inheritdoc/>
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

        /// <summary>
        /// Runs a DialogueGraphAsset inline as a subgraph. Spawns a child
        /// <see cref="DialogueRunner"/>, fires <see cref="OnDialogueStarted"/> so a
        /// DialogueUI can bind to it, waits for the dialogue to end, then follows Out.
        /// The dialogue communicates back through Shared blackboard variables that a
        /// downstream Condition node reads (e.g. an accept/decline flag).
        /// </summary>
        private class RunDialogueHandler : IGraphNodeHandler
        {
            private readonly QuestRunner m_R;
            /// <inheritdoc/>
            public string NodeTypeId => QuestNodeRegistry.TypeRunDialogue;
            /// <summary>Initializes a new instance of the <see cref="RunDialogueHandler"/> class.</summary>
            public RunDialogueHandler(QuestRunner r) => m_R = r;

            /// <inheritdoc/>
            public IEnumerator Execute(NodeData node, GraphRunContext ctx)
            {
                DialogueGraphAsset dlg = null;
                var guid = ctx.GetLinkedGuid(node, "Graph");
                if (!string.IsNullOrEmpty(guid))
                    dlg = ctx.RuntimeBlackboard.GetVariable(guid)?.ObjectValue as DialogueGraphAsset;

                if (dlg == null)
                {
                    Debug.LogWarning("[QuestRunner] Run Dialogue: 'Graph' field is not linked to a " +
                                     "DialogueGraph variable. Skipping.", m_R);
                    ctx.Follow("Out");
                    yield break;
                }

                var go = new GameObject($"QuestDialogue:{dlg.name}");
                go.transform.SetParent(m_R.transform, false);
                var dialogue = go.AddComponent<DialogueRunner>();
                dialogue.Graph = dlg;

                m_R.OnDialogueStarted?.Invoke(dialogue);

                bool done = false;
                dialogue.OnGraphEnded.AddListener(() => done = true);
                dialogue.StartDialogue();

                yield return new WaitUntil(() => done || !m_R.IsRunning);

                if (go != null) Destroy(go);
                if (!m_R.IsRunning) yield break;

                ctx.Follow("Out");
            }
        }
    }


    [Serializable] public class ObjectiveEvent     : UnityEvent<ObjectiveInfo> { }
    [Serializable] public class RewardEvent        : UnityEvent<RewardInfo>    { }
    [Serializable] public class ResultEvent        : UnityEvent<QuestResult>   { }
    [Serializable] public class FailedEvent        : UnityEvent<string>        { }
    [Serializable] public class DialogueRunnerEvent : UnityEvent<DialogueRunner> { }
}
