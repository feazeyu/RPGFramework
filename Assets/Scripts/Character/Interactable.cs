using UnityEngine;

namespace Game.Character
{
    public class Interactable : MonoBehaviour
    {
        public delegate void OnInteract();
        public event OnInteract onInteract;
        public delegate void OnEnterInteractRange();
        public event OnEnterInteractRange onEnterInteractRange;
        public delegate void OnExitInteractRange();
        public event OnExitInteractRange onExitInteractRange;
    }
}
