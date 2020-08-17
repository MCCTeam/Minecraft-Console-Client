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
        /// Create an item with ItemType, Count and Metadata
        /// </summary>
        /// <param name="itemType">Type of the item</param>
        /// <param name="count">Item Count</param>
        /// <param name="nbt">Item Metadata</param>
        public Item(ItemType itemType, int count, Dictionary<string, object> nbt)
        {
            this.Type = itemType;
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

        /// <summary>
        /// Retrieve item display name from NBT properties. NULL if no display name is defined.
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (NBT != null && NBT.ContainsKey("display"))
                {
                    var displayProperties = NBT["display"] as Dictionary<string, object>;
                    if (displayProperties != null && displayProperties.ContainsKey("Name"))
                    {
                        string displayName = displayProperties["Name"] as string;
                        if (!String.IsNullOrEmpty(displayName))
                            return MinecraftClient.Protocol.ChatParser.ParseText(displayProperties["Name"].ToString());
                    }
                }
                return null;
            }
        }
    }
}
