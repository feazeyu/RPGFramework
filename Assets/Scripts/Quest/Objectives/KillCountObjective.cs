using System.Collections.Generic;
using UnityEngine;
using QuestGraph.Runtime;
using Game.Character;

namespace QuestGraph.Objectives
{
    /// <summary>
    /// Objective driver: complete once <see cref="requiredKills"/> entities
    /// with the given tag have died. Optionally rescans for newly spawned
    /// enemies while the objective is active.
    ///
    /// Setup:
    ///   1. Place on the same GameObject as your QuestRunner.
    ///   2. Set objectiveTitle to match the Objective node Title in the graph.
    ///   3. Tag your enemy prefabs with <see cref="enemyTag"/>.
    ///   4. Enemies must have an Entity component (OnDeath is fired from it).
    /// </summary>
    [AddComponentMenu("Quest/Objectives/Kill Count")]
    public class KillCountObjective : QuestObjectiveBase
    {
        [Tooltip("Tag used to find enemy GameObjects.")]
        [SerializeField] public string enemyTag = "Enemy";

        [Tooltip("Number of kills required to complete the objective.")]
        [SerializeField] public int requiredKills = 1;

        [Tooltip("Re-scan for newly spawned enemies every second while active.")]
        [SerializeField] public bool trackNewEnemies = true;

        [SerializeField, HideInInspector]
        private int m_CurrentKills;

        private readonly List<Entity> m_Tracked = new List<Entity>();
        private float m_NextScan;

        protected override void StartTracking(ObjectiveInfo info)
        {
            m_CurrentKills = 0;
            m_NextScan     = 0f;
            ScanForEnemies();
        }

        protected override void StopTracking()
        {
            foreach (var e in m_Tracked)
                if (e != null) e.OnDeath.RemoveListener(OnEntityDied);
            m_Tracked.Clear();
        }

        private void Update()
        {
            if (!m_IsActive || !trackNewEnemies) return;
            if (Time.time < m_NextScan) return;
            m_NextScan = Time.time + 1f;
            ScanForEnemies();
        }

        private void ScanForEnemies()
        {
            foreach (var go in GameObject.FindGameObjectsWithTag(enemyTag))
            {
                if (!go.TryGetComponent<Entity>(out var entity)) continue;
                if (m_Tracked.Contains(entity)) continue;
                entity.OnDeath.AddListener(OnEntityDied);
                m_Tracked.Add(entity);
            }
        }

        private void OnEntityDied()
        {
            if (!m_IsActive) return;
            m_CurrentKills++;
            if (m_CurrentKills >= requiredKills)
                Complete();
        }
    }
}
