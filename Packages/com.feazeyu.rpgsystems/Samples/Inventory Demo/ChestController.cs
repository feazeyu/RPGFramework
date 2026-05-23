using UnityEngine;
using Feazeyu.RPGSystems.Inventory;
using Feazeyu.RPGSystems.Character;

namespace Feazeyu.RPGSystems.Demo
{
    [RequireComponent(typeof(Interactable))]
    public class ChestController : MonoBehaviour
    {
        [SerializeField] private InventoryList m_Inventory;
        [SerializeField] private GameObject m_InteractPrompt;

        private bool m_IsOpen;

        private void Start()
        {
            m_Inventory?.CloseInventory();
        }

        public void Toggle()
        {
            if (m_Inventory == null) return;
            m_IsOpen = !m_IsOpen;
            if (m_IsOpen) m_Inventory.OpenInventory();
            else          m_Inventory.CloseInventory();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<Interactor>(out _))
                m_InteractPrompt?.SetActive(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent<Interactor>(out _)) return;
            m_InteractPrompt?.SetActive(false);
            if (m_IsOpen)
            {
                m_IsOpen = false;
                m_Inventory?.CloseInventory();
            }
        }
    }
}
