using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using QuestGraph.Runtime;
using Feazeyu.RPGSystems.Dialogue;
using Feazeyu.RPGSystems.Inventory;
using Feazeyu.RPGSystems.Items;

namespace QuestGraph.Demo
{
    /// <summary>
    /// Single-MonoBehaviour quest HUD for the chain demo.
    ///
    /// Binds to the <see cref="QuestChainRunner"/> rather than a fixed
    /// <see cref="QuestRunner"/>, because chain quests spawn a fresh runner per
    /// quest at runtime. As each quest starts it latches onto its runner via
    /// <see cref="QuestChainRunner.GetActiveRunner"/> and mirrors that runner's
    /// objective events into a checklist, appending the live (current/required)
    /// counter for accumulation objectives (Kill Count, Accumulate Item, …)
    /// straight from <see cref="QuestRunner.GetProgress"/>.
    /// </summary>
    public class QuestDemoUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Chain runner to track. Auto-found in the scene if left empty.")]
        [SerializeField] private QuestChainRunner m_Chain;
        [SerializeField] private TMP_Text         m_TitleText;
        [SerializeField] private TMP_Text         m_ObjectivesText;
        [SerializeField] private TMP_Text         m_StatusText;
        [Tooltip("Optional. Shows the live countdown of the active quest's Timer node.")]
        [SerializeField] private TMP_Text         m_TimerText;
        [Tooltip("Optional. Bound to the DialogueRunner each quest's Run Dialogue node spawns.")]
        [SerializeField] private DialogueUI       m_DialogueUI;

        // One checklist entry per objective of the currently-active quest.
        private sealed class ObjLine
        {
            public string Guid;
            public string Title;
            public int    Status; // 0 = pending, 1 = complete, 2 = failed
        }

        private readonly List<ObjLine> m_Lines = new();
        private readonly StringBuilder m_Sb    = new();
        private QuestRunner m_Runner;          // current quest's runner (null for simple quests)
        private string      m_LastObjectives;  // cached render to avoid redundant TMP updates
        // Title of a graph quest that has started its graph (e.g. its offer dialogue)
        // but is not yet shown as active — committed once the player accepts (its first
        // objective starts). Null when nothing is pending.
        private string      m_PendingTitle;

        private void Start()
        {
            InventoryManager.Instance?.ReloadItems("Items");

            if (m_Chain == null)
                m_Chain = FindFirstObjectByType<QuestChainRunner>();
            if (m_Chain == null) return;

            m_Chain.OnQuestStarted.AddListener(OnQuestStarted);
            m_Chain.OnQuestCompleted.AddListener(OnQuestCompleted);
            m_Chain.OnQuestFailed.AddListener(OnQuestFailed);
            m_Chain.OnQuestAborted.AddListener(OnQuestAborted);
            m_Chain.OnChainCompleted.AddListener(OnChainCompleted);

            SetTitle("No active quest");
            SetStatus(string.Empty);
        }

        private void OnDestroy()
        {
            if (m_Chain != null)
            {
                m_Chain.OnQuestStarted.RemoveListener(OnQuestStarted);
                m_Chain.OnQuestCompleted.RemoveListener(OnQuestCompleted);
                m_Chain.OnQuestFailed.RemoveListener(OnQuestFailed);
                m_Chain.OnQuestAborted.RemoveListener(OnQuestAborted);
                m_Chain.OnChainCompleted.RemoveListener(OnChainCompleted);
            }
            Unbind();
        }

        // Progress counters are registered one frame after the objective starts
        // (the handler fires OnObjectiveStarted, then RegisterProgress), so poll
        // each frame and only push to TMP when the rendered text actually changes.
        private void Update()
        {
            if (m_Runner == null) return;
            Refresh();
            RefreshTimer();
        }

        private void RefreshTimer()
        {
            if (m_TimerText == null) return;
            if (m_Runner != null && m_Runner.HasActiveTimer)
            {
                float t = m_Runner.TimerRemaining;
                SetTimer($"⏱ {Mathf.CeilToInt(t)}s");
            }
            else SetTimer(string.Empty);
        }

        // ── Chain events ──────────────────────────────────────────────────────

        private void OnQuestStarted(QuestEntry entry)
        {
            Bind(m_Chain.GetActiveRunner(entry.ChainNodeGuid));   // null for simple (non-graph) quests
            m_Lines.Clear();
            m_LastObjectives = null;
            SetStatus(string.Empty);

            // A graph quest "starts" by running its graph, which is typically an offer
            // dialogue — the player hasn't accepted yet. Defer the title until the quest
            // actually begins (its first objective starts) so a declined offer never shows
            // as the active quest. Simple quests have no offer phase, so show immediately.
            if (m_Runner != null)
            {
                m_PendingTitle = entry.DisplayName;
                SetTitle("No active quest");
            }
            else
            {
                SetTitle(entry.DisplayName);
            }
            Refresh();
        }

        // Player declined the offer (or the quest was aborted): it returns to the
        // Available frontier with no completion/failure. Clear the log so it doesn't
        // keep showing a quest that was never accepted.
        private void OnQuestAborted(QuestEntry entry)
        {
            Unbind();
            SetTitle("No active quest");
            m_Lines.Clear();
            m_LastObjectives = null;
            Refresh();
        }

        private void OnQuestCompleted(QuestEntry entry)
        {
            Unbind();
            SetStatus($"Completed: {entry.DisplayName}");
        }

        private void OnQuestFailed(QuestEntry entry, string reason)
        {
            Unbind();
            SetStatus(string.IsNullOrEmpty(reason)
                ? $"Failed: {entry.DisplayName}"
                : $"Failed: {entry.DisplayName} ({reason})");
        }

        private void OnChainCompleted()
        {
            SetTitle("All quests complete!");
            SetStatus(string.Empty);
            m_Lines.Clear();
            m_LastObjectives = null;
            Refresh();
        }

        // ── Active runner binding ─────────────────────────────────────────────

        private void Bind(QuestRunner runner)
        {
            Unbind();
            m_Runner = runner;
            if (m_Runner == null) return;
            m_Runner.OnObjectiveStarted.AddListener(OnObjectiveStarted);
            m_Runner.OnObjectiveCompleted.AddListener(OnObjectiveCompleted);
            m_Runner.OnObjectiveFailed.AddListener(OnObjectiveFailed);
            m_Runner.OnRewardGranted.AddListener(OnRewardGranted);
            m_Runner.OnDialogueStarted.AddListener(OnQuestDialogueStarted);
        }

        private void Unbind()
        {
            m_PendingTitle = null;
            if (m_Runner == null) return;
            m_Runner.OnObjectiveStarted.RemoveListener(OnObjectiveStarted);
            m_Runner.OnObjectiveCompleted.RemoveListener(OnObjectiveCompleted);
            m_Runner.OnObjectiveFailed.RemoveListener(OnObjectiveFailed);
            m_Runner.OnRewardGranted.RemoveListener(OnRewardGranted);
            m_Runner.OnDialogueStarted.RemoveListener(OnQuestDialogueStarted);
            m_Runner = null;
            SetTimer(string.Empty);
        }

        // The quest graph's Run Dialogue node spawns a DialogueRunner; bind the
        // shared DialogueUI to it so the offer/accept-decline dialogue is shown.
        private void OnQuestDialogueStarted(DialogueRunner dialogue)
        {
            if (m_DialogueUI != null && dialogue != null)
                m_DialogueUI.Bind(dialogue);
        }

        // ── Runner (objective) events ─────────────────────────────────────────

        private void OnObjectiveStarted(ObjectiveInfo info)
        {
            // First objective of a graph quest = the player has accepted; commit its title.
            if (!string.IsNullOrEmpty(m_PendingTitle))
            {
                SetTitle(m_PendingTitle);
                m_PendingTitle = null;
            }

            if (Find(info.NodeGuid) != null) return;
            m_Lines.Add(new ObjLine
            {
                Guid   = info.NodeGuid,
                Title  = string.IsNullOrEmpty(info.Title) ? "Objective" : info.Title,
                Status = 0,
            });
            Refresh();
        }

        private void OnObjectiveCompleted(ObjectiveInfo info) => SetStatusFor(info.NodeGuid, 1);
        private void OnObjectiveFailed(ObjectiveInfo info)    => SetStatusFor(info.NodeGuid, 2);

        private void OnRewardGranted(RewardInfo reward)
        {
            if (reward.item is ItemInfo info)
                SetStatus($"Received: {info.Name}!");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private ObjLine Find(string guid)
        {
            foreach (var l in m_Lines) if (l.Guid == guid) return l;
            return null;
        }

        private void SetStatusFor(string guid, int status)
        {
            var line = Find(guid);
            if (line != null) { line.Status = status; Refresh(); }
        }

        private void Refresh()
        {
            m_Sb.Clear();
            for (int i = 0; i < m_Lines.Count; i++)
            {
                var l   = m_Lines[i];
                var box = l.Status == 1 ? "[v]" : l.Status == 2 ? "[x]" : "[ ]";
                m_Sb.Append(box).Append(' ').Append(l.Title);

                // Append a live counter for accumulation objectives that expose one.
                if (l.Status == 0 && m_Runner != null)
                {
                    var p = m_Runner.GetProgress(l.Guid);
                    if (p != null && p.Required > 1)
                        m_Sb.Append(" (").Append(Mathf.Clamp(p.Current, 0, p.Required))
                            .Append('/').Append(p.Required).Append(')');
                }
                if (i < m_Lines.Count - 1) m_Sb.Append('\n');
            }

            var rendered = m_Sb.ToString();
            if (rendered == m_LastObjectives) return;
            m_LastObjectives = rendered;
            if (m_ObjectivesText) m_ObjectivesText.text = rendered;
        }

        private void SetTitle(string text)  { if (m_TitleText)  m_TitleText.text  = text; }
        private void SetStatus(string text) { if (m_StatusText) m_StatusText.text = text; }
        private void SetTimer(string text)  { if (m_TimerText)  m_TimerText.text  = text; }
    }
}
