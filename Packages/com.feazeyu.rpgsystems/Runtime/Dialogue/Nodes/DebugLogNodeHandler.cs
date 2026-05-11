using System.Collections;
using UnityEngine;

namespace DialogueGraph.Runtime
{
    [DialogueNode("debug_log", "Debug Log", "Debug",
        "Prints a message to the Unity console and continues.")]
    public class DebugLogNodeHandler : IGraphNodeHandler
    {
        public string NodeTypeId => "debug_log";

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            Debug.Log(ctx.ResolveString(node, "Message"));
            ctx.Follow("Out");
            yield break;
        }
    }
}
