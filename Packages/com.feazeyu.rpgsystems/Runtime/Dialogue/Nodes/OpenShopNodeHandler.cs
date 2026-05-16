using Feazeyu.RPGSystems.Inventory;
using System.Collections;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Opens the shop UI on the target object and immediately continues.
    /// The target should be a GameObject with a Shopkeep, ShopGridUI, or ShopListUI component.
    /// Combine with a FindObject node to locate the Shopkeep first.
    ///
    /// Fields:
    ///   Target — blackboard GameObject variable pointing at the Shopkeep
    /// </summary>
    [DialogueNode("open_shop", "Open Shop", "Shop",
        "Opens the shop UI on the target Shopkeep/ShopGridUI GameObject and continues.")]
    public class OpenShopNodeHandler : IGraphNodeHandler
    {
        public string NodeTypeId => "open_shop";

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            var field = ctx.GetField(node, "Target");
            if (field != null && !string.IsNullOrEmpty(field.LinkedVariableGuid))
            {
                var v = ctx.RuntimeBlackboard.GetVariable(field.LinkedVariableGuid);
                var go = v?.ObjectValue as GameObject;
                if (go != null)
                    OpenShopOn(go);
                else
                    Debug.LogWarning("[OpenShop] Target blackboard variable held no GameObject.");
            }
            else
            {
                Debug.LogWarning("[OpenShop] No Target field linked — connect a blackboard GameObject variable.");
            }

            ctx.Follow("Out");
            yield break;
        }

        private static void OpenShopOn(GameObject go)
        {
            // Search on the target and its children — handles both direct UI components
            // and the case where the Shopkeep holds the UI references as children.
            var grid = go.GetComponentInChildren<ShopGridUI>(true);
            var list = go.GetComponentInChildren<ShopListUI>(true);
            grid?.OpenInventory();
            list?.OpenInventory();
        }
    }
}
