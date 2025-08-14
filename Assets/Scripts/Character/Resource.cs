using UnityEngine;

namespace Game.Character
{
    public abstract class Resource : MonoBehaviour
    {
        //Events
        public delegate void OnResourceReachesZero();
        public event OnResourceReachesZero onResourceReachesZero;
        public delegate void OnResourceLost(float lostAmount);
        public event OnResourceLost onResourceLost;
        public delegate void OnResourceGained(float gainAmount);
        public event OnResourceGained onResourceGained;
        public delegate void OnResourceChanged(float newAmount);
        public event OnResourceChanged onResourceChanged;
        
        [SerializeField]
        private float _amount;
        public float maxAmount;
        public float Points
        {
            get => _amount;
            set
            {
                float diff = value - _amount;
                if (diff > 0)
                {
                    onResourceGained?.Invoke(diff);
                    onResourceChanged?.Invoke(diff);
                }
                else if (diff < 0) { 
                    onResourceLost?.Invoke(-diff);
                    onResourceChanged?.Invoke(diff);
                }
                    _amount = value;
                if (_amount <= 0)
                {
                    onResourceReachesZero?.Invoke();  
                }
            }
        }
        /// <summary>
        /// Percent as a value between 0 and 1

        public float Percent
        {
            get => _amount / maxAmount;
            set => Points = value * maxAmount;
        }

    }
}
