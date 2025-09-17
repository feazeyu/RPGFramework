using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// Abstract base class for melee weapons, handling hitbox activation and collision logic.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class Melee : WeaponCollisionHandler
    {
        /// <summary>
        /// The Collider2D component used as the hitbox for the melee weapon.
        /// </summary>
        private Collider2D hitbox;

        /// <summary>
        /// Initializes the hitbox and ensures it is set as a trigger and disabled by default.
        /// </summary>
        private void Awake()
        {
            hitbox = GetComponent<Collider2D>();
            if (hitbox == null)
            {
                Debug.LogError("Melee weapon requires a Collider2D component.");
            }
            hitbox.isTrigger = true;
            DisableHitbox();
        }

        /// <summary>
        /// Starts the melee attack by enabling the hitbox and clearing the already hit targets.
        /// </summary>
        public virtual void StartAttack()
        {
            EnableHitbox();
            ClearAlreadyHit();
        }

        /// <summary>
        /// Stops the melee attack by disabling the hitbox and clearing the already hit targets.
        /// </summary>
        public virtual void StopAttack()
        {
            DisableHitbox();
            ClearAlreadyHit();
        }

        /// <summary>
        /// Enables the hitbox collider.
        /// </summary>
        public void EnableHitbox()
        {
            hitbox.enabled = true;
        }

        /// <summary>
        /// Disables the hitbox collider.
        /// </summary>
        public void DisableHitbox()
        {
            hitbox.enabled = false;
        }

        /// <summary>
        /// Handles collision events when another collider enters the hitbox trigger.
        /// </summary>
        /// <param name="collision">The collider that entered the trigger.</param>
        public void OnTriggerEnter2D(Collider2D collision)
        {
            HandleCollision(collision.gameObject);
        }
    }
    
}
