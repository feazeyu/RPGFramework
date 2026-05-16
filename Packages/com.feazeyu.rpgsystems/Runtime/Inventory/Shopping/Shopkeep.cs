using Feazeyu.RPGSystems.Character;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    public class Shopkeep : Interactable
    {
        [Header("Shop")]
        public ShopInventory shopInventory;

        [Tooltip("Grid-based shop UI to open on interact. Optional.")]
        public ShopGridUI shopGridUI;

        [Tooltip("List-based shop UI to open on interact. Optional.")]
        public ShopListUI shopListUI;

        [Tooltip("Close the shop automatically when the player leaves the interaction area.")]
        public bool closeOnAreaExit = true;

        private void Start()
        {
            if (shopInventory == null) return;
            shopGridUI?.Setup(shopInventory);
            shopListUI?.Setup(shopInventory);
        }

        public override void Interact()
        {
            base.Interact();
            shopGridUI?.ToggleInventory();
            shopListUI?.ToggleInventory();
        }

        public override void OnTriggerExit2D(Collider2D collision)
        {
            base.OnTriggerExit2D(collision);
            if (closeOnAreaExit)
            {
                shopGridUI?.CloseInventory();
                shopListUI?.CloseInventory();
            }
        }
    }
}
