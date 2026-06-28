using System.Collections.Generic;
using UnityEngine;

namespace Feazeyu.RPGSystems.Character
{
    /// <summary>
    /// Player-side component that tracks nearby <see cref="Interactable"/>s and
    /// triggers the closest one on demand.
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        /// <summary>Interactables currently within range, maintained by <see cref="Interactable"/> triggers.</summary>
        [HideInInspector]
        public List<Interactable> interactables = new List<Interactable>();

        /// <summary>
        /// Calls <see cref="Interactable.Interact"/> on the closest interactable in range.
        /// </summary>
        /// <returns><c>true</c> if an interaction occurred; otherwise <c>false</c>.</returns>
        public virtual bool InteractWithClosest()
        {
            Interactable closest = null;
            float closestDistance = float.MaxValue;
            foreach (var interactable in interactables)
            {
                float distance = Vector3.Distance(transform.position, interactable.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = interactable;
                }
            }
            if (closest != null)
            {
                closest.Interact();
                return true;
            }
            return false;
        }
    }
}
