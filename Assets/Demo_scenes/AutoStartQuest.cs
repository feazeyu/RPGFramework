using System.Collections;
using UnityEngine;
using QuestGraph.Runtime;

namespace QuestGraph.Demo
{
    public class AutoStartQuest : MonoBehaviour
    {
        [SerializeField] private QuestRunner m_Runner;

        private IEnumerator Start()
        {
            yield return null; // one frame so all Start()s can subscribe to events first
            if (m_Runner == null)
                m_Runner = GetComponent<QuestRunner>();
            m_Runner?.StartQuest();
        }
    }
}
