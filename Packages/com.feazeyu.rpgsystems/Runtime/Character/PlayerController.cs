using Feazeyu.RPGSystems.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Feazeyu.RPGSystems.Character
{
    /// <summary>
    /// Player avatar: top-down movement, aim-based weapon rotation, attack state,
    /// and interaction, driven by the Input System. Extends <see cref="Entity"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : Entity
    {
        /// <summary>Move speed.</summary>
        [Header("Movement Settings")]
        public float moveSpeed = 5f;

        /// <summary>Main camera.</summary>
        [Header("References")]
        [Tooltip("(Optional) Camera used to calculate mouse position on screen")]
        public Camera mainCamera;
        /// <summary>Weapon rotation handler.</summary>
        [Tooltip("(Optional) Component of a gameobject that will get rotated when aiming")]
        public RotateTowardsPoint weaponRotationHandler;
        /// <summary>Flip on aim left.</summary>
        [Tooltip("Flip the sprite when aiming left")]
        public bool flipOnAimLeft = true;
        /// <summary>Interactor.</summary>
        [Tooltip("(Optional) Interactor component reference")]
        public Interactor interactor;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 aimInput;

        /// <summary>Whether the attack input is currently held.</summary>
        public bool IsShooting { get; private set; }

        private SpriteRenderer sr;

        /// <summary>Caches components and subscribes to the player's death.</summary>
        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (weaponRotationHandler == null)
                weaponRotationHandler = GetComponentInChildren<RotateTowardsPoint>();
            sr = GetComponent<SpriteRenderer>();
            Health health = resources[ResourceTypes.Health] as Health;
            if (health != null)
            {
                health.onResourceReachesZero += Die;
            }
        }

        private void OnDestroy()
        {
            Health health = resources[ResourceTypes.Health] as Health;
            if (health != null)
            {
                health.onResourceReachesZero -= Die;
            }
        }

        private void Update()
        {
            HandleAiming();
        }

        private void FixedUpdate()
        {
            Move();
        }

        /// <summary>Input System callback: reads the movement vector.</summary>
        public void OnMove(InputAction.CallbackContext ctx)
        {
            moveInput = ctx.ReadValue<Vector2>();
        }

        /// <summary>Input System callback: reads the aim vector.</summary>
        public void OnLook(InputAction.CallbackContext ctx)
        {
            aimInput = ctx.ReadValue<Vector2>();
        }

        /// <summary>Input System callback: interacts with the closest interactable.</summary>
        public void OnInteract(InputAction.CallbackContext ctx)
        {
            if (interactor == null)
            {
                Debug.LogWarning("Interactor reference not set on PlayerController. This is not a problem if you don't need interaction functionality.");
            }
            if (ctx.started)
            {
                interactor.InteractWithClosest();
            }
        }

        /// <summary>Input System callback: tracks the attack (shooting) held state.</summary>
        public void OnAttack(InputAction.CallbackContext ctx)
        {
            if (ctx.started) IsShooting = true;
            if (ctx.canceled) IsShooting = false;
        }

        private void Move()
        {
            Vector2 targetVelocity = moveInput.normalized * moveSpeed;
            rb.linearVelocity = targetVelocity;
        }

        private void HandleAiming()
        {
            if (weaponRotationHandler == null)
                return;
            weaponRotationHandler.RotateTowards(aimInput);
            if (sr != null && flipOnAimLeft)
            {
                sr.flipX = Mathf.Abs(weaponRotationHandler.angle) > 90;
            }
        }

        private void Die()
        {
            Destroy(gameObject);
        }
    }
}
