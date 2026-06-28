using UnityEngine;
using Feazeyu.RPGSystems.Character;

namespace QuestGraph.Demo
{
    /// <summary>Sample enemy for the Quest demo: takes damage on interact and dies for kill objectives.</summary>
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

        private void Die()
        {
            if (m_Health != null)
                m_Health.Points = 0;
        }
    }
}
