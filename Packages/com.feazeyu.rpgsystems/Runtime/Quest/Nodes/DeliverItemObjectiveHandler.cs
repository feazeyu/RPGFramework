using System.Collections;
using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;
using QuestGraph.Runtime;
using Feazeyu.RPGSystems.Character;
using Feazeyu.RPGSystems.Inventory;

namespace QuestGraph.Nodes
{
    /// <summary>
    /// Handler for the Deliver Item objective node (typeId = "obj_deliver").
    ///
    /// Waits for the player to interact with the specified NPC while carrying
    /// at least Count of ItemId. On success the items are removed from the
    /// player's inventory and the node follows "Completed".
    ///
    /// <b>NPC resolution</b> (first match wins):
    ///   1. Blackboard variable of type GameObject linked to the NPC field.
    ///   2. Inline value treated as a scene object name (GameObject.Find).
    ///
    /// The NPC's Interactable.OnInteract event is used for delivery — make
    /// sure the NPC has an Interactable component.
    /// </summary>
    [QuestNode(QuestNodeRegistry.TypeObjDeliver, "Deliver Item", "Objectives",
        "Interact with an NPC to hand in N items. Items are removed on delivery.")]
    public class DeliverItemObjectiveHandler : IGraphNodeHandler
    {
        public string NodeTypeId => QuestNodeRegistry.TypeObjDeliver;

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            var runner = ctx.Runner as QuestRunner;
            if (runner == null) { ctx.Follow("Failed"); yield break; }

            // ── Read fields ───────────────────────────────────────────────────
            var title = ctx.ResolveString(node, "Title");
            var desc  = ctx.ResolveString(node, "Description");
            int.TryParse(ctx.ResolveString(node, "ItemId"), out int itemId);
            int.TryParse(ctx.ResolveString(node, "Count"),  out int count);
            bool.TryParse(ctx.ResolveString(node, "Optional"), out bool optional);
            if (count <= 0) count = 1;

            var info = new ObjectiveInfo
            {
                NodeGuid    = node.Guid,
                Title       = title,
                Description = desc,
                Optional    = optional,
            };

            // ── Resolve NPC ───────────────────────────────────────────────────
            var interactable = ResolveNPC(node, ctx);
            if (interactable == null)
            {
                Debug.LogWarning($"[DeliverItemObjectiveHandler] No Interactable resolved for node '{title}'.");
                runner.RegisterObjective(info);
                runner.UnregisterObjective(node.Guid, outcome: false);
                ctx.Follow("Failed");
                yield break;
            }

            // ── Wait for delivery ─────────────────────────────────────────────
            runner.RegisterObjective(info);

            bool delivered = false;

            void OnInteract()
            {
                if (delivered) return;
                var svc = PlayerInventoryService.Instance;
                if (svc == null || !svc.HasItem(itemId, count))
                {
                    Debug.Log($"[DeliverItemObjectiveHandler] Need {count}x item#{itemId}, " +
                              $"have {svc?.CountItem(itemId) ?? 0}.");
                    return;
                }
                svc.TakeItem(itemId, count);
                delivered = true;
            }

            interactable.OnInteract.AddListener(OnInteract);

            yield return new WaitUntil(() => delivered || !runner.IsRunning);

            interactable.OnInteract.RemoveListener(OnInteract);

            if (!runner.IsRunning) yield break;

            runner.UnregisterObjective(node.Guid, outcome: true);
            ctx.Follow("Completed");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Interactable ResolveNPC(NodeData node, GraphRunContext ctx)
        {
            var guid = ctx.GetLinkedGuid(node, "NPC");
            if (!string.IsNullOrEmpty(guid))
            {
                var v = ctx.RuntimeBlackboard.GetVariable(guid);
                if (v?.ObjectValue is GameObject go)
                {
                    if (go.TryGetComponent<Interactable>(out var i)) return i;
                }
            }

            var name = ctx.ResolveString(node, "NPC");
            if (!string.IsNullOrEmpty(name))
            {
                var found = GameObject.Find(name);
                if (found != null && found.TryGetComponent<Interactable>(out var i)) return i;
            }

            return null;
        }
    }
}
