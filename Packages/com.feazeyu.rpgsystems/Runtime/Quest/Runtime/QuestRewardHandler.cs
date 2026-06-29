using UnityEngine;
using Feazeyu.RPGSystems.Quest;

namespace Feazeyu.RPGSystems.Quest
{
    /// <summary>
    /// Listens to <see cref="QuestRunner.OnRewardGranted"/>. Override
    /// <see cref="OnRewardGranted"/> to handle XP, currency, or other rewards.
    /// Item rewards are granted via the SpawnItem quest node instead.
    /// </summary>
    [AddComponentMenu("Quest/Quest Reward Handler")]
    public class QuestRewardHandler : MonoBehaviour
    {
        [Tooltip("The QuestRunner to listen to. Auto-found on this GameObject if empty.")]
        [SerializeField] private QuestRunner m_QuestRunner;

        /// <summary>Awake.</summary>
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

        /// <summary>On reward granted.</summary>
        protected virtual void OnRewardGranted(RewardInfo reward) { }
    }
}
