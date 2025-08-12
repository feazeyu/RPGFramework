using UnityEngine;

namespace Game.Character
{
    public class Health : MonoBehaviour
    {
        //Events
        public delegate void OnHealthReachesZero();
        public event OnHealthReachesZero onHealthReachesZero;
        public delegate void OnTakeDamage(float damage);
        public event OnTakeDamage onTakeDamage;
        public delegate void OnHeal(float healAmount);
        public event OnHeal onHeal;
        public delegate void OnHealthChanged(float newHealth);
        public event OnHealthChanged onHealthChanged;
        
        [SerializeField]
        private float _health;
        [SerializeField, HideInInspector]
        private Entity _entity;
        public float maxHealth;
        public float HealthPoints
        {
            get => _health;
            set
            {
                float diff = value - _health;
                if (diff > 0)
                {
                    onHeal?.Invoke(diff);
                    onHealthChanged?.Invoke(diff);
                }
                else if (diff < 0) { 
                    onTakeDamage?.Invoke(-diff);
                    onHealthChanged?.Invoke(diff);
                }
                    _health = value;
                if (_health <= 0)
                {
                    onHealthReachesZero?.Invoke();  
                }
            }
        }
        public float HealthPercent
        {
            get => _health / maxHealth;
            set => HealthPoints = value * maxHealth;
        }
        private Entity GetEntity() { 
            return GetComponent<Entity>();
        }

    }
}
