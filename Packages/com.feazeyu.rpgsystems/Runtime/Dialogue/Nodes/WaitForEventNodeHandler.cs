using System.Collections;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Handler for the Wait For Event node (typeId = "WaitForEvent").
    ///
    /// Suspends the flow token until the named channel is raised on the
    /// <see cref="GraphEventBus"/> — by a Trigger Event node in any running
    /// graph, or by game code calling <c>GraphEventBus.Raise(channel)</c> —
    /// then follows "Out". The raise must happen after this node starts
    /// waiting; there is no latching of earlier raises.
    ///
    /// Shared by every graph system: registered by both DialogueRunner and
    /// QuestRunner.
    /// </summary>
    public class WaitForEventNodeHandler : IGraphNodeHandler
    {
        /// <inheritdoc/>
        public string NodeTypeId => NodeRegistry.TypeWaitForEvent;

        /// <inheritdoc/>
        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            var channel = ctx.ResolveString(node, "Event Channel");
            if (string.IsNullOrEmpty(channel))
            {
                Debug.LogWarning("[WaitForEvent] No event channel set. Skipping.");
                ctx.Follow("Out");
                yield break;
            }

            bool received = false;
            void OnRaised(string raised)
            {
                if (raised == channel) received = true;
            }

            GraphEventBus.EventRaised += OnRaised;
            try
            {
                // The finally also runs when the runner stops the graph mid-wait
                // (StopAllCoroutines disposes the iterator), so no stale listener
                // is left on the bus.
                yield return new WaitUntil(() => received);
            }
            finally
            {
                GraphEventBus.EventRaised -= OnRaised;
            }

            ctx.Follow("Out");
        }
    }
}
