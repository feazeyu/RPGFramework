using Codice.CM.Client.Differences;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Character
{
    public class Interactor : MonoBehaviour
    {
        [HideInInspector]
        public List<Interactable> interactables = new List<Interactable>();


        /// <summary>
        /// Calls Interact() on the closest Interactable in range.
        /// returns true if an interaction occurred, false otherwise.
        /// </summary>
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
