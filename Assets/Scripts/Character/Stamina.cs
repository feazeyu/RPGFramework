using System;
using UnityEngine;

namespace Game.Character
{
    [Serializable]
    public class Stamina : Resource
    {
        public readonly ResourceTypes resourceType = ResourceTypes.Stamina;
    }
}
