using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// Abstract base class for character resources (e.g., health, mana, stamina).
    /// Handles resource value, events for changes, and percent calculation.
    /// </summary>
    public abstract class Resource : MonoBehaviour
    {
        /// <summary>
        /// The type of resource represented by this instance.
        /// </summary>
        public readonly ResourceTypes resourceType = ResourceTypes.None;

        /// <summary>
        /// Delegate for when the resource reaches zero.
        /// </summary>
        public delegate void OnResourceReachesZero();

        /// <summary>
        /// Event triggered when the resource reaches zero.
        /// </summary>
        public event OnResourceReachesZero onResourceReachesZero;

        /// <summary>
        /// Delegate for when resource points are lost.
        /// </summary>
        /// <param name="lostAmount">The amount of resource lost.</param>
        public delegate void OnResourceLost(float lostAmount);

        /// <summary>
        /// Event triggered when resource points are lost.
        /// </summary>
        public event OnResourceLost onResourceLost;

        /// <summary>
        /// Delegate for when resource points are gained.
        /// </summary>
        /// <param name="gainAmount">The amount of resource gained.</param>
        public delegate void OnResourceGained(float gainAmount);

        /// <summary>
        /// Event triggered when resource points are gained.
        /// </summary>
        public event OnResourceGained onResourceGained;

        /// <summary>
        /// Delegate for when resource points are changed.
        /// </summary>
        /// <param name="changeAmount">The amount of resource changed (positive or negative).</param>
        public delegate void OnResourceChanged(float changeAmount);

        /// <summary>
        /// Event triggered when resource points are changed.
        /// </summary>
        public event OnResourceChanged onResourceChanged;

        [SerializeField]
        private float _amount;

        /// <summary>
        /// The maximum amount of this resource.
        /// </summary>
        public float maxAmount;

        /// <summary>
        /// Gets or sets the current amount of resource points.
        /// Triggers events when the value changes, is gained, lost, or reaches zero.
        /// </summary>
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
                else if (diff < 0)
                {
                    onResourceLost?.Invoke(-diff);
                    onResourceChanged?.Invoke(diff);
                }
                _amount = value;
                Mathf.Clamp(_amount, 0, maxAmount);
                if (_amount <= 0)
                {
                    onResourceReachesZero?.Invoke();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current resource as a percentage (0 to 1) of the maximum amount.
        /// </summary>
        public float Percent
        {
            get => _amount / maxAmount;
            set => Points = value * maxAmount;
        }
    }
}
