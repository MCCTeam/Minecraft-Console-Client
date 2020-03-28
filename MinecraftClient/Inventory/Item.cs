using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory
{
    /// <summary>
    /// Represents an item inside a Container
    /// </summary>
    public class Item
    {
        /// <summary>
        /// Item Type ID
        /// </summary>
        public int ID;

        /// <summary>
        /// Item Count
        /// </summary>
        public int Count;

        /// <summary>
        /// Slot ID in the parent inventory
        /// </summary>
        /// <remarks>-1 means currently being dragged by mouse</remarks>
        public int SlotID;

        /// <summary>
        /// Item Metadata
        /// </summary>
        public Dictionary<string, object> NBT;

        /// <summary>
        /// Create an item with Type ID, Count, Slot ID and Metadata
        /// </summary>
        /// <param name="ID">Item Type ID</param>
        /// <param name="Count">Item Count</param>
        /// <param name="SlotID">Item Slot ID in parent inventory</param>
        /// <param name="NBT">Item Metadata</param>
        public Item(int id, int count, int slotID, Dictionary<string, object> nbt)
        {
            this.ID = id;
            this.Count = count;
            this.SlotID = slotID;
            this.NBT = nbt;
        }

        /// <summary>
        /// Create an item with Type ID, Count and Slot ID
        /// </summary>
        /// <param name="ID">Item Type ID</param>
        /// <param name="Count">Item Count</param>
        /// <param name="SlotID">Item Slot ID in parent inventory</param>
        public Item(int id, int count, int slotID)
        {
            this.ID = id;
            this.Count = count;
            this.SlotID = slotID;
            this.NBT = new Dictionary<string, object>();
        }
    }
}
