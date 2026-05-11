    using Feazeyu.RPGSystems.Core;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Feazeyu.RPGSystems.Character
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : Entity
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;

        [Header("References")]
        [Tooltip("(Optional) Camera used to calculate mouse position on screen")]
        public Camera mainCamera;
        [Tooltip("(Optional) Component of a gameobject that will get rotated when aiming")]
        public RotateTowardsPoint weaponRotationHandler;
        [Tooltip("(Optional) Weapon component used for attacks")]
        public Weapon weapon;
        [Tooltip("Flip the sprite when aiming left")]
        public bool flipOnAimLeft = true;
        [Tooltip("(Optional) Interactor component reference")]
        public Interactor interactor;

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 aimInput;
        private bool isShooting;
        private SpriteRenderer sr;
        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
            if (mainCamera == null)
                mainCamera = Camera.main;
            if (weaponRotationHandler == null)
                weaponRotationHandler = GetComponentInChildren<RotateTowardsPoint>();
            if (weapon == null)
                weapon = GetComponentInChildren<Weapon>();
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

        public void Update()
        {
            HandleAiming();
            HandleAttack();
        }

        private void FixedUpdate()
        {
            Move();
        }

        // --- Input Callbacks ---
        public void OnMove(InputAction.CallbackContext ctx)
        {
            moveInput = ctx.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext ctx)
        {
            aimInput = ctx.ReadValue<Vector2>();
        }

        public void OnInteract(InputAction.CallbackContext ctx)
        {
            if (interactor == null) { 
                Debug.LogWarning("Interactor reference not set on PlayerController. This is not a problem if you don't need interaction functionality.");
            }
            if (ctx.started)
            {
                interactor.InteractWithClosest();
            }
        }

        public void OnAttack(InputAction.CallbackContext ctx)
        {
            if (ctx.started) isShooting = true;
            if (ctx.canceled) isShooting = false;
        }

        // --- Movement ---
        private void Move()
        {
            Vector2 targetVelocity = moveInput.normalized * moveSpeed;
            rb.linearVelocity = targetVelocity;
        }

        // --- Aiming ---
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

        // --- Attacks ---
        private void HandleAttack()
        {
            if (isShooting)
            {
                weapon.Attack();
            }
        }


        private void Die()
        {
            Destroy(gameObject);
        }
    }
}
