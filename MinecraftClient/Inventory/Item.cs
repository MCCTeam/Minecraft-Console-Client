using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MinecraftClient.Protocol.Message;

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
        /// Item Count
        /// </summary>
        public int Data;

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
        
        public Item(ItemType itemType, int count, int data, Dictionary<string, object>? nbt) : this(itemType, count, nbt)
        {
            Data = data;
        }

        /// <summary>
        /// Check if the item slot is empty
        /// </summary>
        /// <returns>TRUE if the item is empty</returns>
        public bool IsEmpty
        {
            get { return Type == ItemType.Air || Count == 0; }
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
                    if (NBT["display"] is Dictionary<string, object> displayProperties &&
                        displayProperties.ContainsKey("Name"))
                    {
                        string? displayName = displayProperties["Name"] as string;
                        if (!String.IsNullOrEmpty(displayName))
                            return ChatParser.ParseText(displayProperties["Name"].ToString() ?? string.Empty);
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
                    if (NBT["display"] is Dictionary<string, object> displayProperties &&
                        displayProperties.ContainsKey("Lore"))
                    {
                        object[] displayName = (object[])displayProperties["Lore"];
                        lores.AddRange(from string st in displayName
                            let str = ChatParser.ParseText(st.ToString())
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
                        return int.Parse(damage.ToString() ?? string.Empty, NumberStyles.Any,
                            CultureInfo.CurrentCulture);
                    }
                }

                return 0;
            }
        }

        public static string GetTypeString(ItemType type)
        {
            string type_str = type.ToString();
            string type_renamed = type_str.ToUnderscoreCase();
            string? res1 = ChatParser.TranslateString("item.minecraft." + type_renamed);
            if (!string.IsNullOrEmpty(res1))
                return res1;
            string? res2 = ChatParser.TranslateString("block.minecraft." + type_renamed);
            if (!string.IsNullOrEmpty(res2))
                return res2;
            return type_str;
        }

        public string GetTypeString()
        {
            return GetTypeString(Type);
        }

        public string ToFullString()
        {
            StringBuilder sb = new();
            sb.Append(ToString());

            try
            {
                if (NBT != null && (NBT.TryGetValue("Enchantments", out object? enchantments) ||
                                    NBT.TryGetValue("StoredEnchantments", out enchantments)))
                {
                    foreach (Dictionary<string, object> enchantment in (object[])enchantments)
                    {
                        short level = (short)enchantment["lvl"];
                        string id = ((string)enchantment["id"]).Replace(':', '.');
                        sb.AppendFormat(" | {0} {1}",
                            ChatParser.TranslateString("enchantment." + id) ?? id,
                            ChatParser.TranslateString("enchantment.level." + level) ?? level.ToString());
                    }
                }

                if (Lores != null && Lores.Length > 0)
                {
                    foreach (var lore in Lores)
                        sb.AppendFormat(" | {0}", lore);
                }
            }
            catch (Exception)
            {
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.AppendFormat("x{0,-2} {1}", Count, GetTypeString());

            string? displayName = DisplayName;
            if (!String.IsNullOrEmpty(displayName))
                sb.AppendFormat(" - {0}§8", displayName);

            int damage = Damage;
            if (damage != 0)
                sb.AppendFormat(" | {0}: {1}", Translations.cmd_inventory_damage, damage);

            return sb.ToString();
        }
    }
}