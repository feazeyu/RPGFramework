using System;
using UnityEngine;

namespace Game.Character
{
    public class Entity : MonoBehaviour
    {
        public delegate void OnDeath();
        public event OnDeath onDeath;
        public delegate void OnHealthChanged();
        public event OnHealthChanged onHealthChanged;
        public delegate void OnHit();
        public event OnHit onHit;
        public delegate void OnTakeDamage();
        public event OnTakeDamage onTakeDamage;
        public delegate void OnHeal();
        public event OnHeal onHeal;
    }
}
