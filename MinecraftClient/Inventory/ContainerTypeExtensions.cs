namespace MinecraftClient.Inventory
{
    public static class ContainerTypeExtensions
    {
        /// <summary>
        /// Get the slot count of the container
        /// </summary>
        /// <param name="c"></param>
        /// <returns>Slot count of the container</returns>
        public static int SlotCount(this ContainerType c)
        {
            switch (c)
            {
                case ContainerType.PlayerInventory: return 46;
                case ContainerType.Generic_9x3: return 63;
                case ContainerType.Generic_9x6: return 90;
                case ContainerType.Generic_3x3: return 45;
                case ContainerType.Crafting: return 46;
                case ContainerType.BlastFurnace: return 39;
                case ContainerType.Furnace: return 39;
                case ContainerType.Smoker: return 39;
                case ContainerType.Enchantment: return 38;
                case ContainerType.BrewingStand: return 41;
                case ContainerType.Merchant: return 39;
                case ContainerType.Beacon: return 37;
                case ContainerType.Anvil: return 39;
                case ContainerType.Hopper: return 41;
                case ContainerType.ShulkerBox: return 63;
                case ContainerType.Loom: return 40;
                case ContainerType.Stonecutter: return 38;
                case ContainerType.Lectern: return 37;
                case ContainerType.Cartography: return 39;
                case ContainerType.Grindstone: return 39;
                case ContainerType.Unknown: return 0;
                default: return 0;
            }
        }

        /// <summary>
        /// Get an ASCII art representation of the container
        /// </summary>
        /// <param name="c"></param>
        /// <returns>ASCII art representation or NULL if not implemented for this container type</returns>
        public static string? GetAsciiArt(this ContainerType c)
        {
            return c switch
            {
                ContainerType.PlayerInventory => DefaultConfigResource.ContainerType_PlayerInventory,
                ContainerType.Generic_9x3 => DefaultConfigResource.ContainerType_Generic_9x3,
                ContainerType.Generic_9x6 => DefaultConfigResource.ContainerType_Generic_9x6,
                ContainerType.Generic_3x3 => DefaultConfigResource.ContainerType_Generic_3x3,
                ContainerType.Crafting => DefaultConfigResource.ContainerType_Crafting,
                ContainerType.BlastFurnace => DefaultConfigResource.ContainerType_Furnace,
                ContainerType.Furnace => DefaultConfigResource.ContainerType_Furnace,
                ContainerType.Smoker => DefaultConfigResource.ContainerType_Furnace,
                ContainerType.Enchantment => null,
                ContainerType.BrewingStand => DefaultConfigResource.ContainerType_BrewingStand,
                ContainerType.Merchant => null,
                ContainerType.Beacon => null,
                ContainerType.Anvil => null,
                ContainerType.Hopper => DefaultConfigResource.ContainerType_Hopper,
                ContainerType.ShulkerBox => DefaultConfigResource.ContainerType_Generic_9x3,
                ContainerType.Loom => null,
                ContainerType.Stonecutter => null,
                ContainerType.Lectern => null,
                ContainerType.Cartography => null,
                ContainerType.Grindstone => DefaultConfigResource.ContainerType_Grindstone,
                ContainerType.Unknown => null,
                ContainerType.Generic_9x1 => null,
                ContainerType.Generic_9x2 => null,
                ContainerType.Generic_9x4 => null,
                ContainerType.Generic_9x5 => null,
                ContainerType.SmightingTable => null,
                _ => null,
            };
        }
    }
}
