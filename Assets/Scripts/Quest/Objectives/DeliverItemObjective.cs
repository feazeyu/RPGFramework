using UnityEngine;
using QuestGraph.Runtime;
using Game.Character;
using Game.Items;
using Game.Inventory;

namespace QuestGraph.Objectives
{
    /// <summary>
    /// Objective driver: the player must interact with <see cref="deliveryTarget"/>
    /// while carrying at least <see cref="requiredCount"/> of <see cref="itemInfo"/>.
    /// On a successful delivery the items are removed from inventory and the
    /// objective completes.
    ///
    /// Setup:
    ///   1. Place on the same GameObject as your QuestRunner.
    ///   2. Set objectiveTitle to match the Objective node Title in the graph.
    ///   3. Assign itemInfo and deliveryTarget (an Interactable in the scene).
    ///   4. Ensure a PlayerInventoryService exists in the scene.
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

        [Tooltip("Log a message when the player tries to deliver without enough items.")]
        [SerializeField] public bool logMissingItems = true;

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
            if (!m_IsActive || itemInfo == null) return;
            var svc = PlayerInventoryService.Instance;
            if (svc == null) return;

            if (!svc.HasItem(itemInfo.id, requiredCount))
            {
                if (logMissingItems)
                    Debug.Log($"[DeliverItemObjective] Need {requiredCount}x {itemInfo.Name}, " +
                              $"have {svc.CountItem(itemInfo.id)}.");
                return;
            }

            svc.TakeItem(itemInfo.id, requiredCount);
            Complete();
        }
    }
}
