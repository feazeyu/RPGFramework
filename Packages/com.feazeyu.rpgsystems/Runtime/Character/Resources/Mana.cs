using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Character
{
    [Serializable]
    public class Mana : Resource
    {
        new public readonly ResourceTypes resourceType = ResourceTypes.Mana;
    }
}
