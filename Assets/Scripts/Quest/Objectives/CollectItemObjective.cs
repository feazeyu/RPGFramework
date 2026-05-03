using UnityEngine;
using QuestGraph.Runtime;
using Game.Items;
using Game.Inventory;

namespace QuestGraph.Objectives
{
    /// <summary>
    /// Objective driver: complete once the player's inventory contains at
    /// least <see cref="requiredCount"/> of <see cref="itemInfo"/>.
    /// The inventory is polled every half-second rather than continuously.
    ///
    /// Setup:
    ///   1. Place on the same GameObject as your QuestRunner.
    ///   2. Set objectiveTitle to match the Objective node Title in the graph.
    ///   3. Assign itemInfo (the ItemInfo ScriptableObject for the target item).
    ///   4. Ensure a PlayerInventoryService exists in the scene.
    /// </summary>
    [AddComponentMenu("Quest/Objectives/Collect Item")]
    public class CollectItemObjective : QuestObjectiveBase
    {
        [Tooltip("The item the player must collect.")]
        [SerializeField] public ItemInfo itemInfo;

        [Tooltip("How many the player must have simultaneously.")]
        [SerializeField] public int requiredCount = 1;

        private const float k_CheckInterval = 0.5f;
        private float m_NextCheck;

        protected override void StartTracking(ObjectiveInfo info)
        {
            m_NextCheck = 0f;
        }

        private void Update()
        {
            if (!m_IsActive || itemInfo == null) return;
            if (Time.time < m_NextCheck) return;
            m_NextCheck = Time.time + k_CheckInterval;

            var svc = PlayerInventoryService.Instance;
            if (svc != null && svc.HasItem(itemInfo.id, requiredCount))
                Complete();
        }
    }
}
