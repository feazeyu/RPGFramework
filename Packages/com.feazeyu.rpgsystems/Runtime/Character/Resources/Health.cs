using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Character
{
    /// <summary>
    /// Health <see cref="Resource"/>. Destroys its GameObject when it reaches zero.
    /// </summary>
    [Serializable]
    public class Health : Resource
    {
        /// <summary>The resource type for this component.</summary>
        new public readonly ResourceTypes resourceType = ResourceTypes.Health;

        /// <summary>Subscribes the destroy-on-zero handler.</summary>
        public virtual void Start()
        {
            onResourceReachesZero += () => Destroy(gameObject);
        }

        /// <summary>Unsubscribes the destroy-on-zero handler.</summary>
        public virtual void OnDestroy()
        {
            onResourceReachesZero -= () => Destroy(gameObject);
        }
    }
}
