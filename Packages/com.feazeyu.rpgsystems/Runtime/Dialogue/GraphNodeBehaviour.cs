using System.Collections;
using UnityEngine;

namespace DialogueGraph.Runtime
{
    /// <summary>
    /// Abstract MonoBehaviour that acts as an IGraphNodeHandler.
    /// Place this (or a subclass) on any GameObject in the scene, then either:
    ///   • Add it to the same GameObject as your GraphRunner — it will be
    ///     discovered automatically via GetComponentsInChildren.
    ///   • Call runner.RegisterHandler(this) manually.
    ///
    /// Use case — quest goals:
    ///   public class KillEnemiesGoal : GraphNodeBehaviour
    ///   {
    ///       public override string NodeTypeId => "QuestGoal_KillEnemies";
    ///       public int Required = 5;
    ///       private int m_Count;
    ///
    ///       public override IEnumerator Execute(NodeData node, GraphRunContext ctx)
    ///       {
    ///           m_Count = 0;
    ///           // EnemyManager fires an event; subscribe here
    ///           EnemyManager.OnKill += OnKill;
    ///           yield return new WaitUntil(() => m_Count >= Required);
    ///           EnemyManager.OnKill -= OnKill;
    ///           ctx.Follow("Out");   // advance the graph
    ///       }
    ///
    ///       private void OnKill() => m_Count++;
    ///   }
    ///
    /// The matching node in the graph editor must have NodeType == NodeTypeId.
    /// Register the type in NodeRegistry or tag the class with [DialogueNode].
    /// </summary>
    public abstract class GraphNodeBehaviour : MonoBehaviour, IGraphNodeHandler
    {
        [Tooltip("Must match the NodeType string used in the graph editor.")]
        [SerializeField] private string m_NodeTypeId;

        /// <summary>
        /// The node type ID this behaviour handles.
        /// Override the property if you prefer a hardcoded value over the Inspector field.
        /// </summary>
        public virtual string NodeTypeId => m_NodeTypeId;

        /// <summary>
        /// Implement your node logic here.  When done, call context.Follow("PortName")
        /// or context.End() to continue graph execution.
        /// </summary>
        public abstract IEnumerator Execute(NodeData node, GraphRunContext context);
    }
}
