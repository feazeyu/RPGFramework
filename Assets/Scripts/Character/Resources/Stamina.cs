using System;
using UnityEngine;

namespace Game.Character
{
    [Serializable]
    public class Stamina : Resource
    {
        public readonly new ResourceTypes resourceType = ResourceTypes.Stamina;
    }
}
