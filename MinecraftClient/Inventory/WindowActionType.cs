using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory
{
    /// <summary>
    /// Represents mouse interactions with an inventory window
    /// </summary>
    public enum WindowActionType
    {
        /// <summary>
        /// Left click with mouse on a slot: grab or drop a whole item stack
        /// </summary>
        LeftClick,

        /// <summary>
        /// Right click with mouse on a slot: grab half a stack or drop a single item
        /// </summary>
        RightClick,

        /// <summary>
        /// Middle click with mouse on a slot: grab a full stack from creative inventory
        /// </summary>
        MiddleClick,

        /// <summary>
        /// Shift+Left click with mouse on a slot: send a whole item stack to the hotbar or other inventory
        /// </summary>
        ShiftClick,

        /// <summary>
        /// Drop a single item on ground
        /// </summary>
        DropItem,

        /// <summary>
        /// Drop a whole item stack on ground
        /// </summary>
        DropItemStack,

        /// <summary>
        /// Start hovering slots with left button pressed: Distribute evenly the stack on hovered slots
        /// </summary>
        StartDragLeft,

        /// <summary>
        /// Start hovering slots with right button pressed: Drop one item on each hovered slot
        /// </summary>
        StartDragRight,

        /// <summary>
        /// Start hovering slots with middle button pressed: Create one item stack on each hovered slot in creative mode
        /// </summary>
        StartDragMiddle,

        /// <summary>
        /// Hover a slot to distribute evenly an item stack
        /// </summary>
        AddDragLeft,

        /// <summary>
        /// Hover a slot to drop one item from an item stack
        /// </summary>
        AddDragRight,

        /// <summary>
        /// Hover a slot to create one item stack in creative mode
        /// </summary>
        AddDragMiddle,

        /// <summary>
        /// Stop hovering slots with left button pressed
        /// </summary>
        EndDragLeft,

        /// <summary>
        /// Stop hovering slots with right button pressed
        /// </summary>
        EndDragRight,

        /// <summary>
        /// Stop hovering slots with middble button pressed
        /// </summary>
        EndDragMiddle,
    }
}
