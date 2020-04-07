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

        /// <summary>
        /// Check item is a food
        /// </summary>
        /// <returns>True if is a food</returns>
        public bool IsFood()
        {
            // non-poison and stackable food
            // remarks: auto eat may works with non-stackable food <- not tested
            int[] foods = { 524, 765, 821, 823, 562, 763, 680, 629, 801, 585, 788, 630, 670, 674, 588, 587, 768, 673, 764, 777, 677, 679, 625, 800, 584, 787, 626, 678, 876, 627 };
            if (foods.Contains((int)Type))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
