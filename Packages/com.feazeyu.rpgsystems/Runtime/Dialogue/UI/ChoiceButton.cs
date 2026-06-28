using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>A dialogue choice button that reports its index to a <see cref="DialogueRunner"/> when clicked.</summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class ChoiceButton : MonoBehaviour
    {
        private DialogueRunner m_Runner;
        private int m_Index;

        /// <summary>Binds the button to a runner and the choice index it represents.</summary>
        public void Init(DialogueRunner runner, int index)
        {
            m_Runner = runner;
            m_Index  = index;
        }

        /// <summary>Selects this button's choice on the bound runner.</summary>
        public void Click()
        {
            Debug.Log($"Choice {m_Index} selected");
            m_Runner?.SelectChoice(m_Index);
        }
    }
}
