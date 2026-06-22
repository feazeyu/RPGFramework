using System.Collections;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Instantiates a prefab and continues. The prefab (and the optional parent)
    /// come from blackboard variables, because object references can't be stored
    /// inline on a node field — link a GameObject variable holding the prefab.
    ///
    /// Fields:
    ///   Prefab — blackboard GameObject variable holding the prefab to instantiate (required)
    ///   Parent — optional blackboard Transform (or GameObject) variable; the new
    ///            instance is parented under it. Empty → spawned at the scene root.
    ///   Result — optional blackboard GameObject variable that receives the spawned instance.
    ///
    /// Routes to Out (always), or to Failure when no prefab is available.
    /// </summary>
    [DialogueNode("spawn_prefab", "Spawn Prefab", "Scene",
        "Instantiates a prefab from a blackboard variable, optionally under a parent Transform.")]
    public class SpawnPrefabNodeHandler : IGraphNodeHandler
    {
        public string NodeTypeId => "spawn_prefab";

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            var prefab = ResolveGameObject(ctx, node, "Prefab");
            if (prefab == null)
            {
                Debug.LogWarning("[SpawnPrefab] No Prefab linked — connect a blackboard GameObject variable holding the prefab.");
                ctx.Follow("Failure");
                yield break;
            }

            var parent = ResolveParent(ctx, node, "Parent");

            // worldPositionStays:false so the instance keeps the prefab's authored
            // local transform relative to the parent (i.e. it appears at the
            // parent), which is the least surprising "spawn under parent" behaviour.
            var instance = parent != null
                ? Object.Instantiate(prefab, parent, false)
                : Object.Instantiate(prefab);

            // Optionally publish the spawned instance so later nodes can reference it.
            var resultField = ctx.GetField(node, "Result");
            if (resultField != null && !string.IsNullOrEmpty(resultField.LinkedVariableGuid))
            {
                var v = ctx.RuntimeBlackboard.GetVariable(resultField.LinkedVariableGuid);
                if (v != null) v.ObjectValue = instance;
            }

            ctx.Follow("Out");
            yield break;
        }

        private static GameObject ResolveGameObject(GraphRunContext ctx, NodeData node, string fieldName)
        {
            var field = ctx.GetField(node, fieldName);
            if (field == null || string.IsNullOrEmpty(field.LinkedVariableGuid)) return null;
            return ctx.RuntimeBlackboard.GetVariable(field.LinkedVariableGuid)?.ObjectValue as GameObject;
        }

        private static Transform ResolveParent(GraphRunContext ctx, NodeData node, string fieldName)
        {
            var field = ctx.GetField(node, fieldName);
            if (field == null || string.IsNullOrEmpty(field.LinkedVariableGuid)) return null;

            // Accept either a Transform variable or a GameObject variable.
            var obj = ctx.RuntimeBlackboard.GetVariable(field.LinkedVariableGuid)?.ObjectValue;
            return obj switch
            {
                Transform t  => t,
                GameObject g => g.transform,
                _            => null,
            };
        }
    }
}
