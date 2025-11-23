using System;
using UnityEngine;

namespace Game.Character
{
    [Serializable]
    public class Health : Resource
    {
        new public readonly ResourceTypes resourceType = ResourceTypes.Health;
        public virtual void Start()
        {
            onResourceReachesZero += () => Destroy(gameObject);
        }
        public virtual void OnDestroy()
        {
            onResourceReachesZero -= () => Destroy(gameObject);
        }
    }
    
}
