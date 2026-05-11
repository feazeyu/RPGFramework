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
    ///   Waits until PlayerInventoryService reports Count of ItemId in inventory,
    ///   then follows "Completed".
    ///
    /// <b>Continuous mode</b> (Continuous = true):
    ///   Follows "Out" immediately. A background monitor checks every half-second;
    ///   if the player drops below Count items, the quest fails.
    ///   Example use: "Kill 5 slimes while carrying the magic sword."
    ///   Graph: [Collect Item (sword, continuous)] --Out--> [Kill Count (5 slimes)]
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
                runner.StartCoroutine(ContinuousMonitor(runner, info, itemId, count));
                ctx.Follow("Out");
                yield break;
            }

            // Normal: wait until player has enough items
            runner.RegisterObjective(info);

            float nextCheck = 0f;
            while (runner.IsRunning)
            {
                if (Time.time >= nextCheck)
                {
                    nextCheck = Time.time + k_CheckInterval;
                    var svc = PlayerInventoryService.Instance;
                    if (svc != null && svc.HasItem(itemId, count))
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
            QuestRunner runner, ObjectiveInfo info, int itemId, int count)
        {
            runner.RegisterObjective(info);

            float nextCheck = 0f;
            while (runner.IsRunning)
            {
                if (Time.time >= nextCheck)
                {
                    nextCheck = Time.time + k_CheckInterval;
                    var svc = PlayerInventoryService.Instance;
                    if (svc != null && !svc.HasItem(itemId, count))
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
    }
}
