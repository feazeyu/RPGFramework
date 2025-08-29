using Game.Core.Interfaces;
using Game.Core.Stats;
using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable
namespace Game.Items
{
    [CreateAssetMenu(fileName = "NewItemInfo", menuName = "RPGFramework/Items/ItemInfo")]
    public class ItemInfo : ScriptableObject, INamed
    {
        public int id;
        public string Name => _name;
        [SerializeField]
        private string _name = string.Empty;

        public Sprite? Icon => _icon;
        [SerializeField]
        private Sprite? _icon = null;

        public ItemTarget Target => _target;
        [SerializeField]
        private ItemTarget _target;

        public int Tier => _tier;
        [SerializeField, Range(1, 5)]
        private int _tier;

        public string Description => _description;
        [SerializeField, TextArea]
        private string _description = string.Empty;

        public ItemShape Shape => _shape;
        [SerializeField]
        private ItemShape _shape = new() { Positions = Array.Empty<Vector2Int>() };

        public IEnumerable<StatEffect> StatEffects => _statEffects;
        [SerializeField]
        private EffectList<StatEffectF> _statEffects = new();
    }
}
