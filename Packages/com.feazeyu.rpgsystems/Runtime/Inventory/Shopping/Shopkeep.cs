using Feazeyu.RPGSystems.Character;
using UnityEngine;

namespace Feazeyu.RPGSystems.Inventory
{
    /// <summary>
    /// An <see cref="Interactable"/> NPC that opens a grid- and/or list-based shop UI
    /// bound to its <see cref="ShopInventory"/> on interact.
    /// </summary>
    public class Shopkeep : Interactable
    {
        /// <summary>Shop inventory.</summary>
        [Header("Shop")]
        public ShopInventory shopInventory;

        /// <summary>Shop grid ui.</summary>
        [Tooltip("Grid-based shop UI to open on interact. Optional.")]
        public ShopGridUI shopGridUI;

        /// <summary>Shop list ui.</summary>
        [Tooltip("List-based shop UI to open on interact. Optional.")]
        public ShopListUI shopListUI;

        /// <summary>Close on area exit.</summary>
        [Tooltip("Close the shop automatically when the player leaves the interaction area.")]
        public bool closeOnAreaExit = true;

        private void Start()
        {
            if (shopInventory == null) return;
            shopGridUI?.Setup(shopInventory);
            shopListUI?.Setup(shopInventory);
        }

        /// <inheritdoc/>
        public override void Interact()
        {
            base.Interact();
            shopGridUI?.ToggleInventory();
            shopListUI?.ToggleInventory();
        }

        /// <inheritdoc/>
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
