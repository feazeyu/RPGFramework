using UnityEngine;
using Game.Character;

namespace QuestGraph.Demo
{
    [RequireComponent(typeof(Entity), typeof(Health), typeof(Interactable))]
    public class DemoEnemy : MonoBehaviour
    {
        private Health m_Health;
        private Interactable m_Interactable;

        private void Awake()
        {
            m_Health       = GetComponent<Health>();
            m_Interactable = GetComponent<Interactable>();
        }

        private void Start()
        {
            m_Interactable.OnInteract.AddListener(Die);
        }

        private void OnDestroy()
        {
            if (m_Interactable != null)
                m_Interactable.OnInteract.RemoveListener(Die);
        }

        // Press E near an enemy to kill it.
        private void Die()
        {
            if (m_Health != null)
                m_Health.Points = 0;
        }
    }
}
