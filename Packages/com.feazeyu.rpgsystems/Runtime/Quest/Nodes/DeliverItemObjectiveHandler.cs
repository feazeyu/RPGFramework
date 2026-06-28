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
    /// Waits for the player to interact with the specified NPC while the linked
    /// Inventory contains at least Count of ItemId. On success the items are removed
    /// and the node follows "Completed".
    ///
    /// <b>Field resolution</b> (first match wins):
    ///   NPC       — blackboard GameObject variable, or inline scene object name (GameObject.Find).
    ///   Inventory — blackboard GameObject variable with an IItemContainer component.
    ///
    /// The NPC's Interactable.OnInteract event is used for delivery — make
    /// sure the NPC has an Interactable component.
    /// </summary>
    [QuestNode(QuestNodeRegistry.TypeObjDeliver, "Deliver Item", "Objectives",
        "Interact with an NPC to hand in N items. Items are removed on delivery.")]
    public class DeliverItemObjectiveHandler : IGraphNodeHandler
    {
        /// <inheritdoc/>
        public string NodeTypeId => QuestNodeRegistry.TypeObjDeliver;

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

            var info = new ObjectiveInfo
            {
                NodeGuid    = node.Guid,
                Title       = title,
                Description = desc,
                Optional    = optional,
            };

            var interactable = ResolveNPC(node, ctx);
            if (interactable == null)
            {
                Debug.LogWarning($"[DeliverItemObjectiveHandler] No Interactable resolved for node '{title}'.");
                runner.RegisterObjective(info);
                runner.UnregisterObjective(node.Guid, outcome: false);
                ctx.Follow("Failed");
                yield break;
            }

            var container = ResolveContainer(node, ctx);
            if (container == null)
            {
                Debug.LogWarning($"[DeliverItemObjectiveHandler] No IItemContainer resolved for '{title}'. Link an Inventory field.");
                runner.RegisterObjective(info);
                runner.UnregisterObjective(node.Guid, outcome: false);
                ctx.Follow("Failed");
                yield break;
            }

            runner.RegisterObjective(info);

            bool delivered = false;

            void OnInteract()
            {
                if (delivered) return;
                if (container.CountItem(itemId) < count)
                {
                    Debug.Log($"[DeliverItemObjectiveHandler] Need {count}x item#{itemId}, " +
                              $"have {container.CountItem(itemId)}.");
                    return;
                }
                container.RemoveItem(itemId, count);
                delivered = true;
            }

            interactable.OnInteract.AddListener(OnInteract);

            yield return new WaitUntil(() => delivered || !runner.IsRunning);

            interactable.OnInteract.RemoveListener(OnInteract);

            if (!runner.IsRunning) yield break;

            runner.UnregisterObjective(node.Guid, outcome: true);
            ctx.Follow("Completed");
        }


        private static Interactable ResolveNPC(NodeData node, GraphRunContext ctx)
        {
            var guid = ctx.GetLinkedGuid(node, "NPC");
            if (!string.IsNullOrEmpty(guid))
            {
                var v = ctx.RuntimeBlackboard.GetVariable(guid);
                if (v?.ObjectValue is GameObject go && go.TryGetComponent<Interactable>(out var i))
                    return i;
            }

            var name = ctx.ResolveString(node, "NPC");
            if (!string.IsNullOrEmpty(name))
            {
                var found = GameObject.Find(name);
                if (found != null && found.TryGetComponent<Interactable>(out var i)) return i;
            }

            return null;
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
