using System;

namespace Game.Items
{
    [Flags]
    public enum ItemTarget
    {
        Player = 0b1,
        Tower = 0b10,
    }
}
