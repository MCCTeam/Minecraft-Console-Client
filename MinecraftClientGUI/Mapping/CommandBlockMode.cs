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
    public enum CommandBlockMode
    {
        Sequence = 0,
        Auto = 1,
        Redstone = 2,
    }
}
