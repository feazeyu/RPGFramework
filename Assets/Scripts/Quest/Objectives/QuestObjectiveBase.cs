using UnityEngine;
using QuestGraph.Runtime;

namespace QuestGraph.Objectives
{
    /// <summary>
    /// Base class for objective driver components. Place one (or more) of
    /// these on the same GameObject as a QuestRunner. Each driver watches
    /// for the Objective node whose Title matches <see cref="objectiveTitle"/>,
    /// starts tracking its condition, and calls Complete() / Fail() when done.
    ///
    /// Multiple drivers can live on the same quest object — each matches
    /// independently by title, so parallel or sequential objectives all work.
    /// </summary>
    public abstract class QuestObjectiveBase : MonoBehaviour
    {
        [Tooltip("Exact match against the Title field on the Objective node in the graph.")]
        [SerializeField] public string objectiveTitle;

        [Tooltip("The QuestRunner to drive. Auto-found on parent if left empty.")]
        [SerializeField] protected QuestRunner questRunner;

        protected string m_ActiveNodeGuid;
        protected bool   m_IsActive;

        protected virtual void Awake()
        {
            if (questRunner == null)
                questRunner = GetComponentInParent<QuestRunner>();
        }

        protected virtual void OnEnable()
        {
            if (questRunner == null) return;
            questRunner.OnObjectiveStarted.AddListener(HandleObjectiveStarted);
            questRunner.OnObjectiveCompleted.AddListener(HandleObjectiveEnded);
            questRunner.OnObjectiveFailed.AddListener(HandleObjectiveEnded);
        }

        protected virtual void OnDisable()
        {
            if (questRunner == null) return;
            questRunner.OnObjectiveStarted.RemoveListener(HandleObjectiveStarted);
            questRunner.OnObjectiveCompleted.RemoveListener(HandleObjectiveEnded);
            questRunner.OnObjectiveFailed.RemoveListener(HandleObjectiveEnded);
        }

        private void HandleObjectiveStarted(ObjectiveInfo info)
        {
            if (info.Title != objectiveTitle || m_IsActive) return;
            m_ActiveNodeGuid = info.NodeGuid;
            m_IsActive       = true;
            StartTracking(info);
        }

        private void HandleObjectiveEnded(ObjectiveInfo info)
        {
            if (!m_IsActive || info.NodeGuid != m_ActiveNodeGuid) return;
            m_IsActive       = false;
            m_ActiveNodeGuid = null;
            StopTracking();
        }

        // ── Called by subclasses ──────────────────────────────────────────────

        protected void Complete()
        {
            if (!m_IsActive) return;
            m_IsActive       = false;
            var guid         = m_ActiveNodeGuid;
            m_ActiveNodeGuid = null;
            StopTracking();
            questRunner?.CompleteObjective(guid);
        }

        protected void Fail()
        {
            if (!m_IsActive) return;
            m_IsActive       = false;
            var guid         = m_ActiveNodeGuid;
            m_ActiveNodeGuid = null;
            StopTracking();
            questRunner?.FailObjective(guid);
        }

        // ── Abstract interface ────────────────────────────────────────────────

        protected abstract void StartTracking(ObjectiveInfo info);
        protected virtual  void StopTracking()  { }
    }
}
