using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;
using Feazeyu.RPGSystems.Character;
using Feazeyu.RPGSystems.Inventory;
using QuestGraph.Runtime;

namespace QuestGraph.Nodes
{
    /// <summary>
    /// Builds the composed gate predicate for an objective node.
    ///
    /// Gate nodes are decorators discovered the same way dialogue Requirement
    /// nodes are: any node whose Out is wired into the objective's <c>In</c> port
    /// and whose type is a known gate type contributes a predicate. The objective
    /// only counts a <see cref="ProgressEvent"/> when <b>every</b> gate accepts it.
    ///
    /// Gates evaluate against world/player state (and, where relevant, the event's
    /// <see cref="ProgressEvent.Subject"/>) at the instant progress is offered.
    /// Capability gates fall back to the player — with a one-time warning — when
    /// the subject can't satisfy them (e.g. a creature with no inventory).
    /// </summary>
    public static class ObjectiveGates
    {
        /// <summary>
        /// Returns a predicate that is the AND of every gate attached to the
        /// objective's In port, or null when no gates are attached.
        /// </summary>
        public static Func<ProgressEvent, bool> Compose(NodeData objective, GraphRunContext ctx)
        {
            var preds = new List<Func<ProgressEvent, bool>>();

            foreach (var edge in ctx.Graph.Edges)
            {
                if (edge.InputNodeGuid != objective.Guid || edge.InputPortName != "In") continue;
                var src = ctx.Graph.GetNode(edge.OutputNodeGuid);
                if (src == null) continue;

                var pred = Build(src, ctx);
                if (pred != null) preds.Add(pred);
            }

            if (preds.Count == 0) return null;
            return e =>
            {
                foreach (var p in preds)
                    if (!p(e)) return false;
                return true;
            };
        }

        private static Func<ProgressEvent, bool> Build(NodeData gate, GraphRunContext ctx)
        {
            switch (gate.NodeType)
            {
                case QuestNodeRegistry.TypeGateFlag:     return FlagGate(gate, ctx);
                case QuestNodeRegistry.TypeGateLocation: return LocationGate(gate, ctx);
                case QuestNodeRegistry.TypeGateItem:     return ItemGate(gate, ctx);
                default:                                 return null;
            }
        }


        private static Func<ProgressEvent, bool> FlagGate(NodeData g, GraphRunContext ctx)
        {
            var guid = ctx.GetLinkedGuid(g, "Variable");
            var op   = ctx.ResolveString(g, "Operator");
            var val  = ctx.ResolveString(g, "Value");

            return _ =>
            {
                if (string.IsNullOrEmpty(guid)) return true;
                var bb = ctx.RuntimeBlackboard.GetVariable(guid);
                if (bb == null) return true;
                return Compare(bb.ObjectValue, op, val);
            };
        }


        private static Func<ProgressEvent, bool> LocationGate(NodeData g, GraphRunContext ctx)
        {
            var target = ResolveTransform(g, ctx, "Target");
            float.TryParse(ctx.ResolveString(g, "Radius"), NumberStyles.Float, CultureInfo.InvariantCulture, out float radius);
            if (radius <= 0f) radius = 2f;
            bool useSubject = string.Equals(ctx.ResolveString(g, "Subject"), "Subject", StringComparison.OrdinalIgnoreCase);

            return e =>
            {
                if (target == null) return false;

                Vector3 pos;
                if (useSubject && e.Subject != null)      pos = e.Subject.transform.position;
                else if (useSubject && e.HasPosition)     pos = e.Position;
                else
                {
                    var player = ResolvePlayer();
                    if (player == null) return false;
                    pos = player.position;
                }
                return Vector2.Distance(pos, target.position) <= radius;
            };
        }


        private static Func<ProgressEvent, bool> ItemGate(NodeData g, GraphRunContext ctx)
        {
            int.TryParse(ctx.ResolveString(g, "ItemId"), out int itemId);
            int.TryParse(ctx.ResolveString(g, "Count"),  out int count);
            if (count <= 0) count = 1;
            var op = ctx.ResolveString(g, "Operator");
            if (string.IsNullOrEmpty(op)) op = ">=";

            bool warned = false;

            return e =>
            {
                IItemContainer container = null;
                if (e.Subject != null)
                    container = e.Subject.GetComponentInChildren<IItemContainer>();

                if (container == null)
                {
                    if (!warned)
                    {
                        var who = e.Subject != null ? e.Subject.name : "null";
                        Debug.LogWarning($"[ItemGate] Subject '{who}' has no inventory; defaulting to the player.");
                        warned = true;
                    }
                    var player = ResolvePlayer();
                    if (player != null)
                        container = player.GetComponentInChildren<IItemContainer>();
                }

                if (container == null) return false;
                return CompareInt(container.CountItem(itemId), op, count);
            };
        }


        private static Transform ResolveTransform(NodeData node, GraphRunContext ctx, string field)
        {
            var guid = ctx.GetLinkedGuid(node, field);
            if (!string.IsNullOrEmpty(guid))
            {
                var v = ctx.RuntimeBlackboard.GetVariable(guid);
                if (v?.ObjectValue is Transform t)   return t;
                if (v?.ObjectValue is GameObject go) return go.transform;
            }

            var name = ctx.ResolveString(node, field);
            if (!string.IsNullOrEmpty(name))
            {
                var found = GameObject.Find(name);
                if (found != null) return found.transform;
            }
            return null;
        }

        private static Transform ResolvePlayer()
        {
            var pc = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (pc != null) return pc.transform;
            var tagged = GameObject.FindWithTag("Player");
            return tagged != null ? tagged.transform : null;
        }

        private static bool CompareInt(int lhs, string op, int rhs) => op switch
        {
            "==" => lhs == rhs,
            "!=" => lhs != rhs,
            ">"  => lhs >  rhs,
            ">=" => lhs >= rhs,
            "<"  => lhs <  rhs,
            "<=" => lhs <= rhs,
            _    => lhs >= rhs,
        };

        private static bool Compare(object lhs, string op, string rhsStr)
        {
            if (lhs == null) return false;

            if (double.TryParse(lhs.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lhsN) &&
                double.TryParse(rhsStr,         NumberStyles.Float, CultureInfo.InvariantCulture, out double rhsN))
            {
                return op switch
                {
                    "==" => lhsN == rhsN,
                    "!=" => lhsN != rhsN,
                    ">"  => lhsN >  rhsN,
                    ">=" => lhsN >= rhsN,
                    "<"  => lhsN <  rhsN,
                    "<=" => lhsN <= rhsN,
                    _    => false,
                };
            }

            if (lhs is bool lhsB && bool.TryParse(rhsStr, out bool rhsB))
            {
                return op switch
                {
                    "==" => lhsB == rhsB,
                    "!=" => lhsB != rhsB,
                    _    => false,
                };
            }

            return op switch
            {
                "==" => string.Equals(lhs.ToString(), rhsStr, StringComparison.Ordinal),
                "!=" => !string.Equals(lhs.ToString(), rhsStr, StringComparison.Ordinal),
                _    => false,
            };
        }
    }
}
