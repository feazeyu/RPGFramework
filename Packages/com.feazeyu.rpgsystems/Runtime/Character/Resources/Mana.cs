using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Character
{
    /// <summary>Mana <see cref="Resource"/>.</summary>
    [Serializable]
    public class Mana : Resource
    {
        /// <summary>The resource type for this component.</summary>
        new public readonly ResourceTypes resourceType = ResourceTypes.Mana;
    }
}
