using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace MinecraftClient.Inventory
{
    /// <summary>
    /// Class that contains useful methods to move item around in a container
    /// </summary>
    public class ItemMovingHelper
    {
        private Container c;
        private McClient mc;

        /// <summary>
        /// Create a helper that contains useful methods to move item around in container
        /// </summary>
        /// <param name="c">Source container to use. All method will use this container for handling first slot parameter</param>
        /// <param name="mc">McClient handler. Needed for sending WindowAction packet to the server</param>
        /// <remarks>
        /// If you are using ChatBot API and cannot have direct access to McClient handler, use <see cref="ChatBot.WindowAction(int, int, WindowActionType)"/> as second parameter
        /// </remarks>
        public ItemMovingHelper(Container c, McClient mc)
        {
            this.c = c;
            this.mc = mc;
        }

        /// <summary>
        /// Move an item fron source to dest. Source should contain an item and dest slot should be empty
        /// </summary>
        /// <param name="source">Source slot</param>
        /// <param name="dest">Dest slot</param>
        /// <param name="destContainer">Dest slot's container (only if dest slot is in other container)</param>
        /// <returns>True if success or false if Failed</returns>
        public bool MoveTo(int source, int dest, Container destContainer = null)
        {
            // Condition: source has item and dest has no item
            if (ValidateSlots(source, dest, destContainer) &&
                HasItem(source) &&
                ((destContainer != null && !HasItem(dest, destContainer)) || (destContainer == null && !HasItem(dest))))
                return mc.DoWindowAction(c.ID, source, WindowActionType.LeftClick)
                    && mc.DoWindowAction(destContainer == null ? c.ID : destContainer.ID, dest, WindowActionType.LeftClick);
            else return false;
        }

        /// <summary>
        /// Swap two item. Both slots should contain item
        /// </summary>
        /// <param name="slot1">Slot 1</param>
        /// <param name="slot2">Slot 2</param>
        /// <param name="destContainer">Slot2 container (only if slot2 is in other container)</param>
        /// <returns>True if success or False if Failed</returns>
        public bool Swap(int slot1, int slot2, Container destContainer = null)
        {
            // Condition: Both slot1 and slot2 has item
            if (ValidateSlots(slot1, slot2, destContainer) &&
                HasItem(slot1) &&
                (destContainer != null && HasItem(slot2, destContainer) || (destContainer == null && HasItem(slot2))))
                return mc.DoWindowAction(c.ID, slot1, WindowActionType.LeftClick)
                    && mc.DoWindowAction(destContainer == null ? c.ID : destContainer.ID, slot2, WindowActionType.LeftClick)
                    && mc.DoWindowAction(c.ID, slot1, WindowActionType.LeftClick);
            else return false;
        }

        /// <summary>
        /// Drag a stack of item over a set of slots. Those slots should be empty or have the same item as source item
        /// </summary>
        /// <param name="source">Source item</param>
        /// <param name="slots">Slots to be drag over</param>
        /// <param name="mouseKey">Which mouse key to use: StartDragLeft, StartDragRight or StartDragMiddle</param>
        /// <returns>True if success or false if Failed</returns>
        /// <remarks>
        /// After calling this method, you will need to wait until the server have updated the player inventory before doing next inventory action
        /// </remarks>
        public bool DragOverSlots(int source, IEnumerable<int> slots, WindowActionType mouseKey = WindowActionType.StartDragLeft)
        {
            if (!HasItem(source))
                return false;
            List<int> availableSlots = new List<int>(slots.Count());
            // filter out different item type or non-empty slots (they will be ignored silently)
            foreach (var slot in slots)
                if (ItemTypeEqual(source, slot) || !HasItem(slot))
                    availableSlots.Add(slot);
            if (availableSlots.Count > 0)
            {
                WindowActionType startDragging = WindowActionType.StartDragLeft;
                WindowActionType addDragging = WindowActionType.AddDragLeft;
                WindowActionType endDragging = WindowActionType.EndDragLeft;
                switch (mouseKey)
                {
                    case WindowActionType.StartDragRight:
                        {
                            startDragging = WindowActionType.StartDragRight;
                            addDragging = WindowActionType.AddDragRight;
                            endDragging = WindowActionType.EndDragRight;
                            break;
                        }
                    case WindowActionType.StartDragMiddle:
                        {
                            startDragging = WindowActionType.StartDragMiddle;
                            addDragging = WindowActionType.AddDragMiddle;
                            endDragging = WindowActionType.EndDragMiddle;
                            break;
                        }
                }
                mc.DoWindowAction(c.ID, source, WindowActionType.LeftClick); // grab item
                mc.DoWindowAction(c.ID, -999, startDragging);
                foreach (var slot in availableSlots)
                {
                    mc.DoWindowAction(c.ID, slot, addDragging);
                }
                mc.DoWindowAction(c.ID, -999, endDragging);
                mc.DoWindowAction(c.ID, source, WindowActionType.LeftClick); // put down item left (if any)
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Validate two slot by comparing they are different and within the maximum slot count of the container
        /// </summary>
        /// <param name="s1">Slot 1</param>
        /// <param name="s2">Slot 2</param>
        /// <param name="s2Container">Second container (only if slot2 is in other container)</param>
        /// <returns>The compare result</returns>
        private bool ValidateSlots(int s1, int s2, Container s2Container = null)
        {
            if (s2Container == null)
                return (s1 != s2 && s1 < c.Type.SlotCount() && s2 < c.Type.SlotCount());
            else
                return (s1 < c.Type.SlotCount() && s2 < s2Container.Type.SlotCount());
        }

        /// <summary>
        /// Check if the slot has an item inside
        /// </summary>
        /// <param name="slot">Slot ID</param>
        /// <param name="c">Specify another contianer (only if the slot is in other container)</param>
        /// <returns>True if has item</returns>
        private bool HasItem(int slot, Container c = null)
        {
            if (c == null)
                c = this.c;
            return c.Items.ContainsKey(slot);
        }

        /// <summary>
        /// Check both slots item type are the same
        /// </summary>
        /// <param name="slot1"></param>
        /// <param name="slot2"></param>
        /// <param name="s2Container">Second container (only if slot2 is in other container)</param>
        /// <returns>True if they are equal</returns>
        private bool ItemTypeEqual(int slot1, int slot2, Container s2Container = null)
        {
            if (s2Container == null)
            {
                if (HasItem(slot1) && HasItem(slot2))
                    return c.Items[slot1].Type == c.Items[slot2].Type;
                else return false;
            }
            else
            {
                if (HasItem(slot1) && HasItem(slot2, s2Container))
                    return c.Items[slot1].Type == s2Container.Items[slot2].Type;
                else return false;
            }
        }
    }
}
