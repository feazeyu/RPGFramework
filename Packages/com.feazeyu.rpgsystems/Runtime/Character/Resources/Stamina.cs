using System;
using UnityEngine;

namespace Feazeyu.RPGSystems.Character
{
    /// <summary>Stamina <see cref="Resource"/>.</summary>
    [Serializable]
    public class Stamina : Resource
    {
        /// <summary>The resource type for this component.</summary>
        public readonly new ResourceTypes resourceType = ResourceTypes.Stamina;
    }
}
