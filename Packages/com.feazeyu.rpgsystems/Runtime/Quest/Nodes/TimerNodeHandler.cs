using System.Collections;
using System.Globalization;
using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;
using QuestGraph.Runtime;

namespace QuestGraph.Nodes
{
    /// <summary>
    /// A standalone timer. A token entering the <c>Begin</c> input starts the
    /// clock; after <c>Seconds</c> it fires <see cref="QuestRunner.OnTimerTimeout"/>
    /// and forks a token down <c>Timeout</c>. With <c>AutoRestart</c> on it re-arms
    /// and repeats as a perpetual watchdog (forking a fresh token each cycle while
    /// keeping its own token alive); off, it times out once.
    ///
    /// The timer has no built-in side effect — wire <c>Timeout</c> to whatever
    /// should happen (e.g. a Reset Progress node) to compose behaviour.
    /// </summary>
    [QuestNode(QuestNodeRegistry.TypeTimer, "Timer", "Flow",
        "Starts on Begin; fires Timeout after Seconds. AutoRestart re-arms as a watchdog.")]
    public class TimerNodeHandler : IGraphNodeHandler
    {
        public string NodeTypeId => QuestNodeRegistry.TypeTimer;

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            float.TryParse(ctx.ResolveString(node, "Seconds"),
                NumberStyles.Float, CultureInfo.InvariantCulture, out float secs);
            if (secs <= 0f) secs = 1f;
            bool.TryParse(ctx.ResolveString(node, "AutoRestart"), out bool loop);

            var runner = ctx.Runner as QuestRunner;

            do
            {
                runner?.SetTimerWindow(secs);
                yield return new WaitForSeconds(secs);
                if (!ctx.Runner.IsRunning) yield break;

                runner?.RaiseTimerTimeout();
                ctx.Fork("Timeout");
            }
            while (loop && ctx.Runner.IsRunning);
        }
    }
}
