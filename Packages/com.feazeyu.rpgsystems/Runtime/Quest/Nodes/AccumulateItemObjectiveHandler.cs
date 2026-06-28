using System;
using System.Collections;
using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;
using QuestGraph.Runtime;
using Feazeyu.RPGSystems.Inventory;

namespace QuestGraph.Nodes
{
    /// <summary>
    /// Handler for the Accumulate Item objective node (typeId = "obj_accumulate").
    ///
    /// Counts how many of ItemId the player <b>acquires after the objective starts</b>,
    /// ignoring whatever they already held, and completes once Count have been gained.
    /// Each gained unit is offered to a shared <see cref="ObjectiveProgress"/> as a
    /// <see cref="ProgressEvent"/> (subject = the inventory owner), so the composable
    /// modifiers apply: Gate nodes on the In port (in a zone, while wearing X, …) and
    /// a Timer + Reset Progress pair for "acquire N within a window, resetting on expiry".
    ///
    /// Counting is <b>net</b>: additions count (through the gate) and removals
    /// decrement (ungated, may go negative), so dropping an item and re-collecting
    /// it nets to a single count rather than double-counting. Detection is exact when
    /// the container implements <see cref="IItemCountNotifier"/> (subscribes to
    /// OnItemAdded/OnItemRemoved); otherwise it polls signed CountItem deltas.
    ///
    /// Fields:
    ///   Inventory — blackboard GameObject variable with an IItemContainer component
    /// </summary>
    [QuestNode(QuestNodeRegistry.TypeObjAccumulate, "Accumulate Item", "Objectives",
        "Acquire N of an item after starting (ignores starting amount). Supports Gate/Timer/Reset modifiers.")]
    public class AccumulateItemObjectiveHandler : IGraphNodeHandler
    {
        /// <inheritdoc/>
        public string NodeTypeId => QuestNodeRegistry.TypeObjAccumulate;

        private const float k_CheckInterval = 0.5f;

        /// <inheritdoc/>
        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            var runner = ctx.Runner as QuestRunner;
            if (runner == null) { ctx.Follow("Failed"); yield break; }

            var title = ctx.ResolveString(node, "Title");
            var desc  = ctx.ResolveString(node, "Description");
            int.TryParse(ctx.ResolveString(node, "ItemId"), out int itemId);
            int.TryParse(ctx.ResolveString(node, "Count"),  out int count);
            bool.TryParse(ctx.ResolveString(node, "Optional"), out bool optional);
            if (count <= 0) count = 1;

            var invObject = ResolveInventoryObject(node, ctx);
            var container = invObject != null ? invObject.GetComponent<IItemContainer>() : null;
            if (container == null)
            {
                Debug.LogWarning($"[AccumulateItemObjectiveHandler] No IItemContainer resolved for '{title}'. Link an Inventory field.");
                ctx.Follow("Failed");
                yield break;
            }

            var info = new ObjectiveInfo
            {
                NodeGuid    = node.Guid,
                Title       = title,
                Description = desc,
                Optional    = optional,
            };

            runner.RegisterObjective(info);
            var progress = runner.RegisterProgress(node.Guid, count);
            progress.Gate = ObjectiveGates.Compose(node, ctx);

            bool done = false;

            ProgressEvent MakeEvent() => new ProgressEvent
            {
                Subject     = invObject,
                Position    = invObject != null ? invObject.transform.position : Vector3.zero,
                HasPosition = invObject != null,
            };

            void Offer(int units)
            {
                for (int i = 0; i < units && !done; i++)
                    if (progress.TryAdd(MakeEvent())) done = true;
            }

            var notifier = container as IItemCountNotifier;
            Action<int, int> onAdded   = null;
            Action<int, int> onRemoved = null;
            if (notifier != null)
            {
                onAdded   = (id, n) => { if (id == itemId) Offer(n); };
                onRemoved = (id, n) => { if (id == itemId) progress.Subtract(n); };
                notifier.OnItemAdded   += onAdded;
                notifier.OnItemRemoved += onRemoved;
            }

            int   lastSeen  = container.CountItem(itemId);
            float nextCheck = 0f;

            while (!done && runner.IsRunning)
            {
                if (notifier == null && Time.time >= nextCheck)
                {
                    nextCheck = Time.time + k_CheckInterval;
                    int current = container.CountItem(itemId);
                    int delta   = current - lastSeen;
                    if (delta > 0)      Offer(delta);
                    else if (delta < 0) progress.Subtract(-delta);
                    lastSeen = current;
                }
                yield return null;
            }

            if (notifier != null)
            {
                notifier.OnItemAdded   -= onAdded;
                notifier.OnItemRemoved -= onRemoved;
            }
            runner.UnregisterProgress(node.Guid);

            if (!runner.IsRunning) yield break;

            runner.UnregisterObjective(node.Guid, outcome: true);
            ctx.Follow("Completed");
        }


        private static GameObject ResolveInventoryObject(NodeData node, GraphRunContext ctx)
        {
            var field = ctx.GetField(node, "Inventory");
            if (field == null || string.IsNullOrEmpty(field.LinkedVariableGuid)) return null;
            var v = ctx.RuntimeBlackboard.GetVariable(field.LinkedVariableGuid);
            return v?.ObjectValue as GameObject;
        }
    }
}
