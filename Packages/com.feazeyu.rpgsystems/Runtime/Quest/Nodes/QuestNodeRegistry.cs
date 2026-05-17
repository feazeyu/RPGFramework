using System;
using System.Collections.Generic;
using UnityEngine;
using Feazeyu.RPGSystems.Dialogue;

namespace QuestGraph.Runtime
{
    /// <summary>
    /// Attribute equivalent to <see cref="DialogueNodeAttribute"/> for the
    /// quest palette. Tag a class with <c>[QuestNode(...)]</c> and
    /// <see cref="QuestNodeRegistry"/> picks it up via reflection at
    /// editor load time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class QuestNodeAttribute : Attribute
    {
        public string NodeTypeID      { get; }
        public string DisplayName { get; }
        public string Category    { get; }
        public string Description { get; }
        public string Icon        { get; }

        public QuestNodeAttribute(
            string typeId,
            string displayName,
            string category    = "General",
            string description = "",
            string icon        = "")
        {
            NodeTypeID      = typeId;
            DisplayName = displayName;
            Category    = category;
            Description = description;
            Icon        = icon;
        }
    }

    /// <summary>
    /// Parallel to <see cref="NodeRegistry"/> but scoped to the Quest
    /// editor's "Add Node" menu. Contains every node type either flavour
    /// of quest graph can use; the window filters by
    /// <see cref="QuestGraphAsset.Kind"/> via <see cref="ForKind"/>.
    ///
    /// Type-id constants for shared flow/logic nodes mirror
    /// <see cref="NodeRegistry"/> so a node's stored <c>NodeType</c>
    /// string is universal — a "Start" node means the same thing
    /// regardless of which graph system it lives in.
    /// </summary>
    public static class QuestNodeRegistry
    {
        private static Dictionary<string, DialogueNodeInfo> s_Registry;

        public static IReadOnlyDictionary<string, DialogueNodeInfo> All
        {
            get { EnsureBuilt(); return s_Registry; }
        }

        // ── Built-in type IDs ────────────────────────────────────────────────

        public const string TypeStart          = NodeRegistry.TypeStart;
        public const string TypeEnd            = NodeRegistry.TypeEnd;
        public const string TypeSequence       = NodeRegistry.TypeSequence;
        public const string TypeSelector       = NodeRegistry.TypeSelector;
        public const string TypeCondition      = NodeRegistry.TypeCondition;
        public const string TypeSetVariable    = NodeRegistry.TypeSetVariable;
        public const string TypeTriggerEvent   = NodeRegistry.TypeTriggerEvent;
        public const string TypeWaitForEvent   = NodeRegistry.TypeWaitForEvent;
        public const string TypeRunSubgraph    = NodeRegistry.TypeRunSubgraph;

        public const string TypeFindObject     = NodeRegistry.TypeFindObject;
        public const string TypeDebugLog       = NodeRegistry.TypeDebugLog;

        public const string TypeObjective      = "Objective";
        public const string TypeReward         = "Reward";
        public const string TypeCompleteQuest  = "CompleteQuest";
        public const string TypeFailQuest      = "FailQuest";
        public const string TypeSpawnItem      = "spawn_item";

        // Concrete objective node types
        public const string TypeObjKill      = "obj_kill";
        public const string TypeObjLocation  = "obj_location";
        public const string TypeObjCollect   = "obj_collect";
        public const string TypeObjDeliver   = "obj_deliver";

        // ── Accent colours ───────────────────────────────────────────────────

        public static readonly Color ColFlow      = NodeRegistry.ColFlow;
        public static readonly Color ColLogic     = NodeRegistry.ColLogic;
        public static readonly Color ColEvent     = NodeRegistry.ColEvent;
        public static readonly Color ColVariable  = NodeRegistry.ColVariable;
        public static readonly Color ColStart     = NodeRegistry.ColStart;
        public static readonly Color ColEnd       = NodeRegistry.ColEnd;

        public static readonly Color ColObjective = new Color(0.95f, 0.72f, 0.24f); // amber
        public static readonly Color ColReward    = new Color(0.90f, 0.80f, 0.30f); // gold
        public static readonly Color ColComplete  = new Color(0.34f, 0.78f, 0.34f); // green
        public static readonly Color ColFail      = new Color(0.85f, 0.28f, 0.28f); // red
        public static readonly Color ColSubgraph  = new Color(0.62f, 0.55f, 0.88f); // violet — quest reference

        // ── Palette filters ──────────────────────────────────────────────────
        // Declared by type-id string so the runtime info records stay
        // kind-agnostic — the info itself is just data, the "where it shows
        // up" decision is localised here.

        private static readonly HashSet<string> s_SinglePalette = new HashSet<string>
        {
            TypeStart, TypeEnd,
            TypeSequence, TypeSelector,
            TypeCondition, TypeSetVariable,
            TypeTriggerEvent, TypeWaitForEvent,
            TypeFindObject, TypeDebugLog,
            TypeObjective, TypeReward,
            TypeCompleteQuest, TypeFailQuest,
            TypeSpawnItem,
            TypeObjKill, TypeObjLocation, TypeObjCollect, TypeObjDeliver,
        };

        private static readonly HashSet<string> s_ChainPalette = new HashSet<string>
        {
            TypeStart, TypeEnd,
            TypeCondition, TypeSetVariable,
            TypeTriggerEvent,
            TypeFindObject, TypeDebugLog,
            TypeRunSubgraph,
        };

        /// <summary>
        /// Returns the subset of <see cref="All"/> appropriate for a
        /// graph of the given kind. The window's "Add Node" context
        /// menu is populated from this.
        /// </summary>
        public static IReadOnlyDictionary<string, DialogueNodeInfo> ForKind(QuestKind kind)
        {
            EnsureBuilt();
            var allow = kind == QuestKind.Chain ? s_ChainPalette : s_SinglePalette;

            var filtered = new Dictionary<string, DialogueNodeInfo>();
            foreach (var kv in s_Registry)
                if (allow.Contains(kv.Key))
                    filtered[kv.Key] = kv.Value;
            return filtered;
        }

        /// <summary>Whether a node type is allowed in the given graph kind.</summary>
        public static bool IsAllowedIn(string nodeTypeId, QuestKind kind)
        {
            var allow = kind == QuestKind.Chain ? s_ChainPalette : s_SinglePalette;
            return allow.Contains(nodeTypeId);
        }

        // ── Build ────────────────────────────────────────────────────────────

        private static void EnsureBuilt()
        {
            if (s_Registry != null) return;
            s_Registry = new Dictionary<string, DialogueNodeInfo>();
            RegisterSharedFlow();
            RegisterQuestSpecific();
            RegisterAttributeNodes();
        }

        private static void Register(DialogueNodeInfo info) => s_Registry[info.TypeId] = info;

        private static void RegisterSharedFlow()
        {
            Register(new DialogueNodeInfo
            {
                TypeId = TypeStart, DisplayName = "Start", Category = "Flow",
                Description = "Entry point. Execution begins here.",
                AccentColor = ColStart, Icon = "▶",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single }
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeEnd, DisplayName = "End", Category = "Flow",
                Description = "Terminates graph execution.",
                AccentColor = ColEnd, Icon = "■",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In", Direction = PortDirection.Input, Capacity = PortCapacity.Multi }
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeSequence, DisplayName = "Sequence", Category = "Flow",
                Description = "Runs children in order. Fails on first child failure.",
                AccentColor = ColFlow, Icon = "→",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single }
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeSelector, DisplayName = "Selector", Category = "Flow",
                Description = "Tries children in order; succeeds on first child success.",
                AccentColor = ColFlow, Icon = "?",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single }
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeCondition, DisplayName = "Condition", Category = "Logic",
                Description = "Evaluates a blackboard variable. Routes to True or False output.",
                AccentColor = ColLogic, Icon = "◆",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",    Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "True",  Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "False", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Variable", TypeName = "System.String" },
                    new FieldData { FieldName = "Operator", TypeName = "System.String", InlineValue = "==" },
                    new FieldData { FieldName = "Value",    TypeName = "System.String" },
                }
            });

            Register(new DialogueNodeInfo
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

            Register(new DialogueNodeInfo
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

            Register(new DialogueNodeInfo
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

            Register(new DialogueNodeInfo
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

            Register(new DialogueNodeInfo
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

            Register(new DialogueNodeInfo
            {
                TypeId = TypeRunSubgraph, DisplayName = "Quest Reference", Category = "Quest",
                Description = "References another quest asset. In a chain graph, edges between " +
                              "these nodes mean 'prerequisite' — downstream quests unlock when " +
                              "all upstream ones have completed.",
                AccentColor = ColSubgraph, Icon = "⊞",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Multi }
                },
                DefaultFields = new List<FieldData>
                {
                    // Linked to a blackboard variable of type QuestGraph
                    // (holds a QuestGraphAsset for graph-based quests) or
                    // Quest (holds a QuestAsset for simple quests). The
                    // TypeName here is metadata only — QuestChainRunner
                    // resolves the actual type at runtime.
                    new FieldData { FieldName = "Quest", TypeName = "QuestGraph.Runtime.QuestReference" },
                }
            });
        }

        private static void RegisterQuestSpecific()
        {
            Register(new DialogueNodeInfo
            {
                TypeId = TypeObjective, DisplayName = "Objective", Category = "Quest",
                Description = "A single quest objective. Completes when the runner calls " +
                              "CompleteObjective(), fails on FailObjective(). Routes to " +
                              "Completed or Failed accordingly.",
                AccentColor = ColObjective, Icon = "◎",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",        Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Completed", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Failed",    Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Title",       TypeName = "System.String" },
                    new FieldData { FieldName = "Description", TypeName = "System.String" },
                    new FieldData { FieldName = "Optional",    TypeName = "System.Boolean", InlineValue = "False" },
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeReward, DisplayName = "Reward", Category = "Quest",
                Description = "Grants rewards to the player (items, XP, currency). " +
                              "Fires OnRewardGranted and follows Out.",
                AccentColor = ColReward, Icon = "✦",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single }
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "XP",       TypeName = "System.Int32",  InlineValue = "0" },
                    new FieldData { FieldName = "Currency", TypeName = "System.Int32",  InlineValue = "0" },
                    new FieldData { FieldName = "Item",     TypeName = "UnityEngine.ScriptableObject" },
                    new FieldData { FieldName = "Quantity", TypeName = "System.Int32",  InlineValue = "1" },
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeCompleteQuest, DisplayName = "Complete Quest", Category = "Quest",
                Description = "Terminal node. Marks the quest as successfully completed and ends the graph.",
                AccentColor = ColComplete, Icon = "✓",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In", Direction = PortDirection.Input, Capacity = PortCapacity.Multi }
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeFailQuest, DisplayName = "Fail Quest", Category = "Quest",
                Description = "Terminal node. Marks the quest as failed and ends the graph.",
                AccentColor = ColFail, Icon = "✗",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In", Direction = PortDirection.Input, Capacity = PortCapacity.Multi },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Reason", TypeName = "System.String" },
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeSpawnItem, DisplayName = "Spawn Item", Category = "Quest",
                Description = "Instantiates an item prefab and places it into an IItemContainer " +
                              "found on the Target GameObject. Routes to Success or Failure.",
                AccentColor = ColReward, Icon = "⊕",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",      Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Success", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Failure", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "ItemId", TypeName = "System.Int32",          InlineValue = "0" },
                    new FieldData { FieldName = "Target", TypeName = "UnityEngine.GameObject" },
                }
            });

            // ── Concrete objective nodes ──────────────────────────────────────

            Register(new DialogueNodeInfo
            {
                TypeId = TypeObjKill, DisplayName = "Kill Count", Category = "Objectives",
                Description = "Completes once the player kills the required number of enemies " +
                              "with the given tag. Connect sequentially to chain objectives.",
                AccentColor = ColObjective, Icon = "⚔",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",        Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Completed", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Failed",    Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Title",       TypeName = "System.String", InlineValue = "Kill enemies" },
                    new FieldData { FieldName = "Description", TypeName = "System.String" },
                    new FieldData { FieldName = "Tag",         TypeName = "System.String", InlineValue = "Enemy" },
                    new FieldData { FieldName = "Count",       TypeName = "System.Int32",  InlineValue = "5" },
                    new FieldData { FieldName = "Optional",    TypeName = "System.Boolean", InlineValue = "False" },
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeObjLocation, DisplayName = "Reach Location", Category = "Objectives",
                Description = "Completes once the player is within Radius of Target. " +
                              "Continuous=true: follows Out immediately and monitors in background — " +
                              "if player leaves the area, the quest fails.",
                AccentColor = ColObjective, Icon = "◉",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",        Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Completed", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Failed",    Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Out",       Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Title",       TypeName = "System.String",  InlineValue = "Reach location" },
                    new FieldData { FieldName = "Description", TypeName = "System.String" },
                    new FieldData { FieldName = "Target",      TypeName = "UnityEngine.Transform" },
                    new FieldData { FieldName = "Radius",      TypeName = "System.Single",  InlineValue = "2" },
                    new FieldData { FieldName = "Continuous",  TypeName = "System.Boolean", InlineValue = "False" },
                    new FieldData { FieldName = "Optional",    TypeName = "System.Boolean", InlineValue = "False" },
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeObjCollect, DisplayName = "Collect Item", Category = "Objectives",
                Description = "Completes once the player carries at least Count of the specified item. " +
                              "Continuous=true: follows Out immediately and monitors in background — " +
                              "if the player loses the item, the quest fails.",
                AccentColor = ColObjective, Icon = "⬡",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",        Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Completed", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Failed",    Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Out",       Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Title",       TypeName = "System.String", InlineValue = "Collect item" },
                    new FieldData { FieldName = "Description", TypeName = "System.String" },
                    new FieldData { FieldName = "ItemId",      TypeName = "System.Int32",  InlineValue = "0" },
                    new FieldData { FieldName = "Count",       TypeName = "System.Int32",  InlineValue = "1" },
                    new FieldData { FieldName = "Continuous",  TypeName = "System.Boolean", InlineValue = "False" },
                    new FieldData { FieldName = "Optional",    TypeName = "System.Boolean", InlineValue = "False" },
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeObjDeliver, DisplayName = "Deliver Item", Category = "Objectives",
                Description = "Completes when the player interacts with the NPC while carrying " +
                              "at least Count of the item. Items are removed from inventory on delivery.",
                AccentColor = ColObjective, Icon = "↗",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",        Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Completed", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Failed",    Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Title",       TypeName = "System.String", InlineValue = "Deliver item" },
                    new FieldData { FieldName = "Description", TypeName = "System.String" },
                    new FieldData { FieldName = "ItemId",      TypeName = "System.Int32",  InlineValue = "0" },
                    new FieldData { FieldName = "Count",       TypeName = "System.Int32",  InlineValue = "1" },
                    new FieldData { FieldName = "NPC",         TypeName = "UnityEngine.GameObject" },
                    new FieldData { FieldName = "Optional",    TypeName = "System.Boolean", InlineValue = "False" },
                }
            });
        }

        private static void RegisterAttributeNodes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = assembly.GetTypes(); }
                catch (System.Reflection.ReflectionTypeLoadException e) { types = e.Types; }

                foreach (var type in types)
                {
                    if (type == null) continue;
                    var attr = (QuestNodeAttribute)Attribute.GetCustomAttribute(
                        type, typeof(QuestNodeAttribute));
                    if (attr == null) continue;
                    if (s_Registry.ContainsKey(attr.NodeTypeID)) continue;

                    Register(new DialogueNodeInfo
                    {
                        TypeId      = attr.NodeTypeID,
                        DisplayName = attr.DisplayName,
                        Category    = attr.Category,
                        Description = attr.Description,
                        Icon        = attr.Icon,
                        AccentColor = Color.gray,
                    });
                }
            }
        }

        public static DialogueNodeInfo Get(string typeId)
        {
            EnsureBuilt();
            return s_Registry.TryGetValue(typeId, out var info) ? info : null;
        }
    }
}
