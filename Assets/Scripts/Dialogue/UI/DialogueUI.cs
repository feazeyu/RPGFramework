using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DialogueGraph.Runtime;

namespace DialogueGraph.UI
{
    public class DialogueUI : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private TMP_Text m_SpeakerNameText;
        [SerializeField] private TMP_Text m_DialogueText;
        [SerializeField] private Image m_PortraitImage;
        [SerializeField] private Button m_ContinueButton;
        [SerializeField] private Transform m_ChoicesContainer;
        [SerializeField] private Button m_ChoiceButtonPrefab;

        private DialogueRunner m_Runner;
        private readonly List<Button> m_ChoiceButtons = new List<Button>();

        public bool IsOpen => m_Panel != null && m_Panel.activeSelf;

        private void Awake()
        {
            if (m_Panel != null) m_Panel.SetActive(false);
            if (m_ChoicesContainer != null) m_ChoicesContainer.gameObject.SetActive(false);
        }

        public void Bind(DialogueRunner runner)
        {
            if (m_Runner != null) Unbind();
            m_Runner = runner;
            m_Runner.OnDialogueLine.AddListener(ShowDialogueLine);
            m_Runner.OnChoicesPresented.AddListener(ShowChoices);
            m_Runner.OnGraphEnded.AddListener(Hide);
        }

        private void Unbind()
        {
            if (m_Runner == null) return;
            m_Runner.OnDialogueLine.RemoveListener(ShowDialogueLine);
            m_Runner.OnChoicesPresented.RemoveListener(ShowChoices);
            m_Runner.OnGraphEnded.RemoveListener(Hide);
            m_Runner = null;
        }

        private void ShowDialogueLine(string speaker, string text, Sprite portrait)
        {
            m_Panel.SetActive(true);
            if (m_SpeakerNameText != null) m_SpeakerNameText.text = speaker;
            if (m_DialogueText != null) m_DialogueText.text = text;
            if (m_PortraitImage != null)
            {
                m_PortraitImage.sprite = portrait;
                m_PortraitImage.gameObject.SetActive(portrait != null);
            }
            SetChoicesVisible(false);
            if (m_ContinueButton != null) m_ContinueButton.gameObject.SetActive(true);
        }

        private void ShowChoices(List<string> choices)
        {
            m_Panel.SetActive(true);
            if (m_ContinueButton != null) m_ContinueButton.gameObject.SetActive(false);
            ClearChoiceButtons();
            SetChoicesVisible(true);
            for (int i = 0; i < choices.Count; i++)
            {
                Button btn = Instantiate(m_ChoiceButtonPrefab, m_ChoicesContainer);
                btn.gameObject.SetActive(true);
                TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = choices[i];
                var choiceBtn = btn.gameObject.AddComponent<ChoiceButton>();
                choiceBtn.Init(m_Runner, i);
                btn.onClick.AddListener(choiceBtn.Click);
                m_ChoiceButtons.Add(btn);
            }
        }

        private void Hide()
        {
            ClearChoiceButtons();
            if (m_Panel != null) m_Panel.SetActive(false);
            Unbind();
        }

        private void ClearChoiceButtons()
        {
            foreach (Button btn in m_ChoiceButtons)
                if (btn != null) Destroy(btn.gameObject);
            m_ChoiceButtons.Clear();
        }

        private void SetChoicesVisible(bool visible)
        {
            if (m_ChoicesContainer != null)
                m_ChoicesContainer.gameObject.SetActive(visible);
        }

        public void OnContinueClicked()
        {
            m_Runner?.Advance();
        }

        private void OnDestroy() => Unbind();
    }
}
