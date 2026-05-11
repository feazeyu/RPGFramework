using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;
using Feazeyu.RPGSystems.Dialogue;
using Feazeyu.RPGSystems.Character;

namespace Feazeyu.RPGSystems.Demo
{
    [RequireComponent(typeof(Interactable))]
    public class NPCDialogueTrigger : MonoBehaviour
    {
        [SerializeField] private DialogueRunner m_DialogueRunner;
        [SerializeField] private DialogueUI m_DialogueUI;
        [SerializeField] private GameObject m_InteractPrompt;

        private bool m_PlayerInRange;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<Interactor>(out _)) return;
            m_PlayerInRange = true;
            if (!m_DialogueRunner.IsRunning)
                SetPromptVisible(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent<Interactor>(out _)) return;
            m_PlayerInRange = false;
            SetPromptVisible(false);
            if (m_DialogueRunner.IsRunning)
                m_DialogueRunner.StopGraph();
        }

        // Wired to Interactable.OnInteract in the inspector.
        public void StartDialogue()
        {
            if (m_DialogueRunner == null || m_DialogueRunner.IsRunning) return;
            SetPromptVisible(false);
            m_DialogueUI.Bind(m_DialogueRunner);
            m_DialogueRunner.OnGraphEnded.AddListener(OnDialogueEnded);
            m_DialogueRunner.StartDialogue();
        }

        private void OnDialogueEnded()
        {
            m_DialogueRunner.OnGraphEnded.RemoveListener(OnDialogueEnded);
            if (m_PlayerInRange)
                SetPromptVisible(true);
        }

        private void SetPromptVisible(bool visible)
        {
            if (m_InteractPrompt != null)
                m_InteractPrompt.SetActive(visible);
        }
    }
}
