using UnityEngine;
using QuestGraph.Runtime;
using Feazeyu.RPGSystems.Character;

namespace QuestGraph.Objectives
{
    /// <summary>
    /// Objective driver: complete once the player enters a circle of
    /// <see cref="radius"/> world-units around <see cref="targetLocation"/>.
    ///
    /// Setup:
    ///   1. Place on the same GameObject as your QuestRunner.
    ///   2. Set objectiveTitle to match the Objective node Title in the graph.
    ///   3. Assign targetLocation (e.g., an empty Transform placed in the scene).
    ///   4. Leave playerTransform empty to auto-find PlayerController on Start.
    /// </summary>
    [AddComponentMenu("Quest/Objectives/Reach Location")]
    public class ReachLocationObjective : QuestObjectiveBase
    {
        [Tooltip("The destination the player must reach.")]
        [SerializeField] public Transform targetLocation;

        [Tooltip("Arrival radius in world units.")]
        [SerializeField] public float radius = 2f;

        [Tooltip("Player transform. Auto-found via PlayerController if left empty.")]
        [SerializeField] private Transform playerTransform;

        protected override void Awake()
        {
            base.Awake();
            if (playerTransform == null)
            {
                var pc = FindFirstObjectByType<PlayerController>();
                if (pc != null) playerTransform = pc.transform;
            }
        }

        protected override void StartTracking(ObjectiveInfo info) { }

        private void Update()
        {
            if (!m_IsActive || targetLocation == null || playerTransform == null) return;
            if (Vector2.Distance(playerTransform.position, targetLocation.position) <= radius)
                Complete();
        }

        private void OnDrawGizmosSelected()
        {
            if (targetLocation == null) return;
            Gizmos.color = new Color(0.3f, 0.8f, 0.3f, 0.4f);
            Gizmos.DrawWireSphere(targetLocation.position, radius);
        }
    }
}
