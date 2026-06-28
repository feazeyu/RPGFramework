using System.Collections;
using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;
using QuestGraph.Runtime;
using Feazeyu.RPGSystems.Inventory;

namespace QuestGraph.Nodes
{
    /// <summary>
    /// Handler for the Collect Item objective node (typeId = "obj_collect").
    ///
    /// <b>Level-based</b>: completes once the linked Inventory container holds at
    /// least Count of ItemId — it checks the absolute amount, so whatever the
    /// player already carried counts. For "acquire N from now on, ignoring the
    /// starting amount" (and gate/timer support) use <b>Accumulate Item</b>.
    ///
    /// Waits until the container has Count of ItemId, then follows "Completed".
    /// For a "while carrying X" constraint, attach an Item Gate to the guarded
    /// objective's In port instead.
    ///
    /// Fields:
    ///   Inventory — blackboard GameObject variable with an IItemContainer component
    /// </summary>
    [QuestNode(QuestNodeRegistry.TypeObjCollect, "Collect Item", "Objectives",
        "Have N of an item, then follow Completed.")]
    public class CollectItemObjectiveHandler : IGraphNodeHandler
    {
        /// <inheritdoc/>
        public string NodeTypeId => QuestNodeRegistry.TypeObjCollect;

        private const float k_CheckInterval = 0.5f;

        /// <inheritdoc/>
        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            var runner = ctx.Runner as QuestRunner;
            if (runner == null) { ctx.Follow("Failed"); yield break; }

            var title  = ctx.ResolveString(node, "Title");
            var desc   = ctx.ResolveString(node, "Description");
            int.TryParse(ctx.ResolveString(node, "ItemId"),     out int itemId);
            int.TryParse(ctx.ResolveString(node, "Count"),      out int count);
            bool.TryParse(ctx.ResolveString(node, "Optional"),   out bool optional);
            if (count <= 0) count = 1;

            var container = ResolveContainer(node, ctx);
            if (container == null)
            {
                Debug.LogWarning($"[CollectItemObjectiveHandler] No IItemContainer resolved for '{title}'. Link an Inventory field.");
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

            float nextCheck = 0f;
            while (runner.IsRunning)
            {
                if (Time.time >= nextCheck)
                {
                    nextCheck = Time.time + k_CheckInterval;
                    if (container.CountItem(itemId) >= count)
                        break;
                }
                yield return null;
            }

            if (!runner.IsRunning) yield break;

            runner.UnregisterObjective(node.Guid, outcome: true);
            ctx.Follow("Completed");
        }


        private static IItemContainer ResolveContainer(NodeData node, GraphRunContext ctx)
        {
            var field = ctx.GetField(node, "Inventory");
            if (field == null || string.IsNullOrEmpty(field.LinkedVariableGuid)) return null;
            var v = ctx.RuntimeBlackboard.GetVariable(field.LinkedVariableGuid);
            return (v?.ObjectValue as GameObject)?.GetComponent<IItemContainer>();
        }
    }
}
