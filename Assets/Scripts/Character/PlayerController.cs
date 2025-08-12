using UnityEngine;
using UnityEngine.InputSystem;
namespace Game.Character
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : Entity
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;

        [Header("References")]
        public Camera mainCamera;
        public Transform weaponPivot; 

        private Rigidbody2D rb;
        private Vector2 moveInput;
        private Vector2 aimInput;

        private bool isShooting;
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            if (mainCamera == null)
                mainCamera = Camera.main;

            Health health = GetComponent<Health>();
            if (health != null) { 
                health.onHealthReachesZero += Die;
            }
        }

        private void OnDestroy()
        {
            Health health = GetComponent<Health>();
            if (health != null) { 
                health.onHealthReachesZero -= Die;
            }
        }

        private void Update()
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
            Vector2 aimDirection = Vector2.zero;

            // Mouse aiming
            if (Mouse.current != null && Mouse.current.position.IsActuated())
            {
                
                Vector2 screenPosition = mainCamera.WorldToScreenPoint(transform.position);
                aimDirection = (aimInput - screenPosition).normalized;
            }
            // Controller right stick aiming
            else if (aimInput.sqrMagnitude > 0.01f)
            {
                aimDirection = aimInput.normalized;
            }

            if (aimDirection.sqrMagnitude > 0.001f && weaponPivot != null)
            {
                float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                weaponPivot.rotation = Quaternion.Euler(0, 0, angle);
            }
        }

        // --- Attacks ---
        private void HandleAttack()
        {
            if (isShooting)
            {
                Debug.Log("Pew pew!");
            }
        }


        private void Die() { 
            Destroy(gameObject);
        }
    }
}
