using Game.Core.Interfaces;
using Game.Core.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable
namespace Game.Items
{
    /// <summary>
    /// Represents the information and configuration for an item in the RPG framework.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemInfo", menuName = "RPGFramework/Items/ItemInfo")]
    public class ItemInfo : ScriptableObject, INamed
    {
        /// <summary>
        /// The unique identifier for the item.
        /// </summary>
        public int id;

        /// <summary>
        /// Gets the display name of the item.
        /// </summary>
        public string Name => _name;

        [SerializeField]
        private string _name = string.Empty;

        /// <summary>
        /// Gets the icon representing the item.
        /// </summary>
        public Sprite? Icon => _icon;

        [SerializeField]
        private Sprite? _icon = null;

        /// <summary>
        /// Gets the target type(s) that this item can be applied to.
        /// </summary>
        public ItemTarget Target => _target;

        [SerializeField]
        private ItemTarget _target;

        /// <summary>
        /// Gets the tier or rarity level of the item.
        /// </summary>
        public int Tier => _tier;

        [SerializeField, Range(1, 5)]
        private int _tier;

        /// <summary>
        /// Gets the description of the item.
        /// </summary>
        public string Description => _description;

        [SerializeField, TextArea]
        private string _description = string.Empty;

        /// <summary>
        /// Gets the shape of the item, represented as a set of grid positions.
        /// </summary>
        public ItemShape Shape => _shape;

        [SerializeField]
        private ItemShape _shape = new() { Positions = Array.Empty<Vector2Int>() };

        /// <summary>
        /// Gets the collection of stat effects applied by this item.
        /// </summary>
        public IEnumerable<StatEffect> StatEffects => _statEffects;

        [SerializeField]
        private EffectList<StatEffectF> _statEffects = new();
    }
}
