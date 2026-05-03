using UnityEngine;
using QuestGraph.Runtime;
using Game.Inventory;
using Game.Items;

namespace QuestGraph.Runtime
{
    /// <summary>
    /// Listens to <see cref="QuestRunner.OnRewardGranted"/> and deposits
    /// item rewards directly into the player's inventory via
    /// <see cref="PlayerInventoryService"/>.
    ///
    /// Place this on the same GameObject as the QuestRunner. The Reward
    /// node's Item field must be linked to a blackboard variable holding
    /// an <see cref="ItemInfo"/> ScriptableObject.
    ///
    /// XP and currency fields in RewardInfo are not processed here — wire
    /// OnRewardGranted to your own stat/currency system for those.
    /// </summary>
    [AddComponentMenu("Quest/Quest Reward Handler")]
    public class QuestRewardHandler : MonoBehaviour
    {
        [Tooltip("The QuestRunner to listen to. Auto-found on this GameObject if empty.")]
        [SerializeField] private QuestRunner m_QuestRunner;

        protected virtual void Awake()
        {
            if (m_QuestRunner == null)
                m_QuestRunner = GetComponent<QuestRunner>();
        }

        private void OnEnable()
        {
            if (m_QuestRunner != null)
                m_QuestRunner.OnRewardGranted.AddListener(OnRewardGranted);
        }

        private void OnDisable()
        {
            if (m_QuestRunner != null)
                m_QuestRunner.OnRewardGranted.RemoveListener(OnRewardGranted);
        }

        protected virtual void OnRewardGranted(RewardInfo reward)
        {
            if (reward.item is ItemInfo itemInfo && reward.quantity > 0)
                PlayerInventoryService.Instance?.GiveItem(itemInfo.id, reward.quantity);
        }
    }
}
