using UnityEngine;
using UnityEngine.Events;

namespace Game.Character
{
    [RequireComponent(typeof(Collider2D))]
    public class Interactable : MonoBehaviour
    {
        public UnityEvent OnInteract;
        /// <summary>
        /// Implement your interaction logic here.
        /// </summary>
        public virtual void Interact()
        {
            OnInteract.Invoke();
            Debug.Log("Interacted with " + gameObject.name);
        }

        public virtual void OnTriggerEnter2D(Collider2D collision)
        {
            Interactor interactor = collision.GetComponent<Interactor>();
            if (interactor != null)
            {
                interactor.interactables.Add(this);
            }
        }

        public virtual void OnTriggerExit2D(Collider2D collision)
        {
            Interactor interactor = collision.GetComponent<Interactor>();
            if (interactor != null)
            {
                interactor.interactables.Remove(this);
            }
        }

    }
}
