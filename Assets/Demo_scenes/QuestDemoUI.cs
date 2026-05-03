using System.Collections.Generic;
using UnityEngine;
using TMPro;
using QuestGraph.Runtime;

namespace QuestGraph.Demo
{
    public class QuestDemoUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text   m_ObjectivesText;
        [SerializeField] private TMP_Text   m_StatusText;
        [SerializeField] private QuestRunner m_QuestRunner;

        private readonly List<string> m_Lines = new();

        private void Start()
        {
            if (m_QuestRunner == null)
                m_QuestRunner = FindFirstObjectByType<QuestRunner>();
            if (m_QuestRunner == null) return;

            m_QuestRunner.OnObjectiveStarted.AddListener(OnObjectiveStarted);
            m_QuestRunner.OnObjectiveCompleted.AddListener(OnObjectiveCompleted);
            m_QuestRunner.OnObjectiveFailed.AddListener(OnObjectiveFailed);
            m_QuestRunner.OnQuestCompleted.AddListener(OnQuestCompleted);
            m_QuestRunner.OnQuestFailed.AddListener(OnQuestFailed);
        }

        private void OnDestroy()
        {
            if (m_QuestRunner == null) return;
            m_QuestRunner.OnObjectiveStarted.RemoveListener(OnObjectiveStarted);
            m_QuestRunner.OnObjectiveCompleted.RemoveListener(OnObjectiveCompleted);
            m_QuestRunner.OnObjectiveFailed.RemoveListener(OnObjectiveFailed);
            m_QuestRunner.OnQuestCompleted.RemoveListener(OnQuestCompleted);
            m_QuestRunner.OnQuestFailed.RemoveListener(OnQuestFailed);
        }

        private void OnObjectiveStarted(ObjectiveInfo info)
        {
            m_Lines.Add($"[ ] {info.Title}");
            Refresh();
        }

        private void OnObjectiveCompleted(ObjectiveInfo info)
        {
            ReplaceLine(info.Title, $"[✓] {info.Title}");
            Refresh();
        }

        private void OnObjectiveFailed(ObjectiveInfo info)
        {
            ReplaceLine(info.Title, $"[✗] {info.Title}");
            Refresh();
        }

        private void OnQuestCompleted()
        {
            if (m_StatusText) m_StatusText.text = "Quest Complete!";
        }

        private void OnQuestFailed(string reason)
        {
            if (m_StatusText) m_StatusText.text = $"Failed: {reason}";
        }

        private void ReplaceLine(string title, string replacement)
        {
            for (int i = 0; i < m_Lines.Count; i++)
                if (m_Lines[i].Contains(title)) { m_Lines[i] = replacement; return; }
        }

        private void Refresh()
        {
            if (m_ObjectivesText)
                m_ObjectivesText.text = string.Join("\n", m_Lines);
        }
    }
}
