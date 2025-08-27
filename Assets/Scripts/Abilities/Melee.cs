using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Abilities
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class Melee : WeaponCollisionHandler
    {
        private Collider2D hitbox;
        private void Awake() {
            hitbox = GetComponent<Collider2D>();
            if (hitbox == null) {
                Debug.LogError("Melee weapon requires a Collider2D component.");
            }
            hitbox.isTrigger = true;
            DisableHitbox();
        }

        public virtual void StartAttack() {
            EnableHitbox();
            ClearAlreadyHit();
        }
        public virtual void StopAttack() {
            DisableHitbox();
            ClearAlreadyHit();
        }
        public void EnableHitbox() {
            hitbox.enabled = true;
        }
        public void DisableHitbox() {
            hitbox.enabled = false;
        }
        public void OnTriggerEnter2D(Collider2D collision)
        {
            HandleCollision(collision.gameObject);
        }
    }
    
}
