using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Mapping
{
    /// <summary>
    /// Represents a unit movement in the world
    /// </summary>
    /// <see href="http://minecraft.gamepedia.com/Coordinates"/>
    public enum Direction
    {
        South = 0,
        West = 1,
        North = 2,
        East = 3,
        Up = 4,
        Down = 5
    }
}
