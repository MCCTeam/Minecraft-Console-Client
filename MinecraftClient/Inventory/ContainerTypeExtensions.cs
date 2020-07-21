using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory
{
    public static class ContainerTypeExtensions
    {
        public static int SlotCount(this ContainerType c)
        {
            switch (c)
            {
                case ContainerType.PlayerInventory: return 44;
                case ContainerType.Generic_9x3: return 62;
                case ContainerType.Generic_9x6: return 89;
                case ContainerType.Generic_3x3: return 44;
                case ContainerType.Crafting: return 45;
                case ContainerType.BlastFurnace: return 38;
                case ContainerType.Furnace: return 38;
                case ContainerType.Smoker: return 38;
                case ContainerType.Enchantment: return 37;
                case ContainerType.BrewingStand: return 40;
                case ContainerType.Merchant: return 38;
                case ContainerType.Beacon: return 36;
                case ContainerType.Anvil: return 38;
                case ContainerType.Hopper: return 40;
                case ContainerType.ShulkerBox: return 62;
                case ContainerType.Loom: return 39;
                case ContainerType.Stonecutter: return 37;
                case ContainerType.Lectern: return 36;
                case ContainerType.Cartography: return 38;
                case ContainerType.Grindstone: return 38;
                case ContainerType.Unknown: return 0;
                default: return 0;
            }
        }
    }
}
