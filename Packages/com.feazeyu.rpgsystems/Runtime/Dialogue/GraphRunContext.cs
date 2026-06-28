using System;
using System.Text;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
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

        /// <summary>Runner.</summary>
        public GraphRunner          Runner             { get; }
        /// <summary>Graph.</summary>
        public GraphAsset   Graph              { get; }
        /// <summary>Runtime blackboard.</summary>
        public Blackboard           RuntimeBlackboard  { get; }


        internal Action<string> OnFollow;
        internal Action<string> OnFork;
        internal Action         OnEnd;

        internal GraphRunContext(GraphRunner runner, GraphAsset graph, Blackboard bb)
        {
            Runner            = runner;
            Graph             = graph;
            RuntimeBlackboard = bb;
        }


        /// <summary>
        /// Advance this flow token along the named output port. Spawns one token
        /// per edge connected to that port (auto-fork), then this token is done.
        /// Calling Follow or End more than once per node step is ignored.
        /// </summary>
        public void Follow(string portName) => OnFollow?.Invoke(portName);

        /// <summary>
        /// Spawn new flow token(s) down the named output port <b>without</b> ending
        /// this token. Used by long-lived nodes (e.g. an auto-restarting Timer) that
        /// emit on an output repeatedly while continuing to run.
        /// </summary>
        public void Fork(string portName) => OnFork?.Invoke(portName);

        /// <summary>Terminate the entire graph (all tokens) cleanly.</summary>
        public void End() => OnEnd?.Invoke();


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
        /// Like <see cref="ResolveString"/>, but additionally interpolates
        /// <c>{VariableName}</c> tokens against the runtime blackboard — emulating
        /// a C# interpolated string, e.g. <c>"Hello {PlayerName}!"</c>. Use this for
        /// player-facing text (dialogue lines, choice labels) so authored copy can
        /// merge in live blackboard values. Use <see cref="ResolveString"/> for
        /// non-display fields (operators, numbers, …) where braces are not template.
        /// </summary>
        public string ResolveText(NodeData node, string fieldName)
            => Interpolate(ResolveString(node, fieldName));

        /// <summary>
        /// Replaces every <c>{VariableName}</c> token in <paramref name="template"/>
        /// with the matching runtime blackboard variable's value (looked up by Name).
        /// <c>{{</c> and <c>}}</c> are literal braces (as in C#). Unknown variables are
        /// left as the literal <c>{Name}</c> token so authoring typos are visible.
        /// </summary>
        public string Interpolate(string template)
        {
            if (string.IsNullOrEmpty(template) || template.IndexOf('{') < 0)
                return template;

            var sb = new StringBuilder(template.Length + 16);
            for (int i = 0; i < template.Length; i++)
            {
                char c = template[i];

                if (c == '{')
                {
                    if (i + 1 < template.Length && template[i + 1] == '{') { sb.Append('{'); i++; continue; }

                    int end = template.IndexOf('}', i + 1);
                    if (end < 0) { sb.Append(template, i, template.Length - i); break; }

                    string name = template.Substring(i + 1, end - i - 1).Trim();
                    sb.Append(LookupVariableString(name));
                    i = end;
                }
                else if (c == '}' && i + 1 < template.Length && template[i + 1] == '}')
                {
                    sb.Append('}'); i++;
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private string LookupVariableString(string name)
        {
            if (RuntimeBlackboard != null && !string.IsNullOrEmpty(name))
                foreach (var v in RuntimeBlackboard.Variables)
                    if (v != null && v.Name == name)
                        return v.ObjectValue?.ToString() ?? string.Empty;
            return "{" + name + "}";
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


        /// <summary>Variable name.</summary>
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

        /// <summary>T.</summary>
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
