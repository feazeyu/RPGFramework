using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DialogueGraph.Runtime
{
    /// <summary>
    /// Attach to any GameObject near an NPC or interactable object.
    /// Assign a DialogueGraphAsset, hook up the UI events, then call
    /// StartDialogue() — or enable AutoStartOnInteract to have it start
    /// when the player presses the interact key while inside the trigger.
    ///
    /// ── Minimal setup ────────────────────────────────────────────────────
    ///   1. Add this component to a GameObject.
    ///   2. Assign a DialogueGraphAsset to the Graph field.
    ///   3. Subscribe to OnDialogueLine to drive your UI:
    ///        runner.OnDialogueLine.AddListener((speaker, text, portrait) => ...);
    ///   4. Subscribe to OnChoicesPresented to show choice buttons:
    ///        runner.OnChoicesPresented.AddListener(choices => ...);
    ///      Then call runner.SelectChoice(index) when the player picks one.
    ///   5. Call runner.Advance() when the player presses the "next" button.
    ///
    /// ── Blackboard / exposed variables ───────────────────────────────────
    ///   The asset's Blackboard is cloned at dialogue start so per-run state
    ///   doesn't bleed into the asset.  Exposed variables are accessible via
    ///   GetVariable<T>(name) / SetVariable<T>(name, value).
    /// </summary>
    public class DialogueRunner : MonoBehaviour
    {
        // ── Inspector fields ──────────────────────────────────────────────────

        [Tooltip("The dialogue graph asset to run.")]
        public DialogueGraphAsset Graph;

        // ── Events (wire these up in the Inspector or via code) ───────────────

        /// <summary>Fired when a Dialogue Line node is reached.</summary>
        [Header("Events")]
        public DialogueLineEvent OnDialogueLine;

        /// <summary>Fired when a Choice Branch node is reached. Arg = list of choice texts.</summary>
        public ChoicesEvent OnChoicesPresented;

        /// <summary>Fired when the dialogue ends (End node or no further edges).</summary>
        public UnityEvent OnDialogueEnded;

        /// <summary>Fired when dialogue starts.</summary>
        public UnityEvent OnDialogueStarted;

        /// <summary>Fired when a variable is set via a SetVariable node.</summary>
        public VariableSetEvent OnVariableSet;

        /// <summary>Fired when a TriggerEvent node is executed. Arg = event channel field value.</summary>
        public StringEvent OnEventTriggered;

        /// <summary>Fired when the player enters/leaves the trigger zone (for prompt UI).</summary>
        public BoolEvent OnPlayerInRange;

        // ── State ─────────────────────────────────────────────────────────────

        /// <summary>True while a dialogue is actively running.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>True when waiting for the player to pick a choice.</summary>
        public bool IsWaitingForChoice { get; private set; }

        private Blackboard        m_RuntimeBlackboard;
        private NodeData          m_CurrentNode;
        private bool              m_WaitingForAdvance;

        // Pending choices: index → port name
        private readonly List<(string text, string portName)> m_Choices
            = new List<(string, string)>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Starts the dialogue from the Start node.</summary>
        public void StartDialogue()
        {
            if (Graph == null)
            {
                Debug.LogWarning($"[DialogueRunner] No graph assigned on {name}.", this);
                return;
            }
            if (IsRunning)
            {
                Debug.LogWarning($"[DialogueRunner] Already running on {name}.", this);
                return;
            }

            // Clone the blackboard so this run has isolated state.
            m_RuntimeBlackboard = Graph.Blackboard.Clone(Graph.Blackboard);

            IsRunning          = true;
            m_WaitingForAdvance = false;
            IsWaitingForChoice  = false;

            OnDialogueStarted?.Invoke();

            var startNode = Graph.FindEntryNode();
            if (startNode == null)
            {
                Debug.LogWarning($"[DialogueRunner] Graph '{Graph.name}' has no Start node.", this);
                EndDialogue();
                return;
            }

            AdvanceToNode(startNode);
        }

        /// <summary>
        /// Call this when the player presses "next" / confirms to advance
        /// past a Dialogue Line.
        /// </summary>
        public void Advance()
        {
            if (!IsRunning || IsWaitingForChoice) return;
            m_WaitingForAdvance = false;
        }

        /// <summary>
        /// Call this when the player picks a choice.
        /// Index corresponds to the order in the list given by OnChoicesPresented.
        /// </summary>
        public void SelectChoice(int index)
        {
            if (!IsRunning || !IsWaitingForChoice) return;
            if (index < 0 || index >= m_Choices.Count) return;

            IsWaitingForChoice = false;
            var portName = m_Choices[index].portName;
            m_Choices.Clear();

            var next = GetNodeConnectedToOutputPort(m_CurrentNode, portName);
            if (next != null)
                AdvanceToNode(next);
            else
                EndDialogue();
        }

        /// <summary>Stops the dialogue immediately.</summary>
        public void StopDialogue()
        {
            if (!IsRunning) return;
            EndDialogue();
        }

        // ── Blackboard accessors ──────────────────────────────────────────────

        public T GetVariable<T>(string variableName)
        {
            if (m_RuntimeBlackboard == null) return default;
            var vars = Graph.Blackboard.Variables;
            foreach (var v in vars)
            {
                if (v.Name != variableName) continue;
                if (m_RuntimeBlackboard.TryGetValue<T>(v.Guid, out var val))
                    return val;
            }
            return default;
        }

        public void SetVariable<T>(string variableName, T value)
        {
            if (m_RuntimeBlackboard == null) return;
            var vars = Graph.Blackboard.Variables;
            foreach (var v in vars)
            {
                if (v.Name != variableName) continue;
                m_RuntimeBlackboard.SetValue(v.Guid, value);
                return;
            }
        }

        // ── Graph traversal ───────────────────────────────────────────────────

        private void AdvanceToNode(NodeData node)
        {
            m_CurrentNode = node;
            StartCoroutine(ProcessNode(node));
        }

        private IEnumerator ProcessNode(NodeData node)
        {
            switch (node.NodeType)
            {
                case NodeRegistry.TypeStart:
                    // Pass straight through to the next node.
                    Debug.Log("Startnode");
                    yield return null;
                    FollowOutputPort(node, "Out");
                    break;

                case NodeRegistry.TypeEnd:
                    Debug.Log("Endnode");
                    EndDialogue();
                    break;

                case NodeRegistry.TypeDialogueLine:
                    Debug.Log("DialogueLine");
                    yield return ProcessDialogueLine(node);
                    break;

                case NodeRegistry.TypeChoiceBranch:
                    Debug.Log("ChoiceBranch");
                    yield return ProcessChoiceBranch(node);
                    break;

                case NodeRegistry.TypeCondition:
                    Debug.Log("Condition");
                    ProcessCondition(node);
                    break;

                case NodeRegistry.TypeSetVariable:
                    Debug.Log("Setvar");
                    ProcessSetVariable(node);
                    FollowOutputPort(node, "Out");
                    break;

                case NodeRegistry.TypeTriggerEvent:
                    ProcessTriggerEvent(node);
                    FollowOutputPort(node, "Out");
                    break;

                case NodeRegistry.TypeSequence:
                case NodeRegistry.TypeSelector:
                    // Treat these as simple pass-throughs — follow Out port.
                    FollowOutputPort(node, "Out");
                    break;

                case NodeRegistry.TypeRunSubgraph:
                    yield return ProcessRunSubgraph(node);
                    break;

                default:
                    // Unknown node — skip forward.
                    Debug.LogWarning($"[DialogueRunner] Unknown node type '{node.NodeType}', skipping.");
                    FollowOutputPort(node, "Out");
                    break;
            }
        }

        // ── Node processors ───────────────────────────────────────────────────

        private IEnumerator ProcessDialogueLine(NodeData node)
        {
            var speaker  = ResolveFieldString(node, "Speaker");
            var text     = ResolveFieldString(node, "Text");
            var portrait = ResolveFieldSprite(node, "Portrait");

            OnDialogueLine?.Invoke(speaker, text, portrait);

            // Wait until Advance() clears m_WaitingForAdvance.
            m_WaitingForAdvance = true;
            yield return new WaitUntil(() => !m_WaitingForAdvance);

            FollowOutputPort(node, "Out");
        }

        private IEnumerator ProcessChoiceBranch(NodeData node)
        {
            m_Choices.Clear();

            // Collect every output port whose name starts with "Choice ".
            foreach (var port in node.Ports)
            {
                if (port.Direction != PortDirection.Output) continue;
                if (!port.PortName.StartsWith("Choice ")) continue;

                // Find the matching text field: port "Choice 1" → field "Choice 1 Text"
                var fieldName = port.PortName + " Text";
                var choiceText = ResolveFieldString(node, fieldName);
                if (string.IsNullOrEmpty(choiceText))
                    choiceText = port.PortName; // fallback to port name

                m_Choices.Add((choiceText, port.PortName));
            }

            if (m_Choices.Count == 0)
            {
                // No choices — just move on.
                FollowOutputPort(node, "Out");
                yield break;
            }

            var texts = new List<string>();
            foreach (var c in m_Choices) texts.Add(c.text);

            IsWaitingForChoice = true;
            OnChoicesPresented?.Invoke(texts);

            yield return new WaitUntil(() => !IsWaitingForChoice);
        }

        private void ProcessCondition(NodeData node)
        {
            var variableGuid = GetLinkedGuid(node, "Variable");
            bool result = false;

            if (!string.IsNullOrEmpty(variableGuid))
            {
                var bbVar = m_RuntimeBlackboard.GetVariable(variableGuid);
                if (bbVar != null)
                {
                    var op  = ResolveFieldString(node, "Operator");
                    var val = ResolveFieldString(node, "Value");
                    result  = EvaluateCondition(bbVar.ObjectValue, op, val);
                }
            }

            FollowOutputPort(node, result ? "True" : "False");
        }

        private void ProcessSetVariable(NodeData node)
        {
            var variableGuid = GetLinkedGuid(node, "Variable");
            if (string.IsNullOrEmpty(variableGuid)) return;

            var bbVar = m_RuntimeBlackboard.GetVariable(variableGuid);
            if (bbVar == null) return;

            var valueStr = ResolveFieldString(node, "Value");

            // Parse valueStr to the variable's actual type.
            try
            {
                var converted = Convert.ChangeType(valueStr, bbVar.ValueType);
                bbVar.ObjectValue = converted;
                OnVariableSet?.Invoke(bbVar.Name, valueStr);
            }
            catch
            {
                Debug.LogWarning($"[DialogueRunner] Could not convert '{valueStr}' to {bbVar.ValueType} for variable '{bbVar.Name}'.");
            }
        }

        private void ProcessTriggerEvent(NodeData node)
        {
            var channelValue = ResolveFieldString(node, "Event Channel");
            OnEventTriggered?.Invoke(channelValue);
        }

        private IEnumerator ProcessRunSubgraph(NodeData node)
        {
            // Nested subgraph: instantiate a temporary runner, run it, wait for it.
            var fieldData = GetField(node, "Graph");
            DialogueGraphAsset subAsset = null;

            if (fieldData != null && !string.IsNullOrEmpty(fieldData.LinkedVariableGuid))
            {
                // If linked to a blackboard variable that holds an asset reference,
                // we can't easily get it at runtime from string — log a note.
                Debug.Log("[DialogueRunner] RunSubgraph linked to blackboard variable — not supported at runtime yet.");
            }

            // For now, fall through if no asset is resolvable.
            if (subAsset == null)
            {
                Debug.LogWarning("[DialogueRunner] RunSubgraph: no asset resolved, skipping.");
                FollowOutputPort(node, "Out");
                yield break;
            }

            var subGO     = new GameObject("SubgraphRunner");
            var subRunner = subGO.AddComponent<DialogueRunner>();
            subRunner.Graph                = subAsset;

            bool done = false;
            subRunner.OnDialogueEnded.AddListener(() => done = true);

            // Pipe sub-runner events up to this runner.
            subRunner.OnDialogueLine.AddListener((s, t, p) => OnDialogueLine?.Invoke(s, t, p));
            subRunner.OnChoicesPresented.AddListener(c => OnChoicesPresented?.Invoke(c));

            subRunner.StartDialogue();
            yield return new WaitUntil(() => done);

            Destroy(subGO);
            FollowOutputPort(node, "Out");
        }

        // ── Navigation helpers ────────────────────────────────────────────────

        private void FollowOutputPort(NodeData node, string portName)
        {
            var next = GetNodeConnectedToOutputPort(node, portName);
            if (next != null)
                AdvanceToNode(next);
            else
                EndDialogue();
        }

        private NodeData GetNodeConnectedToOutputPort(NodeData node, string portName)
        {
            foreach (var edge in Graph.Edges)
            {
                if (edge.OutputNodeGuid == node.Guid && edge.OutputPortName == portName)
                    return Graph.GetNode(edge.InputNodeGuid);
            }
            return null;
        }

        private void EndDialogue()
        {
            IsRunning          = false;
            m_WaitingForAdvance = false;
            IsWaitingForChoice  = false;
            m_CurrentNode       = null;
            OnDialogueEnded?.Invoke();
        }

        // ── Field resolution ──────────────────────────────────────────────────

        /// <summary>
        /// Gets the string value for a field: uses the linked blackboard variable
        /// if one is set, otherwise falls back to the inline value.
        /// </summary>
        private string ResolveFieldString(NodeData node, string fieldName)
        {
            var f = GetField(node, fieldName);
            if (f == null) return string.Empty;

            if (!string.IsNullOrEmpty(f.LinkedVariableGuid))
            {
                var v = m_RuntimeBlackboard.GetVariable(f.LinkedVariableGuid);
                if (v != null) return v.ObjectValue?.ToString() ?? string.Empty;
            }

            return f.InlineValue ?? string.Empty;
        }

        private Sprite ResolveFieldSprite(NodeData node, string fieldName)
        {
            // Sprites can only be assigned through the blackboard at runtime.
            var f = GetField(node, fieldName);
            if (f == null || string.IsNullOrEmpty(f.LinkedVariableGuid)) return null;

            var v = m_RuntimeBlackboard.GetVariable(f.LinkedVariableGuid);
            return v?.ObjectValue as Sprite;
        }

        private string GetLinkedGuid(NodeData node, string fieldName)
        {
            var f = GetField(node, fieldName);
            return f?.LinkedVariableGuid ?? string.Empty;
        }

        private static FieldData GetField(NodeData node, string fieldName)
        {
            if (node.Fields == null) return null;
            foreach (var f in node.Fields)
                if (f.FieldName == fieldName) return f;
            return null;
        }

        // ── Condition evaluation ──────────────────────────────────────────────

        private static bool EvaluateCondition(object lhs, string op, string rhsStr)
        {
            if (lhs == null) return false;

            // Try numeric comparison first.
            if (double.TryParse(lhs.ToString(), out double lhsNum) &&
                double.TryParse(rhsStr,          out double rhsNum))
            {
                return op switch
                {
                    "==" => lhsNum == rhsNum,
                    "!=" => lhsNum != rhsNum,
                    ">"  => lhsNum >  rhsNum,
                    ">=" => lhsNum >= rhsNum,
                    "<"  => lhsNum <  rhsNum,
                    "<=" => lhsNum <= rhsNum,
                    _    => false,
                };
            }

            // Boolean comparison.
            if (lhs is bool lhsBool &&
                bool.TryParse(rhsStr, out bool rhsBool))
            {
                return op switch
                {
                    "==" => lhsBool == rhsBool,
                    "!=" => lhsBool != rhsBool,
                    _    => false,
                };
            }

            // String fallback.
            var lhsStr = lhs.ToString();
            return op switch
            {
                "==" => string.Equals(lhsStr, rhsStr, StringComparison.Ordinal),
                "!=" => !string.Equals(lhsStr, rhsStr, StringComparison.Ordinal),
                _    => false,
            };
        }
    }

    // ── Serialisable UnityEvent types ─────────────────────────────────────────

    [Serializable]
    public class DialogueLineEvent : UnityEvent<string, string, Sprite> { }
    // args: speaker, text, portrait

    [Serializable]
    public class ChoicesEvent : UnityEvent<List<string>> { }

    [Serializable]
    public class VariableSetEvent : UnityEvent<string, string> { }
    // args: variableName, newValueAsString

    [Serializable]
    public class StringEvent : UnityEvent<string> { }

    [Serializable]
    public class BoolEvent : UnityEvent<bool> { }
}
