using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using DialogueGraph.Runtime;

namespace DialogueGraph.Editor
{
    /// <summary>
    /// Maps a node type-id (see <see cref="NodeRegistry"/> or
    /// <see cref="QuestGraph.Runtime.QuestNodeRegistry"/>) to an optional
    /// "decorator" callback that injects extra UI into a node card's
    /// field container.
    ///
    /// Decorators register themselves at editor load time via
    /// <c>[InitializeOnLoad]</c> static constructors — see
    /// <see cref="ChoiceBranchDecorator"/> for the canonical example.
    ///
    /// The decorator signature is (container, node, asset, rebuild):
    ///   • container — the node's fields VisualElement to add UI into
    ///   • node      — the NodeData the card is rendering
    ///   • asset     — the owning <see cref="GraphAsset"/> (for mutation
    ///                 + SetDirty). Typed as the shared base so a single
    ///                 decorator can handle both Dialogue and Quest graphs.
    ///   • rebuild   — call this after mutating ports/fields to have the
    ///                 node view re-run BuildPorts / BuildFields while
    ///                 preserving edge connections
    /// </summary>
    public static class NodeViewDecoratorRegistry
    {
        public delegate void Decorator(
            VisualElement container,
            NodeData      node,
            GraphAsset    asset,
            Action        rebuild);

        private static readonly Dictionary<string, Decorator> s_Decorators
            = new Dictionary<string, Decorator>();

        /// <summary>
        /// Register a decorator for the given node type. Later registrations
        /// for the same type overwrite earlier ones (last-write-wins), which
        /// is what you want after a domain reload that re-runs static ctors.
        /// </summary>
        public static void Register(string nodeTypeId, Decorator decorator)
        {
            if (string.IsNullOrEmpty(nodeTypeId) || decorator == null) return;
            s_Decorators[nodeTypeId] = decorator;
        }

        /// <summary>
        /// Returns the decorator for the given node type, or null if none
        /// is registered. Callers typically use <c>?.Invoke(...)</c>.
        /// </summary>
        public static Decorator Get(string nodeTypeId)
        {
            if (string.IsNullOrEmpty(nodeTypeId)) return null;
            return s_Decorators.TryGetValue(nodeTypeId, out var d) ? d : null;
        }

        /// <summary>Remove a decorator. Mainly useful for editor tests.</summary>
        public static void Unregister(string nodeTypeId)
        {
            if (string.IsNullOrEmpty(nodeTypeId)) return;
            s_Decorators.Remove(nodeTypeId);
        }
    }
}
