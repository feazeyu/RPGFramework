using Feazeyu.RPGSystems.Inventory;
using System.Collections;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Closes the shop UI on the target object and immediately continues.
    /// The target should be a GameObject with a Shopkeep, ShopGridUI, or ShopListUI component.
    /// The mirror of <see cref="OpenShopNodeHandler"/>.
    ///
    /// Fields:
    ///   Target — blackboard GameObject variable pointing at the Shopkeep
    ///   Mode   — "Grid" (only the ShopGridUI), "List" (only the ShopListUI), or empty/"Both"
    ///            (default: close whichever of the two are present)
    /// </summary>
    [DialogueNode("close_shop", "Close Shop", "Shop",
        "Closes the shop UI on the target Shopkeep/ShopGridUI GameObject and continues.")]
    public class CloseShopNodeHandler : IGraphNodeHandler
    {
        public string NodeTypeId => "close_shop";

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            var field = ctx.GetField(node, "Target");
            if (field != null && !string.IsNullOrEmpty(field.LinkedVariableGuid))
            {
                var v = ctx.RuntimeBlackboard.GetVariable(field.LinkedVariableGuid);
                var go = v?.ObjectValue as GameObject;
                if (go != null)
                    CloseShopOn(go, ctx.ResolveString(node, "Mode"));
                else
                    Debug.LogWarning("[CloseShop] Target blackboard variable held no GameObject.");
            }
            else
            {
                Debug.LogWarning("[CloseShop] No Target field linked — connect a blackboard GameObject variable.");
            }

            ctx.Follow("Out");
            yield break;
        }

        private static void CloseShopOn(GameObject go, string mode)
        {
            // An empty / "Both" mode closes whichever UIs are present; "Grid"/"List" restrict to one.
            bool both = string.IsNullOrEmpty(mode)
                        || mode.Equals("Both", System.StringComparison.OrdinalIgnoreCase);
            bool closeGrid = both || mode.Equals("Grid", System.StringComparison.OrdinalIgnoreCase);
            bool closeList = both || mode.Equals("List", System.StringComparison.OrdinalIgnoreCase);

            // Search on the target and its children — handles both direct UI components
            // and the case where the Shopkeep holds the UI references as children.
            if (closeGrid) go.GetComponentInChildren<ShopGridUI>(true)?.CloseInventory();
            if (closeList) go.GetComponentInChildren<ShopListUI>(true)?.CloseInventory();
        }
    }
}
