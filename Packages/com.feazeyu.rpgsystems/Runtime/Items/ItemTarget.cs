using System;

namespace Feazeyu.RPGSystems.Items
{
    /// <summary>
    /// Bit flags describing which kinds of entity an item can be applied to.
    /// </summary>
    [Flags]
    public enum ItemTarget
    {
        /// <summary>The item targets the player.</summary>
        Player = 0b1,
    }
}
