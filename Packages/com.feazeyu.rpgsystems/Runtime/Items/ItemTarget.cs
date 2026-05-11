using System;

namespace Feazeyu.RPGSystems.Items
{
    [Flags]
    public enum ItemTarget
    {
        Player = 0b1,
        Tower = 0b10,
    }
}
