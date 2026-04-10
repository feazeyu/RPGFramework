using System;
using UnityEngine;

namespace DialogueGraph.Runtime
{
    /// <summary>
    /// Passed to every IGraphNodeHandler.Execute() call.
    /// Provides field resolution, blackboard access, and the methods to advance
    /// or terminate the graph.
    ///
    /// Handlers MUST call Follow() or End() exactly once to continue execution.
    /// </summary>
    public class GraphRunContext
    {
        // ── Read-only references ──────────────────────────────────────────────

        public GraphRunner          Runner             { get; }
        public DialogueGraphAsset   Graph              { get; }
        public Blackboard           RuntimeBlackboard  { get; }

        // ── Wired by GraphRunner ──────────────────────────────────────────────

        internal Action<string> OnFollow;
        internal Action         OnEnd;

        internal GraphRunContext(GraphRunner runner, DialogueGraphAsset graph, Blackboard bb)
        {
            Runner            = runner;
            Graph             = graph;
            RuntimeBlackboard = bb;
        }

        // ── Graph control ─────────────────────────────────────────────────────

        /// <summary>Advance along the named output port of the current node.</summary>
        public void Follow(string portName) => OnFollow?.Invoke(portName);

        /// <summary>Terminate graph execution cleanly.</summary>
        public void End() => OnEnd?.Invoke();

        // ── Field resolution ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the string value of a field: prefers the linked blackboard
        /// variable's value, falls back to the inline value.
        /// </summary>
        public string ResolveString(NodeData node, string fieldName)
        {
            var f = GetField(node, fieldName);
            if (f == null) return string.Empty;

            if (!string.IsNullOrEmpty(f.LinkedVariableGuid))
            {
                var v = RuntimeBlackboard.GetVariable(f.LinkedVariableGuid);
                if (v != null) return v.ObjectValue?.ToString() ?? string.Empty;
            }

            return f.InlineValue ?? string.Empty;
        }

        /// <summary>
        /// Returns a Sprite from a linked blackboard variable.
        /// Inline values cannot reference Sprites at runtime.
        /// </summary>
        public Sprite ResolveSprite(NodeData node, string fieldName)
        {
            var f = GetField(node, fieldName);
            if (f == null || string.IsNullOrEmpty(f.LinkedVariableGuid)) return null;
            var v = RuntimeBlackboard.GetVariable(f.LinkedVariableGuid);
            return v?.ObjectValue as Sprite;
        }

        /// <summary>Returns the GUID of the blackboard variable linked to a field.</summary>
        public string GetLinkedGuid(NodeData node, string fieldName)
        {
            return GetField(node, fieldName)?.LinkedVariableGuid ?? string.Empty;
        }

        /// <summary>Returns the raw FieldData for a named field, or null.</summary>
        public FieldData GetField(NodeData node, string fieldName)
        {
            if (node.Fields == null) return null;
            foreach (var f in node.Fields)
                if (f.FieldName == fieldName) return f;
            return null;
        }

        // ── Blackboard accessors ──────────────────────────────────────────────

        public T GetVariable<T>(string variableName)
        {
            if (RuntimeBlackboard == null) return default;
            foreach (var v in Graph.Blackboard.Variables)
            {
                if (v.Name != variableName) continue;
                if (RuntimeBlackboard.TryGetValue<T>(v.Guid, out var val))
                    return val;
            }
            return default;
        }

        public void SetVariable<T>(string variableName, T value)
        {
            if (RuntimeBlackboard == null) return;
            foreach (var v in Graph.Blackboard.Variables)
            {
                if (v.Name != variableName) continue;
                RuntimeBlackboard.SetValue(v.Guid, value);
                return;
            }
        }
    }
}
