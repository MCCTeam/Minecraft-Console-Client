using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinecraftClient.Inventory
{
    // For MC 1.14 after ONLY
    public enum ContainerType
    {
        Generic_9x1,
        Generic_9x2,
        Generic_9x3, // chest, ender chest, minecart with chest, barrel
        Generic_9x4,
        Generic_9x5,
        Generic_9x6,
        Generic_3x3,
        Anvil,
        Beacon,
        BlastFurnace,
        BrewingStand,
        Crafting,
        Enchantment,
        Furnace,
        Grindstone,
        Hopper,
        Lectern,
        Loom,
        Merchant,
        ShulkerBox,
        Smoker,
        Cartography,
        Stonecutter,
        // not in the list
        PlayerInventory,
        Unknown
    }
}
