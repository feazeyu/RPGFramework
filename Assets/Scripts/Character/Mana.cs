using System;
using UnityEngine;

namespace Game.Character
{
    [Serializable]
    internal class Mana
    {
        public delegate void OnManaReachesZero();
        public event OnManaReachesZero onManaReachesZero;
        public delegate void OnManaReachesMax();
        public event OnManaReachesMax onManaReachesMax;
        public delegate void OnManaChanged(float newMana);
        public event OnManaChanged onManaChanged;
        public delegate void OnManaLost(float cost);
        public event OnManaLost onManaCost;
        public delegate void OnManaGained(float gain);
        public event OnManaGained onManaGained;
        [SerializeField, HideInInspector]
        private float _mana;
        public float ManaPoints
        {
            get => _mana;
            set
            {
                float diff = value - _mana;
                if (diff > 0)
                {
                    onManaGained?.Invoke(diff);
                    onManaChanged?.Invoke(diff);
                }
                else if (diff < 0)
                {
                    onManaCost?.Invoke(-diff);
                    onManaChanged?.Invoke(diff);
                }
                _mana = value;
                if (_mana <= 0)
                {
                    onManaReachesZero?.Invoke();
                }
                else if (_mana >= maxMana)
                {
                    onManaReachesMax?.Invoke();
                }
            }
        }
        /// <summary>
        /// Percent as a value between 0 and 1
        public float ManaPercent
        {
            get => _mana / maxMana;
            set => ManaPoints = value * maxMana;
        }

        public float maxMana;

    }
}
