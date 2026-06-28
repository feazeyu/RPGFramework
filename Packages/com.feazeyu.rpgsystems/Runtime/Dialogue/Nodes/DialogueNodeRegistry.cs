using System;
using System.Collections.Generic;
using UnityEngine;

namespace Feazeyu.RPGSystems.Dialogue
{
    /// <summary>
    /// Attribute placed on concrete dialogue node definition classes (runtime or
    /// editor-only) so <see cref="DialogueNodeRegistry"/> can discover them
    /// automatically via reflection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class DialogueNodeAttribute : Attribute
    {
        /// <summary>Type id.</summary>
        public new string TypeId      { get; }
        /// <summary>Display name.</summary>
        public string DisplayName { get; }
        /// <summary>Category.</summary>
        public string Category    { get; }
        /// <summary>Description.</summary>
        public string Description { get; }
        /// <summary>Icon.</summary>
        public string Icon        { get; }

        /// <summary>Initializes a new instance of the <see cref="DialogueNodeAttribute"/> class.</summary>
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
    /// Node registry for the dialogue graph system. Built-in shared flow/logic
    /// nodes come from <see cref="NodeRegistry.RegisterCommonNodes"/>; this class
    /// adds the dialogue-specific palette (Dialogue Line, Choice Branch,
    /// Requirement, inventory/shop nodes) and the dialogue flavour of Condition
    /// and Run Subgraph. User types are discovered via <see cref="DialogueNodeAttribute"/>.
    /// </summary>
    public sealed class DialogueNodeRegistry : NodeRegistry
    {
        private static readonly DialogueNodeRegistry s_Instance = new DialogueNodeRegistry();

        /// <summary>All dialogue node types, built on first access.</summary>
        public static IReadOnlyDictionary<string, NodeInfo> All => s_Instance.AllNodes;

        /// <summary>Look up a dialogue node type, or <c>null</c> if not registered.</summary>
        public static NodeInfo Get(string typeId) => s_Instance.GetNode(typeId);


        /// <summary>Type dialogue line.</summary>
        public const string TypeDialogueLine = "DialogueLine";
        /// <summary>Type choice branch.</summary>
        public const string TypeChoiceBranch = "ChoiceBranch";
        /// <summary>Type requirement.</summary>
        public const string TypeRequirement  = "requirement";


        /// <summary>Col dialogue.</summary>
        public static readonly Color ColDialogue = new Color(0.29f, 0.61f, 0.78f);


        /// <inheritdoc/>
        protected override void Build()
        {
            RegisterCommonNodes();
            RegisterDialogueNodes();
            RegisterAttributeNodes<DialogueNodeAttribute>(attr => new NodeInfo
            {
                TypeId      = attr.TypeId,
                DisplayName = attr.DisplayName,
                Category    = attr.Category,
                Description = attr.Description,
                Icon        = attr.Icon,
                AccentColor = Color.gray,
            });
        }

        private void RegisterDialogueNodes()
        {
            Register(new NodeInfo
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

            Register(new NodeInfo
            {
                TypeId = TypeChoiceBranch, DisplayName = "Choice Branch", Category = "Dialogue",
                Description = "Presents player choices. Each output port maps to one choice. Connect a Requirement node to the input of the node following a choice to conditionally hide that choice.",
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
                    new FieldData { FieldName = "Operator", TypeName = "conditional_operator", InlineValue = "==" },
                    new FieldData { FieldName = "Value",    TypeName = "System.String" },
                }
            });

            Register(new NodeInfo
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

            Register(new NodeInfo
            {
                TypeId = TypeRequirement, DisplayName = "Requirement", Category = "Logic",
                Description = "Hides a Choice Branch option when the condition is not met. Connect 'Out' to the input of the node that follows the choice.",
                AccentColor = ColLogic, Icon = "✓",
                DefaultPorts = new List<PortData>
                {
                    new PortData { PortName = "Out", Direction = PortDirection.Output, Capacity = PortCapacity.Single },
                },
                DefaultFields = new List<FieldData>
                {
                    new FieldData { FieldName = "Variable", TypeName = "System.String" },
                    new FieldData { FieldName = "Operator", TypeName = "conditional_operator", InlineValue = "==" },
                    new FieldData { FieldName = "Value",    TypeName = "System.String" },
                }
            });

            Register(new NodeInfo
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

            Register(new NodeInfo
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

            Register(new NodeInfo
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

            Register(new NodeInfo
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

            Register(new NodeInfo
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

            Register(new NodeInfo
            {
                TypeId = "close_shop", DisplayName = "Close Shop", Category = "Shop",
                Description = "Closes the shop UI on the target Shopkeep/ShopGridUI GameObject and continues.",
                AccentColor = new Color(0.75f, 0.35f, 0.35f), Icon = "🚪",
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
    }
}
