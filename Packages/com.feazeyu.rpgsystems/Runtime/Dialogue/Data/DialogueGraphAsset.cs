using UnityEngine;

namespace DialogueGraph.Runtime
{
    /// <summary>
    /// The dialogue-system ScriptableObject asset. All fields and CRUD
    /// live on <see cref="GraphAsset"/>; this subclass exists only to
    /// carry the <c>[CreateAssetMenu]</c> so dialogue graphs get their
    /// own entry under <b>Assets → Create → Dialogue</b>.
    ///
    /// Serialisation-compatible with the pre-split asset: the full type
    /// name and every serialised field (<c>m_Nodes</c>, <c>m_Edges</c>,
    /// <c>m_Blackboard</c>, <c>ViewTransform</c>, <c>BlackboardPosition</c>)
    /// are unchanged — they just live on the base now.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Dialogue/Dialogue Graph",
        fileName = "NewDialogueGraph",
        order    = 1)]
    public class DialogueGraphAsset : GraphAsset { }
}
