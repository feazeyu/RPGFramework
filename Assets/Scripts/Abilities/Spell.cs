using Game.Character;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Abilities
{
    [Serializable]
    internal class Spell : ScriptableObject
    {
        [SerializeReference]
        public List<SpellComponent> Components;
        [SerializeField, HideInInspector]
        private int _manaCost;
        public int ManaCost {
            get {
                if (_manaCost == 0) {
                    _manaCost = Components.Sum(c => (int)c.resourceCosts[ResourceTypes.Mana]);
                }
                return _manaCost;
            }
        }
        [SerializeField, HideInInspector]
        private int _healthCost;
        public int HealthCost {
            get {
                if (_healthCost == 0) {
                    _healthCost = Components.Sum(c => (int)c.resourceCosts[ResourceTypes.Health]);
                }
                return _healthCost;
            }
        }
        [SerializeField, HideInInspector]
        private int _staminaCost;
        public int StaminaCost {
            get {
                if (_staminaCost == 0) {
                    _staminaCost = Components.Sum(c => (int)c.resourceCosts[ResourceTypes.Stamina]);
                }
                return _staminaCost;
            }
        }
        private float _cooldown;
        public float Cooldown { 
            get {
                if (_cooldown == 0) {
                    _cooldown = Components.Sum(c => c.cooldown);
                }
                return _cooldown;
            }
        }
        private float _range;
        public float Range { 
            get {
                if (_range == 0) {
                    _range = Components.Max(c => c.range);
                }
                return _range;
            }
        }
        private float _speed;
        public float Speed { 
            get {
                if (_speed == 0) {
                    _speed = Components.Max(c => c.speed);
                }
                return _speed;
            }
        }
        private float _damage;
        public float Damage { 
            get {
                if (_damage == 0) {
                    _damage = Components.Sum(c => c.damage);
                }
                return _damage;
            }
        }
    }
}
