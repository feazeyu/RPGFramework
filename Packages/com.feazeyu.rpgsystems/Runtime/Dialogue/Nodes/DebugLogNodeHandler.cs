using System.Collections;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>Node handler that prints a message to the Unity console and continues.</summary>
    [DialogueNode("debug_log", "Debug Log", "Debug",
        "Prints a message to the Unity console and continues.")]
    public class DebugLogNodeHandler : IGraphNodeHandler
    {
        /// <inheritdoc/>
        public string NodeTypeId => "debug_log";

        /// <inheritdoc/>
        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            Debug.Log(ctx.ResolveString(node, "Message"));
            ctx.Follow("Out");
            yield break;
        }
    }
}
