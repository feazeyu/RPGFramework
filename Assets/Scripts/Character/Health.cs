using System;
using UnityEngine;

namespace Game.Character
{
    [Serializable]
    public class Health : Resource
    {
        public readonly ResourceTypes resourceType = ResourceTypes.Health;
    }
}
