using UnityEngine;
using QuestGraph.Runtime;
using Feazeyu.RPGSystems.Character;
using Feazeyu.RPGSystems.Items;
using Feazeyu.RPGSystems.Inventory;

namespace QuestGraph.Objectives
{
    /// <summary>
    /// Objective driver: the player must interact with <see cref="deliveryTarget"/>
    /// while the target inventory holds at least <see cref="requiredCount"/> of
    /// <see cref="itemInfo"/>. On a successful delivery the items are removed and
    /// the objective completes.
    ///
    /// Setup:
    ///   1. Place on the same GameObject as your QuestRunner.
    ///   2. Set objectiveTitle to match the Objective node Title in the graph.
    ///   3. Assign itemInfo, deliveryTarget, and inventory.
    ///   4. inventory must be a GameObject with an IItemContainer component.
    /// </summary>
    [AddComponentMenu("Quest/Objectives/Deliver Item")]
    public class DeliverItemObjective : QuestObjectiveBase
    {
        [Tooltip("The item the player must deliver.")]
        [SerializeField] public ItemInfo itemInfo;

        [Tooltip("How many copies must be handed in.")]
        [SerializeField] public int requiredCount = 1;

        [Tooltip("The NPC or object the player interacts with to hand in the items.")]
        [SerializeField] public Interactable deliveryTarget;

        [Tooltip("The inventory to take items from. Must implement IItemContainer.")]
        [SerializeField] private MonoBehaviour m_InventoryRef;

        [Tooltip("Log a message when the player tries to deliver without enough items.")]
        [SerializeField] public bool logMissingItems = true;

        private IItemContainer Inventory => m_InventoryRef as IItemContainer;

        protected override void StartTracking(ObjectiveInfo info)
        {
            if (deliveryTarget != null)
                deliveryTarget.OnInteract.AddListener(OnDeliveryAttempt);
        }

        protected override void StopTracking()
        {
            if (deliveryTarget != null)
                deliveryTarget.OnInteract.RemoveListener(OnDeliveryAttempt);
        }

        private void OnDeliveryAttempt()
        {
            if (!m_IsActive || itemInfo == null || Inventory == null) return;

            int have = Inventory.CountItem(itemInfo.id);
            if (have < requiredCount)
            {
                if (logMissingItems)
                    Debug.Log($"[DeliverItemObjective] Need {requiredCount}x {itemInfo.Name}, have {have}.");
                return;
            }

            Inventory.RemoveItem(itemInfo.id, requiredCount);
            Complete();
        }
    }
}
