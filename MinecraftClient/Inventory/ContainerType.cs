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
        // not in the wiki.vg list
        PlayerInventory,
        Unknown
    }
    public enum ContainerTypeOld
    {
        CONTAINER,
        CHEST,
        CRAFTING_TABLE,
        FURNACE,
        DISPENSER,
        ENCHANTING_TABLE,
        BREWING_STAND,
        VILLAGER,
        BEACON,
        ANVIL,
        HOPPER,
        DROPPER,
        SHULKER_BOX,
        ENTITYHORSE
    }
    public static class ConvertType
    {
        public static ContainerType ToNew(ContainerTypeOld type)
        {
            switch (type)
            {
                case ContainerTypeOld.CONTAINER: return ContainerType.Unknown;
                case ContainerTypeOld.CHEST: return ContainerType.Generic_9x3;
                case ContainerTypeOld.CRAFTING_TABLE: return ContainerType.Crafting;
                case ContainerTypeOld.FURNACE: return ContainerType.Furnace;
                case ContainerTypeOld.DISPENSER: return ContainerType.Generic_3x3;
                case ContainerTypeOld.ENCHANTING_TABLE: return ContainerType.Enchantment;
                case ContainerTypeOld.BREWING_STAND: return ContainerType.BrewingStand;
                case ContainerTypeOld.VILLAGER: return ContainerType.Merchant;
                case ContainerTypeOld.HOPPER: return ContainerType.Hopper;
                case ContainerTypeOld.DROPPER: return ContainerType.Generic_3x3;
                case ContainerTypeOld.SHULKER_BOX: return ContainerType.ShulkerBox;
                case ContainerTypeOld.ENTITYHORSE: return ContainerType.Unknown;
                default: return ContainerType.Unknown;
            }
        }
    }
}
