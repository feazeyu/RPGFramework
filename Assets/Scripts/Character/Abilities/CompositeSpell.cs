using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// Represents a composite spell made up of multiple spell components.
    /// Aggregates costs, cooldown, range, speed, and damage from its components.
    /// </summary>
    [Serializable, CreateAssetMenu(fileName = "NewCompositeSpell", menuName = "RPGFramework/Abilities/CompositeSpell")]
    internal class CompositeSpell : ScriptableObject
    {
        /// <summary>
        /// The list of spell components that make up this composite spell.
        /// </summary>
        [SerializeField]
        private List<SpellInfo> Components = new List<SpellInfo>();

        /// <summary>
        /// The cached total mana cost of the composite spell.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _manaCost;

        /// <summary>
        /// Gets the total mana cost by summing the mana costs of all components.
        /// </summary>
        public int ManaCost
        {
            get
            {
                if (_manaCost == 0)
                {
                    _manaCost = Components.Sum(c => (int)c.resourceCosts[ResourceTypes.Mana]);
                }
                return _manaCost;
            }
        }

        /// <summary>
        /// The cached total health cost of the composite spell.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _healthCost;

        /// <summary>
        /// Gets the total health cost by summing the health costs of all components.
        /// </summary>
        public int HealthCost
        {
            get
            {
                if (_healthCost == 0)
                {
                    _healthCost = Components.Sum(c => (int)c.resourceCosts[ResourceTypes.Health]);
                }
                return _healthCost;
            }
        }

        /// <summary>
        /// The cached total stamina cost of the composite spell.
        /// </summary>
        [SerializeField, HideInInspector]
        private int _staminaCost;

        /// <summary>
        /// Gets the total stamina cost by summing the stamina costs of all components.
        /// </summary>
        public int StaminaCost
        {
            get
            {
                if (_staminaCost == 0)
                {
                    _staminaCost = Components.Sum(c => (int)c.resourceCosts[ResourceTypes.Stamina]);
                }
                return _staminaCost;
            }
        }

        /// <summary>
        /// The cached total cooldown of the composite spell.
        /// </summary>
        private float _cooldown;

        /// <summary>
        /// Gets the total cooldown by summing the cooldowns of all components.
        /// </summary>
        public float Cooldown
        {
            get
            {
                if (_cooldown == 0)
                {
                    _cooldown = Components.Sum(c => c.cooldown);
                }
                return _cooldown;
            }
        }

        /// <summary>
        /// The cached maximum range among all components.
        /// </summary>
        private float _range;

        /// <summary>
        /// Gets the maximum range among all components.
        /// </summary>
        public float Range
        {
            get
            {
                if (_range == 0)
                {
                    _range = Components.Max(c => c.range);
                }
                return _range;
            }
        }

        /// <summary>
        /// The cached maximum speed among all components.
        /// </summary>
        private float _speed;

        /// <summary>
        /// Gets the maximum speed among all components.
        /// </summary>
        public float Speed
        {
            get
            {
                if (_speed == 0)
                {
                    _speed = Components.Max(c => c.speed);
                }
                return _speed;
            }
        }

        /// <summary>
        /// The cached total damage of the composite spell.
        /// </summary>
        private float _damage;

        /// <summary>
        /// Gets the total damage by summing the damage of all components.
        /// </summary>
        public float Damage
        {
            get
            {
                if (_damage == 0)
                {
                    _damage = Components.Sum(c => c.damage);
                }
                return _damage;
            }
        }
    }
}
