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
    public enum CommandBlockFlags
    {
        TrackOutput = 0x01,
        IsConditional = 0x02,
        Automatic = 0x04,
    }
}
