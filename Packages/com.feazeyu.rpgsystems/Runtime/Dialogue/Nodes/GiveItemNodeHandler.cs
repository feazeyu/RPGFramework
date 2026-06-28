using System.Collections;
using Feazeyu.RPGSystems.Inventory;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Dialogue node that tries to add an item to an IItemContainer.
    /// Follows "Success" if the item was added, "Failure" otherwise.
    ///
    /// Fields:
    ///   ItemId  — integer item ID (inline or blackboard Int variable)
    ///   Count   — how many to add (inline or blackboard Int variable), defaults to 1
    ///   Target  — blackboard GameObject variable whose component implements IItemContainer
    /// </summary>
    [DialogueNode("give_item", "Give Item", "Inventory",
        "Tries to add an item to an inventory. Routes to Success or Failure.")]
    public class GiveItemNodeHandler : IGraphNodeHandler
    {
        /// <inheritdoc/>
        public string NodeTypeId => "give_item";

        /// <inheritdoc/>
        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            int.TryParse(ctx.ResolveString(node, "ItemId"), out int itemId);

            int count = 1;
            if (int.TryParse(ctx.ResolveString(node, "Count"), out int parsedCount) && parsedCount > 0)
                count = parsedCount;

            var field = ctx.GetField(node, "Target");
            IItemContainer container = null;
            if (field != null && !string.IsNullOrEmpty(field.LinkedVariableGuid))
            {
                var v = ctx.RuntimeBlackboard.GetVariable(field.LinkedVariableGuid);
                container = (v?.ObjectValue as GameObject)?.GetComponent<IItemContainer>();
            }

            bool success = container?.TryAddItem(itemId, count) ?? false;
            ctx.Follow(success ? "Success" : "Failure");
            yield break;
        }
    }
}
