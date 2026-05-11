using System;
using Feazeyu.RPGSystems.Dialogue;

namespace QuestGraph.Runtime
{
    /// <summary>
    /// Blackboard variable holding a reference to a
    /// <see cref="QuestGraphAsset"/> (graph-based quest). Used by
    /// Quest Reference (RunSubgraph) nodes in chain graphs so the
    /// chain runner can resolve which quest each node represents.
    ///
    /// Matches the pattern of the other asset-typed variables
    /// (Sprite, AudioClip, GameObject) in the Dialogue runtime:
    /// sealed concrete subclass of <see cref="BlackboardVariable{T}"/>
    /// so the SerializedProperty absolute-path lookup used by the
    /// blackboard inspector resolves stably.
    /// </summary>
    [Serializable]
    public sealed class BlackboardVariableQuestGraph : BlackboardVariable<QuestGraphAsset>
    {
        public BlackboardVariableQuestGraph() { }
        public BlackboardVariableQuestGraph(QuestGraphAsset v) : base(v) { }

        public override BlackboardVariable Clone()
            => new BlackboardVariableQuestGraph(m_Value)
            {
                Name    = Name,
                Guid    = Guid,
                Exposed = Exposed,
                Shared  = Shared,
            };
    }

    /// <summary>
    /// Blackboard variable holding a reference to a
    /// <see cref="QuestAsset"/> (simple quest, no graph). Chain
    /// graphs can reference simple quests via a Quest Reference
    /// node the same way they reference graph-based ones; the
    /// chain runner recognises the type at lookup time and either
    /// spawns a <see cref="QuestRunner"/> (graph) or waits for an
    /// external call to
    /// <see cref="QuestChainRunner.NotifyExternalQuestCompleted"/>
    /// (simple).
    /// </summary>
    [Serializable]
    public sealed class BlackboardVariableQuest : BlackboardVariable<QuestAsset>
    {
        public BlackboardVariableQuest() { }
        public BlackboardVariableQuest(QuestAsset v) : base(v) { }

        public override BlackboardVariable Clone()
            => new BlackboardVariableQuest(m_Value)
            {
                Name    = Name,
                Guid    = Guid,
                Exposed = Exposed,
                Shared  = Shared,
            };
    }
}
