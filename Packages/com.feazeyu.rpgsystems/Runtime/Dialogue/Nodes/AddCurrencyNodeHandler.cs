using System.Collections;
using Feazeyu.RPGSystems.Inventory;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Adds the specified amount to the player's wallet and continues.
    ///
    /// Fields:
    ///   Amount — gold to add (inline or blackboard Int)
    /// </summary>
    [DialogueNode("add_currency", "Add Currency", "Shop",
        "Adds money to the player's wallet and continues.")]
    public class AddCurrencyNodeHandler : IGraphNodeHandler
    {
        public string NodeTypeId => "add_currency";

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            int.TryParse(ctx.ResolveString(node, "Amount"), out int amount);
            PlayerWallet.Instance?.Add(amount);
            ctx.Follow("Out");
            yield break;
        }
    }
}
