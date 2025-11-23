using System;
using UnityEngine;

namespace Game.Character
{
    [Serializable]
    public class Mana : Resource
    {
        new public readonly ResourceTypes resourceType = ResourceTypes.Mana;
    }
}
