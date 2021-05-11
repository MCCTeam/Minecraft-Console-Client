using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory
{
    /// <summary>
    /// Represents a Minecraft inventory (player inventory, chest, etc.)
    /// </summary>
    public class Container
    {
        /// <summary>
        /// ID of the container on the server
        /// </summary>
        public int ID;

        /// <summary>
        /// Type of container
        /// </summary>
        public ContainerType Type;

        /// <summary>
        /// title of container
        /// </summary>
        public string Title;

        /// <summary>
        /// Container Items
        /// </summary>
        public Dictionary<int, Item> Items;

        /// <summary>
        /// Create an empty container
        /// </summary>
        public Container() { }

        /// <summary>
        /// Create an empty container with ID, Type and Title
        /// </summary>
        /// <param name="id">Container ID</param>
        /// <param name="type">Container Type</param>
        /// <param name="title">Container Title</param>
        public Container(int id, ContainerType type, string title)
        {
            ID = id;
            Type = type;
            Title = title;
            Items = new Dictionary<int, Item>();
        }

        /// <summary>
        /// Create a container with ID, Type, Title and Items
        /// </summary>
        /// <param name="id">Container ID</param>
        /// <param name="type">Container Type</param>
        /// <param name="title">Container Title</param>
        /// <param name="items">Container Items (key: slot ID, value: item info)</param>
        public Container(int id, ContainerType type, string title, Dictionary<int, Item> items)
        {
            ID = id;
            Type = type;
            Title = title;
            Items = items;
        }

        /// <summary>
        /// Create an empty container with ID, Type and Title
        /// </summary>
        /// <param name="id">Container ID</param>
        /// <param name="type">Container Type</param>
        /// <param name="title">Container title</param>
        public Container(int id, ContainerTypeOld type, string title)
        {
            ID = id;
            Title = title;
            Type = ConvertType.ToNew(type);
            Items = new Dictionary<int, Item>();
        }

        /// <summary>
        /// Create an empty container with ID, Type and Title
        /// </summary>
        /// <param name="id">Container ID</param>
        /// <param name="typeID">Container Type</param>
        /// <param name="title">Container Title</param>
        public Container(int id, int typeID, string title)
        {
            ID = id;
            Type = GetContainerType(typeID);
            Title = title;
            Items = new Dictionary<int, Item>();
        }

        /// <summary>
        /// Create an empty container with Type
        /// </summary>
        /// <param name="type">Container Type</param>
        public Container(ContainerType type)
        {
            ID = -1;
            Type = type;
            Title = null;
            Items = new Dictionary<int, Item>();
        }

        /// <summary>
        /// Create an empty container with T^ype and Items
        /// </summary>
        /// <param name="type">Container Type</param>
        /// <param name="items">Container Items (key: slot ID, value: item info)</param>
        public Container(ContainerType type, Dictionary<int, Item> items)
        {
            ID = -1;
            Type = type;
            Title = null;
            Items = items;
        }

        /// <summary>
        /// Get container type from Type ID
        /// </summary>
        /// <param name="typeID">Container Type ID</param>
        /// <returns>Container Type</returns>
        public static ContainerType GetContainerType(int typeID)
        {
            // https://wiki.vg/Inventory didn't state the inventory ID, assume that list start with 0
            switch (typeID)
            {
                case 0: return ContainerType.Generic_9x1;
                case 1: return ContainerType.Generic_9x2;
                case 2: return ContainerType.Generic_9x3;
                case 3: return ContainerType.Generic_9x4;
                case 4: return ContainerType.Generic_9x5;
                case 5: return ContainerType.Generic_9x6;
                case 6: return ContainerType.Generic_3x3;
                case 7: return ContainerType.Anvil;
                case 8: return ContainerType.Beacon;
                case 9: return ContainerType.BlastFurnace;
                case 10: return ContainerType.BrewingStand;
                case 11: return ContainerType.Crafting;
                case 12: return ContainerType.Enchantment;
                case 13: return ContainerType.Furnace;
                case 14: return ContainerType.Grindstone;
                case 15: return ContainerType.Hopper;
                case 16: return ContainerType.Lectern;
                case 17: return ContainerType.Loom;
                case 18: return ContainerType.Merchant;
                case 19: return ContainerType.ShulkerBox;
                case 20: return ContainerType.Smoker;
                case 21: return ContainerType.Cartography;
                case 22: return ContainerType.Stonecutter;
                default: return ContainerType.Unknown;
            }
        }

        /// <summary>
        /// Search an item in the container
        /// </summary>
        /// <param name="itemType">The item to search</param>
        /// <returns>An array of slot ID</returns>
        public int[] SearchItem(ItemType itemType)
        {
            List<int> result = new List<int>();
            if (Items != null)
            {
                foreach (var item in Items)
                {
                    if (item.Value.Type == itemType)
                        result.Add(item.Key);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// List empty slots in the container
        /// </summary>
        /// <returns>An array of slot ID</returns>
        /// <remarks>Also depending on the container type, some empty slots cannot be used e.g. armor slots. This might cause issues.</remarks>
        public int[] GetEmpytSlots()
        {
            List<int> result = new List<int>();
            for (int i = 0; i < Type.SlotCount(); i++)
            {
                result.Add(i);
            }
            foreach (var item in Items)
            {
                result.Remove(item.Key);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Check the given slot ID is a hotbar slot and give the hotbar number
        /// </summary>
        /// <param name="slotId">The slot ID to check</param>
        /// <param name="hotbar">Zero-based, 0-8. -1 if not a hotbar</param>
        /// <returns>True if given slot ID is a hotbar slot</returns>
        public bool IsHotbar(int slotId, out int hotbar)
        {
            int hotbarStart = Type.SlotCount() - 9;
            // Remove offhand slot
            if (Type == ContainerType.PlayerInventory)
                hotbarStart--;
            if ((slotId >= hotbarStart) && (slotId <= hotbarStart + 9))
            {
                hotbar = slotId - hotbarStart;
                return true;
            }
            else
            {
                hotbar = -1;
                return false;
            }
        }
    }
}
