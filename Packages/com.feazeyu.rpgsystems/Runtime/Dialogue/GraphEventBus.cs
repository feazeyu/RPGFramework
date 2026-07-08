using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Process-wide, play-session-scoped event channel bus for the graph systems.
    ///
    /// Trigger Event nodes call <see cref="Raise"/> with their channel name and
    /// Wait For Event nodes suspend until that channel is raised — across all
    /// running graphs, dialogue and quest alike. Game code can wake waiting
    /// graphs the same way: <c>GraphEventBus.Raise("BossDefeated")</c>.
    ///
    /// Channels are plain strings compared ordinally; there is no payload and no
    /// latching — a raise only reaches nodes that are already waiting. Like
    /// <see cref="SharedBlackboardStore"/>, the bus is cleared at the start of
    /// every play session so no stale subscribers survive when domain reload is
    /// disabled.
    /// </summary>
    public static class GraphEventBus
    {
        /// <summary>Raised with the channel name on every <see cref="Raise"/> call.</summary>
        public static event Action<string> EventRaised;

        /// <summary>
        /// Clears all subscribers. Runs automatically before the first scene loads
        /// each play session (independent of the Enter Play Mode settings).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => EventRaised = null;

        /// <summary>Raises the named channel, waking every listener registered for it.</summary>
        public static void Raise(string channel)
        {
            if (string.IsNullOrEmpty(channel)) return;
            EventRaised?.Invoke(channel);
        }
    }
}
