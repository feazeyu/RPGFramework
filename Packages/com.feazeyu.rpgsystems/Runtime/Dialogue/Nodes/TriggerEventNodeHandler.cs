using System.Collections;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Generic handler for the Trigger Event node (typeId = "TriggerEvent"):
    /// raises the node's channel on the <see cref="GraphEventBus"/> (waking any
    /// Wait For Event node listening for it) and follows "Out".
    ///
    /// Used by runners without their own Trigger Event handling (QuestRunner).
    /// DialogueRunner keeps its own handler so it can additionally fire its
    /// OnEventTriggered UnityEvent, but raises the same bus.
    /// </summary>
    public class TriggerEventNodeHandler : IGraphNodeHandler
    {
        /// <inheritdoc/>
        public string NodeTypeId => NodeRegistry.TypeTriggerEvent;

        /// <inheritdoc/>
        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            GraphEventBus.Raise(ctx.ResolveString(node, "Event Channel"));
            ctx.Follow("Out");
            yield break;
        }
    }
}
