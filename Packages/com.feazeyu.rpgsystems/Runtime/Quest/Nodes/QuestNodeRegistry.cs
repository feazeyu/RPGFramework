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
    /// Node registry for the Quest graph system. Derives from
    /// <see cref="NodeRegistry"/>, so the shared flow/logic nodes come from
    /// <see cref="NodeRegistry.RegisterCommonNodes"/>; this class adds the
    /// quest-specific palette (Objective, Reward, terminals, objectives) and the
    /// quest flavour of Condition and Quest Reference (Run Subgraph). Contains
    /// every node type either flavour of quest graph can use; the window filters
    /// by <see cref="QuestGraphAsset.Kind"/> via <see cref="ForKind"/>.
    ///
    /// Type-id constants for shared flow/logic nodes mirror
    /// <see cref="NodeRegistry"/> so a node's stored <c>NodeType</c>
    /// string is universal — a "Start" node means the same thing
    /// regardless of which graph system it lives in.
    /// </summary>
    public sealed class QuestNodeRegistry : NodeRegistry
    {
        private static readonly QuestNodeRegistry s_Instance = new QuestNodeRegistry();

        public static IReadOnlyDictionary<string, NodeInfo> All => s_Instance.AllNodes;

        // ── Quest-specific type IDs ──────────────────────────────────────────
        // Shared flow/logic type-ids (Start, End, Condition, RunSubgraph, …)
        // are inherited from NodeRegistry — QuestNodeRegistry.TypeStart resolves
        // to the base constant, keeping the stored NodeType strings universal.

        public const string TypeObjective      = "Objective";
        public const string TypeReward         = "Reward";
        public const string TypeCompleteQuest  = "CompleteQuest";
        public const string TypeFailQuest      = "FailQuest";
        public const string TypeSpawnItem      = "spawn_item";
        public const string TypeRunDialogue    = "run_dialogue";

        // Concrete objective node types
        public const string TypeObjKill       = "obj_kill";
        public const string TypeObjLocation   = "obj_location";
        public const string TypeObjCollect    = "obj_collect";
        public const string TypeObjAccumulate = "obj_accumulate";
        public const string TypeObjDeliver    = "obj_deliver";

        // Composable modifier / flow node types
        public const string TypeTimer         = "timer";
        public const string TypeResetProgress = "reset_progress";
        public const string TypeGateFlag      = "gate_flag";
        public const string TypeGateLocation  = "gate_location";
        public const string TypeGateItem      = "gate_item";

        // ── Quest accent colours ─────────────────────────────────────────────
        // Shared colours (ColFlow, ColLogic, ColStart, …) are inherited from
        // NodeRegistry.

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
            TypeCondition, TypeSetVariable,
            TypeTriggerEvent, TypeWaitForEvent,
            TypeFindObject, TypeDebugLog, TypeSpawnPrefab,
            TypeObjective, TypeReward,
            TypeCompleteQuest, TypeFailQuest,
            TypeSpawnItem, TypeRunDialogue,
            TypeObjKill, TypeObjLocation, TypeObjCollect, TypeObjAccumulate, TypeObjDeliver,
            TypeTimer, TypeResetProgress,
            TypeGateFlag, TypeGateLocation, TypeGateItem,
        };

        // Only nodes the QuestChainRunner actually acts on belong in a Chain graph:
        // RunSubgraph (the quests), Condition (availability gates), SetVariable
        // (per-quest blackboard side-effects), plus Start/End as structural markers.
        // Flow nodes the chain never executes (TriggerEvent, FindObject, DebugLog,
        // …) are intentionally excluded so the palette only offers nodes that do
        // something.
        private static readonly HashSet<string> s_ChainPalette = new HashSet<string>
        {
            TypeStart, TypeEnd,
            TypeCondition, TypeSetVariable,
            TypeRunSubgraph,
        };

        /// <summary>
        /// Returns the subset of <see cref="All"/> appropriate for a
        /// graph of the given kind. The window's "Add Node" context
        /// menu is populated from this.
        /// </summary>
        public static IReadOnlyDictionary<string, NodeInfo> ForKind(QuestKind kind)
        {
            var allow = kind == QuestKind.Chain ? s_ChainPalette : s_SinglePalette;

            var filtered = new Dictionary<string, NodeInfo>();
            foreach (var kv in s_Instance.AllNodes)
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

        protected override void Build()
        {
            RegisterCommonNodes();
            RegisterQuestFlow();
            RegisterQuestSpecific();
            RegisterAttributeNodes<QuestNodeAttribute>(attr => new NodeInfo
            {
                TypeId      = attr.NodeTypeID,
                DisplayName = attr.DisplayName,
                Category    = attr.Category,
                Description = attr.Description,
                Icon        = attr.Icon,
                AccentColor = Color.gray,
            });
        }

        /// <summary>
        /// Quest-specific variants of nodes whose layout differs from the shared
        /// defaults: a plain-string Condition operator, and Run Subgraph
        /// re-themed as a Quest Reference.
        /// </summary>
        private void RegisterQuestFlow()
        {
            Register(new NodeInfo
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

            Register(new NodeInfo
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

        private void RegisterQuestSpecific()
        {
            Register(new NodeInfo
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

            Register(new NodeInfo
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

            Register(new NodeInfo
            {
                TypeId = TypeCompleteQuest, DisplayName = "Complete Quest", Category = "Quest",
                Description = "Terminal node. Marks the quest as successfully completed and ends the graph.",
                AccentColor = ColComplete, Icon = "✓",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In", Direction = PortDirection.Input, Capacity = PortCapacity.Multi }
                }
            });

            Register(new NodeInfo
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

            Register(new NodeInfo
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

            Register(new NodeInfo
            {
                TypeId = TypeRunDialogue, DisplayName = "Run Dialogue", Category = "Quest",
                Description = "Runs a DialogueGraphAsset inline as a subgraph (e.g. an NPC offering " +
                              "the quest). Link the Graph field to a DialogueGraph blackboard variable. " +
                              "The dialogue can write a Shared blackboard variable the quest then reads " +
                              "with a Condition node (e.g. accept/decline). Follows Out when it ends.",
                AccentColor = ColSubgraph, Icon = "💬",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Graph", TypeName = "Feazeyu.RPGSystems.Dialogue.DialogueGraphAsset" },
                }
            });

            // ── Concrete objective nodes ──────────────────────────────────────

            Register(new NodeInfo
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

            Register(new NodeInfo
            {
                TypeId = TypeObjLocation, DisplayName = "Reach Location", Category = "Objectives",
                Description = "Completes once the player is within Radius of Target. " +
                              "For a 'stay in zone' constraint, attach a Location Gate to the " +
                              "guarded objective's In port instead.",
                AccentColor = ColObjective, Icon = "◉",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",        Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Completed", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Failed",    Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Title",       TypeName = "System.String",  InlineValue = "Reach location" },
                    new FieldData { FieldName = "Description", TypeName = "System.String" },
                    new FieldData { FieldName = "Target",      TypeName = "UnityEngine.Transform" },
                    new FieldData { FieldName = "Radius",      TypeName = "System.Single",  InlineValue = "2" },
                    new FieldData { FieldName = "Optional",    TypeName = "System.Boolean", InlineValue = "False" },
                }
            });

            Register(new NodeInfo
            {
                TypeId = TypeObjCollect, DisplayName = "Collect Item", Category = "Objectives",
                Description = "Level-based: completes once the player carries at least Count of the specified " +
                              "item (counts whatever they already had). For 'acquire N from now on' use " +
                              "Accumulate Item; for a 'while carrying X' constraint attach an Item Gate to " +
                              "the guarded objective's In port.",
                AccentColor = ColObjective, Icon = "⬡",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",        Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Completed", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Failed",    Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Title",       TypeName = "System.String", InlineValue = "Collect item" },
                    new FieldData { FieldName = "Description", TypeName = "System.String" },
                    new FieldData { FieldName = "ItemId",      TypeName = "System.Int32",  InlineValue = "0" },
                    new FieldData { FieldName = "Count",       TypeName = "System.Int32",  InlineValue = "1" },
                    new FieldData { FieldName = "Inventory",   TypeName = "UnityEngine.GameObject" },
                    new FieldData { FieldName = "Optional",    TypeName = "System.Boolean", InlineValue = "False" },
                }
            });

            Register(new NodeInfo
            {
                TypeId = TypeObjAccumulate, DisplayName = "Accumulate Item", Category = "Objectives",
                Description = "Counts items of ItemId acquired after the objective starts (ignores the starting " +
                              "amount), completing at Count. Net: dropping an item decrements progress, so " +
                              "drop+re-collect counts once. Exact when the container raises OnItemAdded/Removed, " +
                              "else polls. Supports Gate (In port), Timer, and Reset Progress modifiers.",
                AccentColor = ColObjective, Icon = "∑",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",        Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Completed", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Failed",    Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Title",       TypeName = "System.String", InlineValue = "Gather items" },
                    new FieldData { FieldName = "Description", TypeName = "System.String" },
                    new FieldData { FieldName = "ItemId",      TypeName = "System.Int32",  InlineValue = "0" },
                    new FieldData { FieldName = "Count",       TypeName = "System.Int32",  InlineValue = "1" },
                    new FieldData { FieldName = "Inventory",   TypeName = "UnityEngine.GameObject" },
                    new FieldData { FieldName = "Optional",    TypeName = "System.Boolean", InlineValue = "False" },
                }
            });

            Register(new NodeInfo
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
                    new FieldData { FieldName = "Inventory",   TypeName = "UnityEngine.GameObject" },
                    new FieldData { FieldName = "Optional",    TypeName = "System.Boolean", InlineValue = "False" },
                }
            });

            RegisterModifierNodes();
        }

        /// <summary>
        /// Composable modifier / flow nodes: the Timer and Reset Progress flow
        /// nodes, and the Gate decorators that attach to an objective's In port.
        /// </summary>
        private void RegisterModifierNodes()
        {
            Register(new NodeInfo
            {
                TypeId = TypeTimer, DisplayName = "Timer", Category = "Flow",
                Description = "Starts when a token enters Begin; fires Timeout after Seconds. " +
                              "AutoRestart re-arms as a perpetual watchdog. Wire Timeout to whatever " +
                              "should happen (e.g. Reset Progress).",
                AccentColor = ColFlow, Icon = "⏱",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "Begin",   Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Timeout", Direction = PortDirection.Output, Capacity = PortCapacity.Multi },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Seconds",     TypeName = "System.Single",  InlineValue = "30" },
                    new FieldData { FieldName = "AutoRestart", TypeName = "System.Boolean", InlineValue = "False" },
                }
            });

            Register(new NodeInfo
            {
                TypeId = TypeResetProgress, DisplayName = "Reset Progress", Category = "Flow",
                Description = "When triggered, wipes the progress of every objective its Target output " +
                              "is wired to (reference-only — never re-runs them), then follows Out.",
                AccentColor = ColFlow, Icon = "↺",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",     Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Target", Direction = PortDirection.Output, Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Out",    Direction = PortDirection.Output, Capacity = PortCapacity.Multi },
                },
            });

            Register(new NodeInfo
            {
                TypeId = TypeGateFlag, DisplayName = "Flag Gate", Category = "Gates",
                Description = "Objective decorator: wire Out into an objective's In. Progress only counts " +
                              "while the blackboard variable satisfies the comparison. Covers 'while wearing X' " +
                              "and collider-driven zone flags (see ZoneFlag).",
                AccentColor = ColLogic, Icon = "⚑",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Multi },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Variable", TypeName = "System.String" },
                    new FieldData { FieldName = "Operator", TypeName = "System.String", InlineValue = "==" },
                    new FieldData { FieldName = "Value",    TypeName = "System.String", InlineValue = "True" },
                }
            });

            Register(new NodeInfo
            {
                TypeId = TypeGateLocation, DisplayName = "Location Gate", Category = "Gates",
                Description = "Objective decorator: progress only counts while the actor is within Radius of " +
                              "Target. Subject='Player' (default) tests the player; 'Subject' tests the event's " +
                              "subject (e.g. where the enemy died).",
                AccentColor = ColLogic, Icon = "◎",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Multi },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Target",  TypeName = "UnityEngine.Transform" },
                    new FieldData { FieldName = "Radius",  TypeName = "System.Single", InlineValue = "2" },
                    new FieldData { FieldName = "Subject", TypeName = "System.String", InlineValue = "Player" },
                }
            });

            Register(new NodeInfo
            {
                TypeId = TypeGateItem, DisplayName = "Item Gate", Category = "Gates",
                Description = "Objective decorator: progress only counts while an inventory holds the required " +
                              "count of ItemId. Resolves the event subject's inventory, falling back to the " +
                              "player (with a warning) when the subject has none.",
                AccentColor = ColLogic, Icon = "⬡",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Multi },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "ItemId",   TypeName = "System.Int32",  InlineValue = "0" },
                    new FieldData { FieldName = "Count",    TypeName = "System.Int32",  InlineValue = "1" },
                    new FieldData { FieldName = "Operator", TypeName = "System.String", InlineValue = ">=" },
                }
            });
        }

        public static NodeInfo Get(string typeId) => s_Instance.GetNode(typeId);
    }
}
