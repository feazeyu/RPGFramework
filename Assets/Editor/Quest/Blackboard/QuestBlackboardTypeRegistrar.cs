using UnityEditor;
using UnityEngine;
using DialogueGraph.Editor;
using DialogueGraph.Runtime;
using QuestGraph.Runtime;

namespace QuestGraph.Editor
{
    /// <summary>
    /// Registers Quest and QuestGraph with the blackboard variable
    /// type picker so users can hold either kind of quest reference
    /// as a blackboard value. Chain graphs then link a Quest
    /// Reference node's "Quest" field to one of these variables.
    ///
    /// Runs on every editor load (static constructor + domain reload)
    /// so the registration survives script compilation cycles.
    /// </summary>
    [InitializeOnLoad]
    public static class QuestBlackboardTypeRegistrar
    {
        // Violet matches Quest Reference node accent in QuestNodeRegistry.
        private static readonly Color QuestGraphColour = new Color(0.62f, 0.55f, 0.88f);
        // Amber matches Objective node accent — "a quest in abstract".
        private static readonly Color QuestColour      = new Color(0.95f, 0.72f, 0.24f);

        static QuestBlackboardTypeRegistrar()
        {
            BlackboardVariableTypeRegistry.Register(new BlackboardVariableTypeRegistry.Entry
            {
                TypeName     = "QuestGraph",
                AccentColour = QuestGraphColour,
                Factory      = () => new BlackboardVariableQuestGraph(),
                Matcher      = v => v is BlackboardVariableQuestGraph,
            });

            BlackboardVariableTypeRegistry.Register(new BlackboardVariableTypeRegistry.Entry
            {
                TypeName     = "Quest",
                AccentColour = QuestColour,
                Factory      = () => new BlackboardVariableQuest(),
                Matcher      = v => v is BlackboardVariableQuest,
            });
        }
    }
}
