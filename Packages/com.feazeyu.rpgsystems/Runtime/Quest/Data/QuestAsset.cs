using System;
using System.Collections.Generic;
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
    /// State lives on the ScriptableObject (it is the shared,
    /// inspector-wired identity that both the chain runner and external
    /// game code mutate, so it cannot be cloned per-runner the way a
    /// <see cref="Blackboard"/> is). Because mutating a serialized asset
    /// during play leaves the in-memory copy dirty after exiting play
    /// mode, the runtime state (<see cref="m_State"/>) is reset to
    /// <see cref="QuestState.NotStarted"/> automatically at the start of
    /// every play session — see <see cref="ResetRuntimeStateOnPlay"/> and
    /// <see cref="OnEnable"/> — so editor runs always begin from a clean
    /// slate without any explicit game-start cleanup. <see cref="Reset"/>
    /// remains available for manually re-arming a quest mid-session.
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

        /// <summary>On started.</summary>
        [Header("Events")]
        public UnityEvent OnStarted;
        /// <summary>On completed.</summary>
        public UnityEvent OnCompleted;
        /// <summary>On failed.</summary>
        public UnityEvent OnFailed;
        /// <summary>On reset.</summary>
        public UnityEvent OnReset;

        /// <summary>Title.</summary>
        public string     Title       { get => m_Title;       set => m_Title = value; }
        /// <summary>Description.</summary>
        public string     Description { get => m_Description; set => m_Description = value; }
        /// <summary>State.</summary>
        public QuestState State       => m_State;
        /// <summary>Is completed.</summary>
        public bool       IsCompleted => m_State == QuestState.Completed;
        /// <summary>Is failed.</summary>
        public bool       IsFailed    => m_State == QuestState.Failed;
        /// <summary>Is active.</summary>
        public bool       IsActive    => m_State == QuestState.Active;


        private static readonly HashSet<QuestAsset> s_Live = new HashSet<QuestAsset>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeStateOnPlay()
        {
            foreach (var q in s_Live)
                if (q != null) q.m_State = QuestState.NotStarted;
        }

        private void OnEnable()
        {
            s_Live.Add(this);
            m_State = QuestState.NotStarted;
        }

        private void OnDisable() => s_Live.Remove(this);


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
