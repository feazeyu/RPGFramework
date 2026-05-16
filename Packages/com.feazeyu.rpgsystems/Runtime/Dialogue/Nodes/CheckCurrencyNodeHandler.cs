using System.Collections;
using Feazeyu.RPGSystems.Inventory;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Checks whether the player's wallet has at least the specified amount.
    /// Routes to Enough if balance >= Amount, NotEnough otherwise.
    ///
    /// Fields:
    ///   Amount — the gold threshold to check (inline or blackboard Int)
    /// </summary>
    [DialogueNode("check_currency", "Check Currency", "Shop",
        "Routes Enough or NotEnough based on the player's wallet balance.")]
    public class CheckCurrencyNodeHandler : IGraphNodeHandler
    {
        public string NodeTypeId => "check_currency";

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            int.TryParse(ctx.ResolveString(node, "Amount"), out int amount);
            bool enough = (PlayerWallet.Instance?.Balance ?? 0) >= amount;
            ctx.Follow(enough ? "Enough" : "NotEnough");
            yield break;
        }
    }
}
