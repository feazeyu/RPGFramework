using System.Collections;
using Feazeyu.RPGSystems.Dialogue;
using QuestGraph.Runtime;

namespace QuestGraph.Nodes
{
    /// <summary>
    /// When a token reaches this node, it wipes the accumulated progress of every
    /// objective it targets, then continues out <c>Out</c>.
    ///
    /// Targets are chosen by edge: wire this node's <c>Target</c> output into the
    /// <c>In</c> port of each objective whose progress should reset. Target edges
    /// are reference-only — they never re-enter or re-run the objective.
    ///
    /// Compose with a Timer (Timeout → Begin … Timeout → Reset Progress) for
    /// "reset on expiry", or trigger from any flow path for manual resets.
    /// </summary>
    [QuestNode(QuestNodeRegistry.TypeResetProgress, "Reset Progress", "Flow",
        "Resets the progress of the objective(s) its Target output is wired to.")]
    public class ResetProgressNodeHandler : IGraphNodeHandler
    {
        /// <inheritdoc/>
        public string NodeTypeId => QuestNodeRegistry.TypeResetProgress;

        /// <inheritdoc/>
        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            if (ctx.Runner is QuestRunner runner)
            {
                foreach (var edge in ctx.Graph.Edges)
                {
                    if (edge.OutputNodeGuid != node.Guid || edge.OutputPortName != "Target") continue;
                    runner.ResetObjectiveProgress(edge.InputNodeGuid);
                }
            }

            ctx.Follow("Out");
            yield break;
        }
    }
}
