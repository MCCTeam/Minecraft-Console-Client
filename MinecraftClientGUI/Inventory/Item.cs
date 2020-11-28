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
        
        /// <summary>
        /// Retrieve item lores from NBT properties. Returns null if no lores is defined.
        /// </summary>
        public string[] Lores
        {
            get
            {
                List<string> lores = new List<string>();
                if (NBT != null && NBT.ContainsKey("display"))
                {
                    var displayProperties = NBT["display"] as Dictionary<string, object>;
                    if (displayProperties != null && displayProperties.ContainsKey("Lore"))
                    {
                        object[] displayName = displayProperties["Lore"] as object[];
                        foreach (string st in displayName)
                        {
                            string str = MinecraftClient.Protocol.ChatParser.ParseText(st.ToString());
                            lores.Add(str);
                        }
                        return lores.ToArray();
                    }
                }
                return null;
            }
        }
        
        /// <summary>
        /// Retrieve item damage from NBT properties. Returns 0 if no damage is defined.
        /// </summary>
        public int Damage
        {
            get
            {
                if (NBT != null && NBT.ContainsKey("Damage"))
                {
                    object damage = NBT["Damage"];
                    if (damage != null)
                    {
                        return int.Parse(damage.ToString());
                    }
                }
                return 0;
            }
        }
    }
}
