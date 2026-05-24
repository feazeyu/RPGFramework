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
    /// <b>Normal mode</b> (Continuous = false):
    ///   Waits until the linked Inventory container has Count of ItemId, then follows "Completed".
    ///
    /// <b>Continuous mode</b> (Continuous = true):
    ///   Follows "Out" immediately. A background monitor checks every half-second;
    ///   if the container drops below Count items, the quest fails.
    ///   Example use: "Kill 5 slimes while carrying the magic sword."
    ///   Graph: [Collect Item (sword, continuous)] --Out--> [Kill Count (5 slimes)]
    ///
    /// Fields:
    ///   Inventory — blackboard GameObject variable with an IItemContainer component
    /// </summary>
    [QuestNode(QuestNodeRegistry.TypeObjCollect, "Collect Item", "Objectives",
        "Have N of an item. Continuous=true monitors in background for 'while carrying' quests.")]
    public class CollectItemObjectiveHandler : IGraphNodeHandler
    {
        public string NodeTypeId => QuestNodeRegistry.TypeObjCollect;

        private const float k_CheckInterval = 0.5f;

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            var runner = ctx.Runner as QuestRunner;
            if (runner == null) { ctx.Follow("Failed"); yield break; }

            // ── Read fields ───────────────────────────────────────────────────
            var title  = ctx.ResolveString(node, "Title");
            var desc   = ctx.ResolveString(node, "Description");
            int.TryParse(ctx.ResolveString(node, "ItemId"),     out int itemId);
            int.TryParse(ctx.ResolveString(node, "Count"),      out int count);
            bool.TryParse(ctx.ResolveString(node, "Continuous"), out bool continuous);
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

            // ── Dispatch ──────────────────────────────────────────────────────
            if (continuous)
            {
                runner.StartCoroutine(ContinuousMonitor(runner, info, itemId, count, container));
                ctx.Follow("Out");
                yield break;
            }

            // Normal: wait until the container has enough items
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

        // ── Continuous background monitor ─────────────────────────────────────

        private static IEnumerator ContinuousMonitor(
            QuestRunner runner, ObjectiveInfo info, int itemId, int count, IItemContainer container)
        {
            runner.RegisterObjective(info);

            float nextCheck = 0f;
            while (runner.IsRunning)
            {
                if (Time.time >= nextCheck)
                {
                    nextCheck = Time.time + k_CheckInterval;
                    if (container.CountItem(itemId) < count)
                    {
                        runner.UnregisterObjective(info.NodeGuid, outcome: false);
                        runner.ForceFailQuest($"Lost required item: {info.Title}");
                        yield break;
                    }
                }
                yield return null;
            }

            runner.UnregisterObjective(info.NodeGuid, outcome: true);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static IItemContainer ResolveContainer(NodeData node, GraphRunContext ctx)
        {
            var field = ctx.GetField(node, "Inventory");
            if (field == null || string.IsNullOrEmpty(field.LinkedVariableGuid)) return null;
            var v = ctx.RuntimeBlackboard.GetVariable(field.LinkedVariableGuid);
            return (v?.ObjectValue as GameObject)?.GetComponent<IItemContainer>();
        }
    }
}
