using UnityEngine;
using DialogueGraph.Runtime;

namespace DialogueGraph.UI
{
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class ChoiceButton : MonoBehaviour
    {
        private DialogueRunner m_Runner;
        private int m_Index;

        public void Init(DialogueRunner runner, int index)
        {
            m_Runner = runner;
            m_Index  = index;
        }

        public void Click()
        {
            Debug.Log($"Choice {m_Index} selected");
            m_Runner?.SelectChoice(m_Index);
        }
    }
}
