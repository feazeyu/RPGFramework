using System.Collections;
using Feazeyu.RPGSystems.Inventory;
using UnityEngine;

namespace DialogueGraph.Runtime
{
    /// <summary>
    /// Dialogue node that tries to add an item to an IItemContainer.
    /// Follows "Success" if the item was added, "Failure" otherwise.
    ///
    /// Fields:
    ///   ItemId  — integer item ID (inline or blackboard Int variable)
    ///   Count   — how many to add (inline or blackboard Int variable), defaults to 1
    ///   Target  — optional blackboard GameObject variable with IItemContainer component;
    ///             falls back to PlayerInventoryService.Instance if unset
    /// </summary>
    [DialogueNode("give_item", "Give Item", "Inventory",
        "Tries to add an item to an inventory. Routes to Success or Failure.")]
    public class GiveItemNodeHandler : IGraphNodeHandler
    {
        public string NodeTypeId => "give_item";

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            Debug.Log("Giving item");
            int.TryParse(ctx.ResolveString(node, "ItemId"), out int itemId);

            int count = 1;
            if (int.TryParse(ctx.ResolveString(node, "Count"), out int parsedCount) && parsedCount > 0)
                count = parsedCount;

            bool success;
            var field = ctx.GetField(node, "Target");
            if (field != null && !string.IsNullOrEmpty(field.LinkedVariableGuid))
            {
                var v = ctx.RuntimeBlackboard.GetVariable(field.LinkedVariableGuid);
                var container = (v?.ObjectValue as GameObject)?.GetComponent<IItemContainer>();
                success = container?.TryAddItem(itemId, count) ?? false;
            }
            else
            {
                success = PlayerInventoryService.Instance?.TryAddItem(itemId, count) ?? false;
            }

            ctx.Follow(success ? "Success" : "Failure");
            yield break;
        }
    }
}
