using System.Collections;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Implement this interface to define what happens when the graph runner
    /// reaches a node of a specific type.
    ///
    /// Register implementations with GraphRunner.RegisterHandler(), or place a
    /// GraphNodeBehaviour component on the runner's GameObject so it is
    /// discovered automatically.
    ///
    /// When execution is complete the handler must call either:
    ///   context.Follow("PortName")  — advance along an output port
    ///   context.End()               — terminate the graph
    /// Failing to call either will stall the graph indefinitely.
    /// </summary>
    public interface IGraphNodeHandler
    {
        /// <summary>Must match NodeData.NodeType for the nodes this handler processes.</summary>
        string NodeTypeId { get; }

        /// <summary>
        /// Coroutine that executes the node.  The runner yields on this, so the
        /// handler can span multiple frames.  Call context.Follow / context.End
        /// when done — either inside this coroutine or from an external callback.
        /// </summary>
        IEnumerator Execute(NodeData node, GraphRunContext context);
    }
}
