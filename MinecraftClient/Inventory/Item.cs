﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

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
        public Dictionary<string, object>? NBT;

        /// <summary>
        /// Create an item with ItemType, Count and Metadata
        /// </summary>
        /// <param name="itemType">Type of the item</param>
        /// <param name="count">Item Count</param>
        /// <param name="nbt">Item Metadata</param>
        public Item(ItemType itemType, int count, Dictionary<string, object>? nbt)
        {
            Type = itemType;
            Count = count;
            NBT = nbt;
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
        public string? DisplayName
        {
            get
            {
                if (NBT != null && NBT.ContainsKey("display"))
                {
                    if (NBT["display"] is Dictionary<string, object> displayProperties && displayProperties.ContainsKey("Name"))
                    {
                        string? displayName = displayProperties["Name"] as string;
                        if (!String.IsNullOrEmpty(displayName))
                            return MinecraftClient.Protocol.ChatParser.ParseText(displayProperties["Name"].ToString()!);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Retrieve item lores from NBT properties. Returns null if no lores is defined.
        /// </summary>
        public string[]? Lores
        {
            get
            {
                List<string> lores = new();
                if (NBT != null && NBT.ContainsKey("display"))
                {
                    if (NBT["display"] is Dictionary<string, object> displayProperties && displayProperties.ContainsKey("Lore"))
                    {
                        object[] displayName = (object[])displayProperties["Lore"];
                        lores.AddRange(from string st in displayName
                                       let str = MinecraftClient.Protocol.ChatParser.ParseText(st.ToString())
                                       select str);
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
                        return int.Parse(damage.ToString()!);
                    }
                }
                return 0;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendFormat("x{0,-2} {1}", Count, Type.ToString());

            string? displayName = DisplayName;
            if (!String.IsNullOrEmpty(displayName))
                sb.AppendFormat(" - {0}§8", displayName);

            int damage = Damage;
            if (damage != 0)
                sb.AppendFormat(" | {0}: {1}", Translations.Get("cmd.inventory.damage"), damage);

            return sb.ToString();
        }
    }
}
