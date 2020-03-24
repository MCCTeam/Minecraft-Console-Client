using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory
{
    public class Container
    {
        public int ID;
        public ContainerType Type;
        public string Title;
        public Dictionary<int, Item> Items;

        public Container(int id, ContainerType type, string title)
        {
            ID = id;
            Type = type;
            Title = title;
        }
        public Container(int id, Protocol.InventoryType type, string title)
        {
            ID = id;
            Title = title;
        }
        public Container(int id, int typeID, string title)
        {
            ID = id;
            Type = GetContainerType(typeID);
            Title = title;
        }

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
    }
}
