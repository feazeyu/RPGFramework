using UnityEngine;
using QuestGraph.Runtime;
using Feazeyu.RPGSystems.Character;
using Feazeyu.RPGSystems.Inventory;

namespace QuestGraph.Demo
{
    /// <summary>
    /// Replaces the old QuestDemoDirector. Drives the chain demo straight from NPC
    /// interaction instead of auto-starting quests:
    ///
    ///   • Talking to the NPC (wire <see cref="OfferNextQuest"/> to the NPC's
    ///     <c>Interactable.OnInteract</c>) starts the chain on first use and then
    ///     starts the next Available quest. Each quest graph opens with a Run
    ///     Dialogue node, so starting it immediately plays the offer dialogue where
    ///     the player accepts or declines. Declining aborts the quest, which the
    ///     chain returns to Available — so the next interaction re-offers it.
    ///   • A quest is only offered while none is active, so the same interaction
    ///     also serves the Deliver objective (which hands in items on interact)
    ///     without double-starting.
    ///   • Optionally seeds the player's inventory so the first (Deliver) quest is
    ///     doable. This is pure scene setup, not orchestration.
    /// </summary>
    public class QuestGiver : MonoBehaviour
    {
        [Header("Chain")]
        [Tooltip("Chain runner to drive. Auto-found in the scene if left empty.")]
        [SerializeField] private QuestChainRunner m_Chain;

        [Header("Starting inventory (scene setup)")]
        [Tooltip("GameObject carrying the player's IItemContainer (InventoryList/Grid).")]
        [SerializeField] private GameObject m_PlayerInventory;
        [SerializeField] private int m_StartingItemId    = 10;
        [SerializeField] private int m_StartingItemCount = 5;

        private Interactable m_Interactable;

        private void Awake()
        {
            // Subscribe in code so the demo needs no UnityEvent wiring in the scene:
            // every interaction with this NPC offers the next quest (when idle).
            m_Interactable = GetComponent<Interactable>();
            if (m_Interactable != null)
                m_Interactable.OnInteract.AddListener(OfferNextQuest);
        }

        private void OnDestroy()
        {
            if (m_Interactable != null)
                m_Interactable.OnInteract.RemoveListener(OfferNextQuest);
        }

        private void Start()
        {
            if (m_Chain == null)
                m_Chain = GetComponent<QuestChainRunner>();
            if (m_Chain == null)
                m_Chain = FindFirstObjectByType<QuestChainRunner>();

            var container = m_PlayerInventory != null
                ? m_PlayerInventory.GetComponent<IItemContainer>()
                : null;
            if (container != null && m_StartingItemCount > 0)
                container.TryAddItem(m_StartingItemId, m_StartingItemCount);
        }

        /// <summary>Wire this to the NPC's Interactable.OnInteract.</summary>
        public void OfferNextQuest()
        {
            if (m_Chain == null) return;

            if (!m_Chain.IsStarted) m_Chain.StartChain();

            // A graph quest is mid-run (objectives, or its offer dialogue) — let it
            // be. The Deliver objective consumes this same interact to hand in items.
            if (m_Chain.ActiveRunners.Count > 0) return;

            var available = m_Chain.GetAvailableQuests();
            if (available.Count > 0)
                m_Chain.StartQuest(available[0].ChainNodeGuid);
        }
    }
}
