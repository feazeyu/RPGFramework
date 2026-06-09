using System.Collections.Generic;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Process-wide, play-session-scoped store for Shared blackboard variables.
    ///
    /// A Shared variable is global: every runtime blackboard that contains a
    /// variable with the same Guid resolves to one shared instance, so writing it
    /// in one dialogue is immediately visible in every other run of that variable
    /// ("affect all instances of this variable across dialogues").
    ///
    /// The store lives only in memory and is wiped at the start of every play
    /// session, and it is seeded from a <em>clone</em> of the authored variable —
    /// so it never mutates the asset. Exit play mode and the Blackboard returns to
    /// its serialized defaults.
    /// </summary>
    public static class SharedBlackboardStore
    {
        // Keyed by variable Guid. Clone() preserves the Guid, so the shared
        // instance stays findable via Blackboard.GetVariable(guid).
        private static readonly Dictionary<string, BlackboardVariable> s_Shared
            = new Dictionary<string, BlackboardVariable>();

        /// <summary>
        /// Clears the store. Runs automatically before the first scene loads each
        /// play session (independent of the Enter Play Mode / domain-reload setting),
        /// guaranteeing a clean slate that re-seeds from the authored defaults.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear() => s_Shared.Clear();

        /// <summary>
        /// Returns the global runtime instance for <paramref name="authored"/>,
        /// creating it (as a clone of the authored variable) on first use. The
        /// authored asset variable is never returned and never mutated.
        /// </summary>
        public static BlackboardVariable Resolve(BlackboardVariable authored)
        {
            if (authored == null) return null;

            var key = authored.Guid;
            // No stable identity to share on — fall back to an isolated clone.
            if (string.IsNullOrEmpty(key)) return authored.Clone();

            if (!s_Shared.TryGetValue(key, out var shared))
            {
                shared = authored.Clone();
                s_Shared[key] = shared;
            }
            return shared;
        }
    }
}
