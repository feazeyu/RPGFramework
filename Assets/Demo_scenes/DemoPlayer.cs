using UnityEngine;
using UnityEngine.InputSystem;
using Game.Character;

namespace DialogueGraph.Demo
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Interactor))]
    public class DemoPlayer : MonoBehaviour
    {
        [SerializeField] private float m_MoveSpeed = 5f;

        private Rigidbody2D m_Rb;
        private Interactor m_Interactor;

        private void Awake()
        {
            m_Rb = GetComponent<Rigidbody2D>();
            m_Interactor = GetComponent<Interactor>();
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb[Key.E].wasPressedThisFrame)
                m_Interactor.InteractWithClosest();
        }

        private void FixedUpdate()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            Vector2 input = Vector2.zero;
            if (kb[Key.W].isPressed || kb[Key.UpArrow].isPressed)    input.y += 1f;
            if (kb[Key.S].isPressed || kb[Key.DownArrow].isPressed)  input.y -= 1f;
            if (kb[Key.A].isPressed || kb[Key.LeftArrow].isPressed)  input.x -= 1f;
            if (kb[Key.D].isPressed || kb[Key.RightArrow].isPressed) input.x += 1f;

            m_Rb.linearVelocity = input.normalized * m_MoveSpeed;
        }
    }
}
