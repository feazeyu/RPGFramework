using System;
using System.Collections.Generic;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Metadata record for a single node type, registered via [DialogueNode] attribute.
    /// The Editor reads this to populate the right-click "Add Node" menu and to
    /// generate default ports/fields when a node is created.
    /// </summary>
    public class DialogueNodeInfo
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
    /// Attribute placed on concrete node definition classes (runtime or editor-only)
    /// so the NodeRegistry can discover them automatically via reflection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class DialogueNodeAttribute : Attribute
    {
        public new string TypeId      { get; }
        public string DisplayName { get; }
        public string Category    { get; }
        public string Description { get; }
        public string Icon        { get; }

        public DialogueNodeAttribute(
            string typeId,
            string displayName,
            string category    = "General",
            string description = "",
            string icon        = "")
        {
            TypeId      = typeId;
            DisplayName = displayName;
            Category    = category;
            Description = description;
            Icon        = icon;
        }
    }

    /// <summary>
    /// Central registry of all node types available in the graph.
    /// Built-in types are registered in the static constructor;
    /// user types are discovered via reflection using [DialogueNode].
    /// </summary>
    public static class NodeRegistry
    {
        private static Dictionary<string, DialogueNodeInfo> s_Registry;

        public static IReadOnlyDictionary<string, DialogueNodeInfo> All
        {
            get { EnsureBuilt(); return s_Registry; }
        }

        // ── Built-in type IDs ────────────────────────────────────────────────

        public const string TypeStart         = "Start";
        public const string TypeDialogueLine  = "DialogueLine";
        public const string TypeChoiceBranch  = "ChoiceBranch";
        public const string TypeCondition     = "Condition";
        public const string TypeSetVariable   = "SetVariable";
        public const string TypeTriggerEvent  = "TriggerEvent";
        public const string TypeWaitForEvent  = "WaitForEvent";
        public const string TypeSequence      = "Sequence";
        public const string TypeSelector      = "Selector";
        public const string TypeRunSubgraph   = "RunSubgraph";
        public const string TypeEnd           = "End";
        public const string TypeFindObject    = "find_object";
        public const string TypeDebugLog      = "debug_log";

        // ── Accent colours ───────────────────────────────────────────────────

        public static readonly Color ColFlow      = new Color(0.18f, 0.62f, 0.48f);
        public static readonly Color ColDialogue  = new Color(0.29f, 0.61f, 0.78f);
        public static readonly Color ColLogic     = new Color(0.94f, 0.65f, 0.20f);
        public static readonly Color ColEvent     = new Color(0.88f, 0.31f, 0.44f);
        public static readonly Color ColVariable  = new Color(0.69f, 0.42f, 0.97f);
        public static readonly Color ColStart     = new Color(0.34f, 0.78f, 0.34f);
        public static readonly Color ColEnd       = new Color(0.75f, 0.25f, 0.25f);

        // ── Build ────────────────────────────────────────────────────────────

        private static void EnsureBuilt()
        {
            if (s_Registry != null) return;
            s_Registry = new Dictionary<string, DialogueNodeInfo>();
            RegisterBuiltins();
            RegisterAttributeNodes();
        }

        private static void Register(DialogueNodeInfo info)
            => s_Registry[info.TypeId] = info;

        private static void RegisterBuiltins()
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
                Description = "Runs children left-to-right. Fails on first child failure.",
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
                TypeId = TypeDialogueLine, DisplayName = "Dialogue Line", Category = "Dialogue",
                Description = "Displays a single line of dialogue from a speaker.",
                AccentColor = ColDialogue, Icon = "💬",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single }
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Speaker",  TypeName = "System.String" },
                    new FieldData { FieldName = "Text",     TypeName = "System.String" },
                    new FieldData { FieldName = "Portrait", TypeName = "UnityEngine.Sprite" },
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = TypeChoiceBranch, DisplayName = "Choice Branch", Category = "Dialogue",
                Description = "Presents player choices. Each output port maps to one choice.",
                AccentColor = ColDialogue, Icon = "⊕",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",       Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Choice 1", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Choice 2", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Choice 1 Text", TypeName = "System.String" },
                    new FieldData { FieldName = "Choice 2 Text", TypeName = "System.String" },
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
                TypeId = TypeRunSubgraph, DisplayName = "Run Subgraph", Category = "Flow",
                Description = "Executes another DialogueGraphAsset inline.",
                AccentColor = ColFlow, Icon = "⊞",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single }
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Graph", TypeName = "Feazeyu.RPGSystems.Dialogue.DialogueGraphAsset" },
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
                TypeId = "give_item", DisplayName = "Give Item", Category = "Inventory",
                Description = "Tries to add an item to an inventory. Routes to Success or Failure.",
                AccentColor = new Color(0.20f, 0.72f, 0.42f), Icon = "🎁",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",      Direction = PortDirection.Input,  Capacity = PortCapacity.Multi },
                    new PortData { PortName = "Success", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Failure", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "ItemId",  TypeName = "System.Int32",          InlineValue = "0" },
                    new FieldData { FieldName = "Count",   TypeName = "System.Int32",          InlineValue = "1" },
                    new FieldData { FieldName = "Target",  TypeName = "UnityEngine.GameObject" },
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = "check_currency", DisplayName = "Check Currency", Category = "Shop",
                Description = "Routes Enough or NotEnough based on the player's wallet balance.",
                AccentColor = new Color(1.0f, 0.84f, 0.0f), Icon = "💰",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",        Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Enough",    Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "NotEnough", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Amount", TypeName = "System.Int32", InlineValue = "0" },
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = "add_currency", DisplayName = "Add Currency", Category = "Shop",
                Description = "Adds money to the player's wallet and continues.",
                AccentColor = new Color(0.20f, 0.72f, 0.42f), Icon = "+$",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Amount", TypeName = "System.Int32", InlineValue = "0" },
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = "remove_currency", DisplayName = "Remove Currency", Category = "Shop",
                Description = "Deducts money from the player's wallet. Routes Success or Failure.",
                AccentColor = new Color(0.88f, 0.31f, 0.44f), Icon = "-$",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",      Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Success", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                    new PortData { PortName = "Failure", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Amount", TypeName = "System.Int32", InlineValue = "0" },
                }
            });

            Register(new DialogueNodeInfo
            {
                TypeId = "open_shop", DisplayName = "Open Shop", Category = "Shop",
                Description = "Opens the shop UI on the target Shopkeep/ShopGridUI GameObject and continues.",
                AccentColor = new Color(0.29f, 0.61f, 0.78f), Icon = "🛒",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "In",  Direction = PortDirection.Input,  Capacity = PortCapacity.Multi  },
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Target", TypeName = "UnityEngine.GameObject" },
                }
            });
        }

        /// <summary>
        /// Scans all loaded assemblies for classes tagged with [DialogueNode]
        /// and registers them automatically.
        /// </summary>
        private static void RegisterAttributeNodes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    var attr = (DialogueNodeAttribute)Attribute.GetCustomAttribute(
                        type, typeof(DialogueNodeAttribute));
                    if (attr == null) continue;

                    if (s_Registry.ContainsKey(attr.TypeId)) continue;

                    Register(new DialogueNodeInfo
                    {
                        TypeId      = attr.TypeId,
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
