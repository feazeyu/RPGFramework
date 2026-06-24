using UnityEditor;
using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;

namespace Feazeyu.RPGSystems.EditorTools
{
    /// <summary>
    /// Registers the DialogueGraph type with the blackboard variable
    /// type picker so a graph can hold a <see cref="DialogueGraphAsset"/>
    /// reference as a blackboard value — the only way to feed a subgraph
    /// into a dialogue Run Subgraph node.
    ///
    /// Lives on the dialogue side rather than in <see cref="BlackboardPanel"/>
    /// so the panel stays graph-system-agnostic — mirrors
    /// <see cref="QuestBlackboardTypeRegistrar"/>.
    ///
    /// Runs on every editor load (static constructor + domain reload)
    /// so the registration survives script compilation cycles.
    /// </summary>
    [InitializeOnLoad]
    public static class DialogueBlackboardTypeRegistrar
    {
        // Matches the Run Subgraph node accent.
        private static readonly Color DialogueGraphColour = new Color(0.18f, 0.62f, 0.48f);

        static DialogueBlackboardTypeRegistrar()
        {
            BlackboardVariableTypeRegistry.Register(new BlackboardVariableTypeRegistry.Entry
            {
                TypeName     = "DialogueGraph",
                AccentColour = DialogueGraphColour,
                Factory      = () => new BlackboardVariableDialogueGraph(),
                Matcher      = v => v is BlackboardVariableDialogueGraph,
            });
        }
    }
}
