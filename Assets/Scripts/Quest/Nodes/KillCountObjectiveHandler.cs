using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DialogueGraph.Runtime;
using QuestGraph.Runtime;
using Game.Character;

namespace QuestGraph.Nodes
{
    /// <summary>
    /// Handler for the Kill Count objective node (typeId = "obj_kill").
    ///
    /// Waits until the player has killed <c>Count</c> entities tagged with
    /// <c>Tag</c>, then follows "Completed". Entities spawned after activation
    /// are picked up automatically every second.
    ///
    /// Sequential chaining: connect the "Completed" port to the next
    /// objective node's "In" port.
    /// </summary>
    [QuestNode(QuestNodeRegistry.TypeObjKill, "Kill Count", "Objectives",
        "Kill N enemies with the given tag. Chain via Completed → next objective.")]
    public class KillCountObjectiveHandler : IGraphNodeHandler
    {
        public string NodeTypeId => QuestNodeRegistry.TypeObjKill;

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            var runner = ctx.Runner as QuestRunner;
            if (runner == null) { ctx.Follow("Failed"); yield break; }

            // ── Read fields ───────────────────────────────────────────────────
            var title   = ctx.ResolveString(node, "Title");
            var desc    = ctx.ResolveString(node, "Description");
            var tag     = ctx.ResolveString(node, "Tag");
            if (string.IsNullOrWhiteSpace(tag)) tag = "Enemy";
            int.TryParse(ctx.ResolveString(node, "Count"),   out int required);
            bool.TryParse(ctx.ResolveString(node, "Optional"), out bool optional);
            if (required <= 0) required = 1;

            var info = new ObjectiveInfo
            {
                NodeGuid    = node.Guid,
                Title       = title,
                Description = desc,
                Optional    = optional,
            };

            runner.RegisterObjective(info);

            // ── Subscribe to existing and future enemies ───────────────────────
            int kills = 0;
            var tracked   = new List<Entity>();
            float nextScan = 0f;

            void OnEntityDied()
            {
                kills++;
            }

            void ScanEnemies()
            {
                foreach (var go in GameObject.FindGameObjectsWithTag(tag))
                {
                    if (!go.TryGetComponent<Entity>(out var e)) continue;
                    if (tracked.Contains(e)) continue;
                    e.OnDeath.AddListener(OnEntityDied);
                    tracked.Add(e);
                }
            }

            ScanEnemies();

            // ── Wait for kill target ──────────────────────────────────────────
            while (kills < required && runner.IsRunning)
            {
                if (Time.time >= nextScan)
                {
                    nextScan = Time.time + 1f;
                    ScanEnemies();
                }
                yield return null;
            }

            // ── Cleanup ───────────────────────────────────────────────────────
            foreach (var e in tracked)
                if (e != null) e.OnDeath.RemoveListener(OnEntityDied);
            tracked.Clear();

            if (!runner.IsRunning) yield break;

            runner.UnregisterObjective(node.Guid, outcome: true);
            ctx.Follow("Completed");
        }
    }
}
