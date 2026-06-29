using System.Collections;
using UnityEngine;
using Feazeyu.RPGSystems.Quest;

namespace Feazeyu.RPGSystems.Demo
{
    /// <summary>Sample helper that starts a quest automatically one frame after the scene loads.</summary>
    public class AutoStartQuest : MonoBehaviour
    {
        [SerializeField] private QuestRunner m_Runner;

        private IEnumerator Start()
        {
            yield return null;
            if (m_Runner == null)
                m_Runner = GetComponent<QuestRunner>();
            m_Runner?.StartQuest();
        }
    }
}
