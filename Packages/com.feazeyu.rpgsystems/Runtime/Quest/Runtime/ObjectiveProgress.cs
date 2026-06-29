using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Quest
{
    /// <summary>
    /// Context describing a single unit of objective progress (one kill, one
    /// pickup, one interaction, …). Gates inspect it to decide whether the unit
    /// counts.
    ///
    /// <see cref="Subject"/> is the natural actor/target of the event — the
    /// killed enemy, the item's owner, etc. It may be null; gates that need a
    /// capability the subject lacks (e.g. an inventory on a creature that has
    /// none) fall back to the player and warn rather than failing silently.
    /// </summary>
    public struct ProgressEvent
    {
        /// <summary>The event's natural subject, or null. Gates default to the player.</summary>
        public GameObject Subject;

        /// <summary>World position the event occurred at, when <see cref="HasPosition"/> is true.</summary>
        public Vector3 Position;

        /// <summary>Whether <see cref="Position"/> carries a meaningful value.</summary>
        public bool HasPosition;
    }

    /// <summary>
    /// Resettable progress counter shared between an objective handler and any
    /// modifiers (gates, timers, reset nodes) attached to that objective node.
    ///
    /// The objective handler raises raw events via <see cref="TryAdd"/>; the
    /// composed <see cref="Gate"/> predicate filters them, so an event only
    /// counts when every attached gate is satisfied. A Timer/Reset node can wipe
    /// accumulated progress at any time via <see cref="Reset"/>.
    /// </summary>
    public sealed class ObjectiveProgress
    {
        /// <summary>Node guid.</summary>
        public string NodeGuid;
        /// <summary>Required.</summary>
        public int    Required;

        /// <summary>Current.</summary>
        public int Current { get; private set; }

        /// <summary>Composed AND of all attached gate predicates; null = always open.</summary>
        public Func<ProgressEvent, bool> Gate;

        /// <summary>Fires whenever Current changes (HUD countdowns, timers subscribe).</summary>
        public event Action OnChanged;

        /// <summary>
        /// Offer one unit of progress. Returns true once <see cref="Required"/> is
        /// reached. Ignored (returns false) when a gate rejects the event.
        /// </summary>
        public bool TryAdd(ProgressEvent e, int amount = 1)
        {
            if (Gate != null && !Gate(e)) return false;
            Current += amount;
            OnChanged?.Invoke();
            return Current >= Required;
        }

        /// <summary>
        /// Remove progress (e.g. an accumulated item was dropped). Ungated and not
        /// clamped — Current may go negative — so a drop followed by re-acquiring the
        /// same item nets to a single count instead of double-counting.
        /// </summary>
        public void Subtract(int amount = 1)
        {
            if (amount == 0) return;
            Current -= amount;
            OnChanged?.Invoke();
        }

        /// <summary>Wipe accumulated progress back to zero.</summary>
        public void Reset()
        {
            if (Current == 0) return;
            Current = 0;
            OnChanged?.Invoke();
        }
    }
}
