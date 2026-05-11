using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DialogueGraph.Runtime
{
    /// <summary>
    /// Subclass of GraphRunner that handles dialogue-specific node types:
    ///   • DialogueLine  — fires OnDialogueLine, waits for Advance()
    ///   • ChoiceBranch  — fires OnChoicesPresented, waits for SelectChoice()
    ///   • TriggerEvent  — fires OnEventTriggered
    ///   • WaitForEvent  — stubbed (logs a warning)
    ///
    /// All structural nodes (Start, End, Condition, SetVariable, Sequence,
    /// Selector, RunSubgraph) are handled by the base GraphRunner.
    ///
    /// ── Minimal setup ────────────────────────────────────────────────────
    ///   1. Add this component to the NPC/interactable GameObject.
    ///   2. Assign a DialogueGraphAsset to Graph.
    ///   3. Wire OnDialogueLine to your UI: (speaker, text, portrait) => ...
    ///   4. Wire OnChoicesPresented to your choice buttons: choices => ...
    ///      Then call runner.SelectChoice(index) on button press.
    ///   5. Call runner.Advance() on "next line" input.
    ///   6. Call runner.StartDialogue() to begin.
    /// </summary>
    public class DialogueRunner : GraphRunner
    {
        // ── Dialogue events ───────────────────────────────────────────────────

        [Header("Dialogue Events")]
        public DialogueLineEvent OnDialogueLine;
        public ChoicesEvent      OnChoicesPresented;
        public VariableSetEvent  OnVariableSet;
        public StringEvent       OnEventTriggered;

        // ── State ─────────────────────────────────────────────────────────────

        public bool IsWaitingForChoice { get; private set; }

        private bool m_WaitingForAdvance;
        private string m_SelectedPortName;
        private readonly List<(string text, string portName)> m_Choices
            = new List<(string, string)>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            RegisterHandler(new DialogueLineHandler(this));
            RegisterHandler(new ChoiceBranchHandler(this));
            RegisterHandler(new TriggerEventHandler(this));
            RegisterHandler(new WaitForEventHandler());
            RegisterHandler(new GiveItemNodeHandler());
            RegisterHandler(new DebugLogNodeHandler());
            RegisterHandler(new FindObjectNodeHandler());
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void StartDialogue() => StartGraph();

        public void Advance()
        {
            if (!IsRunning || IsWaitingForChoice) return;
            m_WaitingForAdvance = false;
        }

        public void SelectChoice(int index)
        {
            if (!IsRunning || !IsWaitingForChoice) return;
            if (index < 0 || index >= m_Choices.Count) return;

            m_SelectedPortName = m_Choices[index].portName;
            IsWaitingForChoice = false;
        }

        // ── GraphRunner overrides ─────────────────────────────────────────────

        protected override void OnGraphStop()
        {
            m_WaitingForAdvance = false;
            IsWaitingForChoice  = false;
            m_SelectedPortName  = null;
            m_Choices.Clear();
        }

        protected override void OnVariableChanged(string name, string value)
            => OnVariableSet?.Invoke(name, value);

        protected override GraphRunner CreateSubRunner(GameObject go)
            => go.AddComponent<DialogueRunner>();

        // ── Inner handler classes ─────────────────────────────────────────────

        private class DialogueLineHandler : IGraphNodeHandler
        {
            private readonly DialogueRunner m_R;
            public string NodeTypeId => NodeRegistry.TypeDialogueLine;
            public DialogueLineHandler(DialogueRunner r) => m_R = r;

            public IEnumerator Execute(NodeData node, GraphRunContext ctx)
            {
                m_R.OnDialogueLine?.Invoke(
                    ctx.ResolveString(node, "Speaker"),
                    ctx.ResolveString(node, "Text"),
                    ctx.ResolveSprite(node,  "Portrait"));

                m_R.m_WaitingForAdvance = true;
                yield return new WaitUntil(() => !m_R.m_WaitingForAdvance);
                ctx.Follow("Out");
            }
        }

        private class ChoiceBranchHandler : IGraphNodeHandler
        {
            private readonly DialogueRunner m_R;
            public string NodeTypeId => NodeRegistry.TypeChoiceBranch;
            public ChoiceBranchHandler(DialogueRunner r) => m_R = r;

            public IEnumerator Execute(NodeData node, GraphRunContext ctx)
            {
                m_R.m_Choices.Clear();

                foreach (var port in node.Ports)
                {
                    if (port.Direction != PortDirection.Output) continue;
                    if (!port.PortName.StartsWith("Choice ")) continue;
                    var text = ctx.ResolveString(node, port.PortName + " Text");
                    if (string.IsNullOrEmpty(text)) text = port.PortName;
                    m_R.m_Choices.Add((text, port.PortName));
                }

                if (m_R.m_Choices.Count == 0) { ctx.Follow("Out"); yield break; }

                var texts = new List<string>();
                foreach (var c in m_R.m_Choices) texts.Add(c.text);

                m_R.m_SelectedPortName = null;
                m_R.IsWaitingForChoice = true;
                m_R.OnChoicesPresented?.Invoke(texts);

                yield return new WaitUntil(() => !m_R.IsWaitingForChoice);
                var selected = m_R.m_SelectedPortName;
                m_R.m_Choices.Clear();
                ctx.Follow(selected);
            }
        }

        private class TriggerEventHandler : IGraphNodeHandler
        {
            private readonly DialogueRunner m_R;
            public string NodeTypeId => NodeRegistry.TypeTriggerEvent;
            public TriggerEventHandler(DialogueRunner r) => m_R = r;

            public IEnumerator Execute(NodeData node, GraphRunContext ctx)
            {
                m_R.OnEventTriggered?.Invoke(ctx.ResolveString(node, "Event Channel"));
                ctx.Follow("Out");
                yield break;
            }
        }

        private class WaitForEventHandler : IGraphNodeHandler
        {
            public string NodeTypeId => NodeRegistry.TypeWaitForEvent;

            public IEnumerator Execute(NodeData node, GraphRunContext ctx)
            {
                Debug.LogWarning("[DialogueRunner] WaitForEvent is not implemented. Skipping.");
                ctx.Follow("Out");
                yield break;
            }
        }
    }

    // ── Serialisable UnityEvent types ─────────────────────────────────────────

    [Serializable] public class DialogueLineEvent : UnityEvent<string, string, Sprite> { }
    [Serializable] public class ChoicesEvent       : UnityEvent<List<string>>           { }
    [Serializable] public class VariableSetEvent   : UnityEvent<string, string>         { }
    [Serializable] public class StringEvent        : UnityEvent<string>                 { }
    [Serializable] public class BoolEvent          : UnityEvent<bool>                   { }
}
