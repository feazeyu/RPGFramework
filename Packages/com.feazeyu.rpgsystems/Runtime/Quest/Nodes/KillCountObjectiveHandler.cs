using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Feazeyu.RPGSystems.Dialogue;
using QuestGraph.Runtime;
using Feazeyu.RPGSystems.Character;

namespace QuestGraph.Nodes
{
    /// <summary>
    /// Handler for the Kill Count objective node (typeId = "obj_kill").
    ///
    /// Waits until the player has killed <c>Count</c> entities tagged with
    /// <c>Tag</c>, then follows "Completed". Entities spawned after activation
    /// are picked up automatically every second.
    ///
    /// Each kill is offered to a shared <see cref="ObjectiveProgress"/> as a
    /// <see cref="ProgressEvent"/> (subject = the slain enemy), so composable
    /// modifiers apply: attach Gate nodes to the In port to require conditions
    /// (in a zone, while wearing X, while carrying Y), and a Timer + Reset
    /// Progress pair to enforce a time window that wipes progress on expiry.
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

            // Shared, resettable, gated progress counter for this objective.
            var progress = runner.RegisterProgress(node.Guid, required);
            progress.Gate = ObjectiveGates.Compose(node, ctx);

            // ── Subscribe to existing and future enemies ───────────────────────
            bool done = false;
            var callbacks = new Dictionary<Entity, UnityAction>();
            float nextScan = 0f;

            void OnEntityDied(Entity e)
            {
                var evt = new ProgressEvent
                {
                    Subject     = e != null ? e.gameObject : null,
                    Position    = e != null ? e.transform.position : Vector3.zero,
                    HasPosition = e != null,
                };
                if (progress.TryAdd(evt)) done = true;
            }

            void Track(Entity e)
            {
                if (callbacks.ContainsKey(e)) return;
                UnityAction cb = () => OnEntityDied(e);
                e.OnDeath.AddListener(cb);
                callbacks[e] = cb;
            }

            void ScanEnemies()
            {
                foreach (var go in GameObject.FindGameObjectsWithTag(tag))
                    if (go.TryGetComponent<Entity>(out var e)) Track(e);
            }

            ScanEnemies();

            // ── Wait for kill target ──────────────────────────────────────────
            while (!done && runner.IsRunning)
            {
                if (Time.time >= nextScan)
                {
                    nextScan = Time.time + 1f;
                    ScanEnemies();
                }
                yield return null;
            }

            // ── Cleanup ───────────────────────────────────────────────────────
            foreach (var kv in callbacks)
                if (kv.Key != null) kv.Key.OnDeath.RemoveListener(kv.Value);
            callbacks.Clear();
            runner.UnregisterProgress(node.Guid);

            if (!runner.IsRunning) yield break;

            runner.UnregisterObjective(node.Guid, outcome: true);
            ctx.Follow("Completed");
        }
    }
}
