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
        /// Item Type
        /// </summary>
        public ItemType Type;

        /// <summary>
        /// Item Count
        /// </summary>
        public int Count;

        /// <summary>
        /// Item Metadata
        /// </summary>
        public Dictionary<string, object> NBT;

        /// <summary>
        /// Create an item with Type ID, Count and Metadata
        /// </summary>
        /// <param name="ID">Item Type ID</param>
        /// <param name="Count">Item Count</param>
        /// <param name="NBT">Item Metadata</param>
        public Item(int id, int count, Dictionary<string, object> nbt)
        {
            this.Type = (ItemType)id;
            this.Count = count;
            this.NBT = nbt;
        }

        /// <summary>
        /// Check if the item slot is empty
        /// </summary>
        /// <returns>TRUE if the item is empty</returns>
        public bool IsEmpty
        {
            get
            {
                return Type == ItemType.Air || Count == 0;
            }
        }
    }
}
