using System.Collections;
using Feazeyu.RPGSystems.Inventory;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Tries to spend the specified amount from the player's wallet.
    /// Routes to Success if the player had enough, Failure if not.
    ///
    /// Fields:
    ///   Amount — gold to deduct (inline or blackboard Int)
    /// </summary>
    [DialogueNode("remove_currency", "Remove Currency", "Shop",
        "Deducts money from the player's wallet. Routes Success or Failure.")]
    public class RemoveCurrencyNodeHandler : IGraphNodeHandler
    {
        public string NodeTypeId => "remove_currency";

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            int.TryParse(ctx.ResolveString(node, "Amount"), out int amount);
            bool success = PlayerWallet.Instance?.TrySpend(amount) ?? false;
            ctx.Follow(success ? "Success" : "Failure");
            yield break;
        }
    }
}
