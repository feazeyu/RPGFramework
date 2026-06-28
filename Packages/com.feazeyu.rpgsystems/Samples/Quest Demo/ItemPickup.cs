using UnityEngine;
using Feazeyu.RPGSystems.Character;
using Feazeyu.RPGSystems.Inventory;

namespace QuestGraph.Demo
{
    /// <summary>
    /// World pickup. On interact, adds <see cref="amount"/> of item
    /// <see cref="itemId"/> to a target <see cref="IItemContainer"/> (the player's
    /// inventory) and despawns. One script serves both the apple pickups and the
    /// chest (which simply "gives" the Burn item on interact).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : Interactable
    {
        [Header("Pickup")]
        [Tooltip("Item id to grant (Apple = 10, Burn = 5).")]
        [SerializeField] private int itemId = 10;
        [SerializeField] private int amount = 1;

        [Tooltip("Optional. The inventory to add to. Auto-found (InventoryList/Grid) if left empty.")]
        [SerializeField] private GameObject inventoryObject;

        [SerializeField] private bool despawnOnPickup = true;

        public override void Interact()
        {
            base.Interact();

            var container = ResolveContainer();
            if (container == null)
            {
                Debug.LogWarning($"[ItemPickup] No IItemContainer found for '{name}'.", this);
                return;
            }

            if (container.TryAddItem(itemId, amount) && despawnOnPickup)
                Destroy(gameObject);
        }

        private IItemContainer ResolveContainer()
        {
            if (inventoryObject != null && inventoryObject.TryGetComponent<IItemContainer>(out var c))
                return c;

            var list = FindFirstObjectByType<InventoryList>();
            if (list != null) return list;
            return FindFirstObjectByType<InventoryGrid>();
        }
    }
}
