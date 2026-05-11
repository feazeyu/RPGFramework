using System.Collections;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Finds a scene GameObject by name or tag and writes it to a linked
    /// blackboard GameObject variable.
    ///
    /// Fields:
    ///   Mode   — "ByName" (default) or "ByTag"
    ///   Value  — the name / tag string to search for (inline or blackboard String)
    ///   Target — blackboard GameObject variable that receives the result
    ///
    /// Ports: Found (object was located), NotFound.
    /// </summary>
    [DialogueNode("find_object", "Find Object", "Scene",
        "Finds a scene GameObject by name or tag and stores it in a blackboard variable.")]
    public class FindObjectNodeHandler : IGraphNodeHandler
    {
        public string NodeTypeId => "find_object";

        public IEnumerator Execute(NodeData node, GraphRunContext ctx)
        {
            string mode  = ctx.ResolveString(node, "Mode");
            string value = ctx.ResolveString(node, "Value");

            GameObject found = string.Equals(mode, "ByTag", System.StringComparison.OrdinalIgnoreCase)
                ? FindByTag(value)
                : GameObject.Find(value);

            if (found != null)
            {
                var targetField = ctx.GetField(node, "Target");
                if (targetField != null && !string.IsNullOrEmpty(targetField.LinkedVariableGuid))
                {
                    var bbVar = ctx.RuntimeBlackboard.GetVariable(targetField.LinkedVariableGuid);
                    if (bbVar != null) bbVar.ObjectValue = found;
                }
                ctx.Follow("Found");
            }
            else
            {
                ctx.Follow("NotFound");
            }

            yield break;
        }

        private static GameObject FindByTag(string tag)
        {
            try   { return GameObject.FindWithTag(tag); }
            catch (UnityException) { return null; } // tag doesn't exist in TagManager
        }
    }
}
