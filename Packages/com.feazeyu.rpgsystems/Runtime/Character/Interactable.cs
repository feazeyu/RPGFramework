using UnityEngine;
using UnityEngine.Events;

namespace Feazeyu.RPGSystems.Character
{
    /// <summary>
    /// A world object the player can interact with. Registers itself with an
    /// <see cref="Interactor"/> while that interactor is within its trigger collider.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Interactable : MonoBehaviour
    {
        /// <summary>Raised when the object is interacted with.</summary>
        public UnityEvent OnInteract;
        /// <summary>Raised when an interactor enters this object's trigger area.</summary>
        public UnityEvent OnAreaEnter;
        /// <summary>Raised when an interactor leaves this object's trigger area.</summary>
        public UnityEvent OnAreaExit;

        /// <summary>
        /// Performs the interaction. Override to add custom behavior; the base
        /// implementation raises <see cref="OnInteract"/>.
        /// </summary>
        public virtual void Interact()
        {
            OnInteract.Invoke();
            Debug.Log("Interacted with " + gameObject.name);
        }

        /// <summary>Registers an entering <see cref="Interactor"/> and raises <see cref="OnAreaEnter"/>.</summary>
        public virtual void OnTriggerEnter2D(Collider2D collision)
        {
            Interactor interactor = collision.GetComponent<Interactor>();
            if (interactor != null)
            {
                interactor.interactables.Add(this);
                OnAreaEnter.Invoke();
            }
        }

        /// <summary>Deregisters a leaving <see cref="Interactor"/> and raises <see cref="OnAreaExit"/>.</summary>
        public virtual void OnTriggerExit2D(Collider2D collision)
        {
            Interactor interactor = collision.GetComponent<Interactor>();
            if (interactor != null)
            {
                interactor.interactables.Remove(this);
                OnAreaExit.Invoke();
            }
        }
    }
}
