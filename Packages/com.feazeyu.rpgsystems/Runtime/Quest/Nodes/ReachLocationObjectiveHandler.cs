using System.Collections;
using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;
using Feazeyu.RPGSystems.Quest;
using Feazeyu.RPGSystems.Character;

namespace Feazeyu.RPGSystems.Quest.Nodes
{
    /// <summary>
    /// Handler for the Reach Location objective node (typeId = "obj_location").
    ///
    /// Waits until the player is within Radius of Target, then follows
    /// "Completed". Use this to gate the next objective behind arriving
    /// somewhere — connect Completed → next objective's In. For a
    /// "do X while staying in zone Y" constraint, attach a Location Gate
    /// to the guarded objective's In port instead.
    ///
    /// <b>Target resolution</b> (first match wins):
    ///   1. Blackboard variable of type Transform linked to the Target field.
    ///   2. Inline value treated as a scene object name (GameObject.Find).
    /// </summary>
    [QuestNode(QuestNodeRegistry.TypeObjLocation, "Reach Location", "Objectives",
        "Enter a radius around a target, then follow Completed.")]
    public class ReachLocationObjectiveHandler : IGraphNodeHandler
    {
        /// <inheritdoc/>
        public string NodeTypeId => QuestNodeRegistry.TypeObjLocation;

        /// <inheritdoc/>
        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            var runner = ctx.Runner as QuestRunner;
            if (runner == null) { ctx.Follow("Failed"); yield break; }

            var title  = ctx.ResolveString(node, "Title");
            var desc   = ctx.ResolveString(node, "Description");
            float.TryParse(ctx.ResolveString(node, "Radius"), out float radius);
            bool.TryParse(ctx.ResolveString(node, "Optional"),   out bool optional);
            if (radius <= 0f) radius = 2f;

            var info = new ObjectiveInfo
            {
                NodeGuid    = node.Guid,
                Title       = title,
                Description = desc,
                Optional    = optional,
            };

            Transform target = ResolveTarget(node, ctx);

            var player = FindPlayer();

            runner.RegisterObjective(info);

            while (runner.IsRunning)
            {
                if (player != null && target != null &&
                    Vector2.Distance(player.position, target.position) <= radius)
                    break;
                yield return null;
            }

            if (!runner.IsRunning) yield break;

            runner.UnregisterObjective(node.Guid, outcome: true);
            ctx.Follow("Completed");
        }


        private static Transform ResolveTarget(NodeData node, GraphRunContext ctx)
        {
            var guid = ctx.GetLinkedGuid(node, "Target");
            if (!string.IsNullOrEmpty(guid))
            {
                var v = ctx.RuntimeBlackboard.GetVariable(guid);
                if (v?.ObjectValue is Transform t)   return t;
                if (v?.ObjectValue is GameObject go) return go.transform;
            }

            var name = ctx.ResolveString(node, "Target");
            if (!string.IsNullOrEmpty(name))
            {
                var found = GameObject.Find(name);
                if (found != null) return found.transform;
            }

            Debug.LogWarning("[ReachLocationObjectiveHandler] Target not resolved.");
            return null;
        }

        private static Transform FindPlayer()
        {
            var pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) return pc.transform;

            var tagged = GameObject.FindWithTag("Player");
            if (tagged != null) return tagged.transform;

            Debug.LogWarning("[ReachLocationObjectiveHandler] No PlayerController or 'Player'-tagged object found.");
            return null;
        }
    }
}
