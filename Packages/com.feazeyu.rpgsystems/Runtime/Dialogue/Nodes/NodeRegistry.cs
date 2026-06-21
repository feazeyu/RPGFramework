using System;
using System.Collections.Generic;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Metadata record for a single node type. The Editor reads this to populate
    /// the right-click "Add Node" menu and to generate default ports/fields when
    /// a node is created. Shared by every graph system (dialogue, quest, …).
    /// </summary>
    public class NodeInfo
    {
        public string   TypeId;          // unique key stored in NodeData.NodeType
        public string   DisplayName;
        public string   Category;        // used to group entries in the context menu
        public string   Description;
        public Color    AccentColor;
        public string   Icon;            // unicode glyph or resource path

        // Default port layout – editor uses these when creating a new node.
        public List<PortData> DefaultPorts = new List<PortData>();
        // Default field layout.
        public List<FieldData> DefaultFields = new List<FieldData>();
    }

    /// <summary>
    /// Abstract base for every graph system's node registry. Provides the shared
    /// infrastructure all registries need:
    /// <list type="bullet">
    /// <item><description>The universal flow/logic node type-id constants and accent
    /// colours, so a stored <c>NodeType</c> string means the same thing in any
    /// graph system (a "Start" node is a "Start" node everywhere).</description></item>
    /// <item><description>The lazily-built type lookup table plus the
    /// <see cref="Register"/> / <see cref="AllNodes"/> / <see cref="GetNode"/>
    /// plumbing.</description></item>
    /// <item><description><see cref="RegisterCommonNodes"/> — the flow/logic/scene
    /// nodes shared by every system — and <see cref="RegisterAttributeNodes{TAttr}"/>
    /// for reflection-based discovery.</description></item>
    /// </list>
    /// Concrete registries (<c>DialogueNodeRegistry</c>, <c>QuestNodeRegistry</c>)
    /// derive from this, override <see cref="Build"/> to add their own node set, and
    /// expose a static façade over a private singleton instance.
    /// </summary>
    public abstract class NodeRegistry
    {
        // ── Shared built-in type IDs ─────────────────────────────────────────
        // Universal across all graph systems.

        public const string TypeStart        = "Start";
        public const string TypeEnd          = "End";
        public const string TypeCondition    = "Condition";
        public const string TypeSetVariable  = "SetVariable";
        public const string TypeTriggerEvent = "TriggerEvent";
        public const string TypeWaitForEvent = "WaitForEvent";
        public const string TypeRunSubgraph  = "RunSubgraph";
        public const string TypeFindObject   = "find_object";
        public const string TypeDebugLog     = "debug_log";

        // ── Shared accent colours ────────────────────────────────────────────

        public static readonly Color ColFlow      = new Color(0.18f, 0.62f, 0.48f);
        public static readonly Color ColLogic     = new Color(0.94f, 0.65f, 0.20f);
        public static readonly Color ColEvent     = new Color(0.88f, 0.31f, 0.44f);
        public static readonly Color ColVariable  = new Color(0.69f, 0.42f, 0.97f);
        public static readonly Color ColStart     = new Color(0.34f, 0.78f, 0.34f);
        public static readonly Color ColEnd       = new Color(0.75f, 0.25f, 0.25f);

        // ── Lookup table ─────────────────────────────────────────────────────

        private Dictionary<string, NodeInfo> _registry;

        /// <summary>All node types this registry exposes, built on first access.</summary>
        protected IReadOnlyDictionary<string, NodeInfo> AllNodes
        {
            get { EnsureBuilt(); return _registry; }
        }

        /// <summary>Look up a single node type, or <c>null</c> if not registered.</summary>
        protected NodeInfo GetNode(string typeId)
        {
            EnsureBuilt();
            return _registry.TryGetValue(typeId, out var info) ? info : null;
        }

        /// <summary>Add (or overwrite) a node type in the table.</summary>
        protected void Register(NodeInfo info) => _registry[info.TypeId] = info;

        private void EnsureBuilt()
        {
            if (_registry != null) return;
            _registry = new Dictionary<string, NodeInfo>();
            Build();
        }

        /// <summary>
        /// Template method: derived registries populate their node set here,
        /// typically by calling <see cref="RegisterCommonNodes"/> first, then
        /// registering their own built-ins and <see cref="RegisterAttributeNodes{TAttr}"/>.
        /// </summary>
        protected abstract void Build();

        // ── Shared built-in nodes ────────────────────────────────────────────

        /// <summary>
        /// Registers the flow/logic/scene/event nodes that every graph system
        /// shares. Nodes whose layout differs between systems — <c>Condition</c>
        /// and <c>RunSubgraph</c> — are intentionally left to each derived
        /// registry.
        /// </summary>
        protected void RegisterCommonNodes()
        {
            Register(new NodeInfo
            {
                TypeId = TypeStart, DisplayName = "Start", Category = "Flow",
                Description = "Entry point. Execution begins here.",
                AccentColor = ColStart, Icon = "▶",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single }
                }
            });

            Register(new NodeInfo
            {
                TypeId = TypeEnd, DisplayName = "End", Category = "Flow",
                Description = "Terminates graph execution.",
                AccentColor = ColEnd, Icon = "■",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In", Direction = PortDirection.Input, Capacity = PortCapacity.Multi }
                }
            });

            Register(new NodeInfo
            {
                TypeId = TypeSetVariable, DisplayName = "Set Variable", Category = "Logic",
                Description = "Writes a value to a Blackboard variable.",
                AccentColor = ColVariable, Icon = "✎",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single }
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Variable", TypeName = "System.String" },
                    new FieldData { FieldName = "Value",    TypeName = "System.String" },
                }
            });

            Register(new NodeInfo
            {
                TypeId = TypeTriggerEvent, DisplayName = "Trigger Event", Category = "Events",
                Description = "Fires a game event channel.",
                AccentColor = ColEvent, Icon = "⚡",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single }
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Event Channel", TypeName = "UnityEngine.ScriptableObject" },
                }
            });

            Register(new NodeInfo
            {
                TypeId = TypeWaitForEvent, DisplayName = "Wait For Event", Category = "Events",
                Description = "Suspends execution until a game event is received.",
                AccentColor = ColEvent, Icon = "⏳",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single }
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Event Channel", TypeName = "UnityEngine.ScriptableObject" },
                }
            });

            Register(new NodeInfo
            {
                TypeId = TypeDebugLog, DisplayName = "Debug Log", Category = "Debug",
                Description = "Prints a message to the Unity console and continues.",
                AccentColor = new Color(0.55f, 0.55f, 0.55f), Icon = "⬛",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Message", TypeName = "System.String" },
                }
            });

            Register(new NodeInfo
            {
                TypeId = TypeFindObject, DisplayName = "Find Object", Category = "Scene",
                Description = "Finds a scene GameObject by name or tag and stores it in a blackboard variable.",
                AccentColor = ColVariable, Icon = "⌖",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",       Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Found",    Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "NotFound", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Mode",   TypeName = "System.String",            InlineValue = "ByName" },
                    new FieldData { FieldName = "Value",  TypeName = "System.String" },
                    new FieldData { FieldName = "Target", TypeName = "UnityEngine.GameObject" },
                }
            });
        }

        /// <summary>
        /// Scans all loaded assemblies for classes tagged with <typeparamref name="TAttr"/>
        /// and registers the <see cref="NodeInfo"/> produced by <paramref name="factory"/>.
        /// Types whose id is already registered (built-ins) are skipped, as are
        /// factories that return <c>null</c>.
        /// </summary>
        protected void RegisterAttributeNodes<TAttr>(Func<TAttr, NodeInfo> factory)
            where TAttr : Attribute
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (System.Reflection.ReflectionTypeLoadException e) { types = e.Types; }

                foreach (var type in types)
                {
                    if (type == null) continue;
                    var attr = (TAttr)Attribute.GetCustomAttribute(type, typeof(TAttr));
                    if (attr == null) continue;

                    var info = factory(attr);
                    if (info == null || _registry.ContainsKey(info.TypeId)) continue;
                    Register(info);
                }
            }
        }
    }
}
