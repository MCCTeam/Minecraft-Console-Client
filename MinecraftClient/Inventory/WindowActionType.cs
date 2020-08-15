using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory
{
    public enum WindowActionType
    {
        LeftClick,
        RightClick,
        MiddleClick,
        ShiftClick,
        DropItem,
        DropItemStack,
        StartDragLeft,
        StartDragRight,
        StartDragMiddle,
        EndDragLeft,
        EndDragRight,
        EndDragMiddle,
        AddDragLeft,
        AddDragRight,
        AddDragMiddle,
    }
}
