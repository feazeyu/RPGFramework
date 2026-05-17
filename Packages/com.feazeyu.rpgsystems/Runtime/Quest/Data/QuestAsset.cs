using System;
using UnityEngine;
using UnityEngine.Events;

namespace QuestGraph.Runtime
{
    /// <summary>
    /// A lightweight alternative to <see cref="QuestGraphAsset"/>.
    ///
    /// Use this when a quest doesn't need graph-based logic — no
    /// branching objectives, no conditional flow, just "player did
    /// the thing → quest is complete." The simple quest is a plain
    /// ScriptableObject with a title, description, and a boolean
    /// state; game code calls <see cref="Complete"/> (or
    /// <see cref="Fail"/>) when the condition is satisfied.
    ///
    /// Also usable as a blackboard value, so chain graphs can
    /// reference simple quests the same way they reference graph
    /// quests. When a chain encounters a Quest Reference linked to
    /// a <see cref="QuestAsset"/> (rather than a
    /// <see cref="QuestGraphAsset"/>), no <see cref="QuestRunner"/>
    /// is spawned — completion is driven externally by the
    /// game calling <see cref="Complete"/>, which the chain runner
    /// observes via
    /// <see cref="QuestChainRunner.NotifyExternalQuestCompleted"/>.
    ///
    /// State lives on the ScriptableObject, so it persists across
    /// scene loads in the editor and at runtime. Call
    /// <see cref="Reset"/> in game-start logic if you want runs
    /// to begin from a clean slate.
    /// </summary>
    [CreateAssetMenu(
        menuName = "RPGFramework/Quest/Simple Quest",
        fileName = "NewSimpleQuest",
        order    = 3)]
    public class QuestAsset : ScriptableObject
    {
        [Header("Metadata")]
        [SerializeField] private string m_Title;
        [SerializeField, TextArea(2, 5)] private string m_Description;

        [Header("State")]
        [SerializeField] private QuestState m_State = QuestState.NotStarted;

        [Header("Events")]
        public UnityEvent OnStarted;
        public UnityEvent OnCompleted;
        public UnityEvent OnFailed;
        public UnityEvent OnReset;

        public string     Title       { get => m_Title;       set => m_Title = value; }
        public string     Description { get => m_Description; set => m_Description = value; }
        public QuestState State       => m_State;
        public bool       IsCompleted => m_State == QuestState.Completed;
        public bool       IsFailed    => m_State == QuestState.Failed;
        public bool       IsActive    => m_State == QuestState.Active;

        // ── Lifecycle ────────────────────────────────────────────────────────

        /// <summary>Transition to Active. No-op if already completed/failed.</summary>
        public void Start()
        {
            if (m_State != QuestState.NotStarted) return;
            m_State = QuestState.Active;
            OnStarted?.Invoke();
        }

        /// <summary>
        /// Mark the quest complete. Callable from any state — the call
        /// is idempotent if the quest is already Completed.
        /// </summary>
        public void Complete()
        {
            if (m_State == QuestState.Completed) return;
            m_State = QuestState.Completed;
            OnCompleted?.Invoke();
        }

        /// <summary>Mark the quest failed. Idempotent if already Failed.</summary>
        public void Fail()
        {
            if (m_State == QuestState.Failed) return;
            m_State = QuestState.Failed;
            OnFailed?.Invoke();
        }

        /// <summary>
        /// Clear state back to NotStarted. Intended for game-start
        /// cleanup so play sessions don't inherit stale ScriptableObject
        /// state from a previous run.
        /// </summary>
        public void Reset()
        {
            m_State = QuestState.NotStarted;
            OnReset?.Invoke();
        }
    }

    /// <summary>
    /// Lifecycle state for a <see cref="QuestAsset"/>.
    /// </summary>
    [Serializable]
    public enum QuestState
    {
        NotStarted = 0,
        Active     = 1,
        Completed  = 2,
        Failed     = 3,
    }
}
