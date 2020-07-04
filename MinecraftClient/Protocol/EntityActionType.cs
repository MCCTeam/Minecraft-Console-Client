using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Protocol
{
    public enum EntityActionType
    {
        StartSneaking = 0,
        StopSneaking = 1,
        LeaveBed = 2,
        StartSprinting = 3,
        StopSprinting = 4,
        StartJumpWithHorse = 5,
        StopJumpWithHorse = 6,
        OpenHorseInventory = 7,
        StartFlyingWithElytra = 8,
    }
}
