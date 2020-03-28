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
        /// Slot ID in the parent inventory (-1 means not specified)
        /// </summary>
        public int SlotID = -1;

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
        public Item(int ID,int Count,int SlotID, Dictionary<string, object> NBT)
        {
            this.ID = ID;
            this.Count = Count;
            this.SlotID = SlotID;
            this.NBT = NBT;
        }

        /// <summary>
        /// Create an item with Type ID, Count and Slot ID
        /// </summary>
        /// <param name="ID">Item Type ID</param>
        /// <param name="Count">Item Count</param>
        /// <param name="SlotID">Item Slot ID in parent inventory</param>
        public Item(int ID, int Count, int SlotID)
        {
            this.ID = ID;
            this.Count = Count;
            this.SlotID = SlotID;
        }

        /// <summary>
        /// Create an item with Type ID and Count
        /// </summary>
        /// <param name="ID">Item Type ID</param>
        /// <param name="Count">Item Count</param>
        public Item(int ID, int Count)
        {
            this.ID = ID;
            this.Count = Count;
        }
    }
}
