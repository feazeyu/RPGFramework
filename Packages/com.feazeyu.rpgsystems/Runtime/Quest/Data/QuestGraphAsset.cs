using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;

namespace QuestGraph.Runtime
{
    /// <summary>
    /// Which flavour of quest graph this asset represents.
    ///
    /// <see cref="Single"/> — the quest's internal logic. Uses Objective /
    /// Reward / CompleteQuest / FailQuest nodes. Executed by
    /// <see cref="QuestRunner"/> like any graph.
    ///
    /// <see cref="Chain"/> — a dependency graph of quests. Each RunSubgraph
    /// node references a Single quest asset; edges between RunSubgraph
    /// nodes mean prerequisite ("B depends on A"). Not walked linearly —
    /// <see cref="QuestChainRunner"/> maintains completion state and
    /// exposes the topological frontier as AvailableQuests.
    /// </summary>
    public enum QuestKind
    {
        Single = 0,
        Chain  = 1,
    }

    /// <summary>
    /// The quest-system ScriptableObject asset. Sibling of
    /// <see cref="DialogueGraphAsset"/>; both inherit from
    /// <see cref="GraphAsset"/> to share data and CRUD.
    ///
    /// Two entries appear under <b>Assets → Create → Quest</b>:
    /// <list type="bullet">
    /// <item><description><b>Quest Graph</b> — creates a Single quest</description></item>
    /// <item><description><b>Quest Chain</b> — creates a Chain graph (still a QuestGraphAsset, with <see cref="Kind"/> pre-set to <see cref="QuestKind.Chain"/>)</description></item>
    /// </list>
    /// The kind is serialised on the asset; the editor's node palette
    /// and the runtime behaviour both switch on it.
    /// </summary>
    [CreateAssetMenu(
        menuName = "RPGFramework/Quest/Quest Graph",
        fileName = "NewQuestGraph",
        order    = 1)]
    public class QuestGraphAsset : GraphAsset
    {
        [SerializeField]
        private QuestKind m_Kind = QuestKind.Single;

        public QuestKind Kind
        {
            get => m_Kind;
            set => m_Kind = value;
        }
    }
}
