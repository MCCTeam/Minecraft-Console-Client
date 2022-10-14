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
            return c switch
            {
#pragma warning disable format // @formatter:off
                ContainerType.PlayerInventory  =>  46,
                ContainerType.Generic_9x3      =>  63,
                ContainerType.Generic_9x6      =>  90,
                ContainerType.Generic_3x3      =>  45,
                ContainerType.Crafting         =>  46,
                ContainerType.BlastFurnace     =>  39,
                ContainerType.Furnace          =>  39,
                ContainerType.Smoker           =>  39,
                ContainerType.Enchantment      =>  38,
                ContainerType.BrewingStand     =>  41,
                ContainerType.Merchant         =>  39,
                ContainerType.Beacon           =>  37,
                ContainerType.Anvil            =>  39,
                ContainerType.Hopper           =>  41,
                ContainerType.ShulkerBox       =>  63,
                ContainerType.Loom             =>  40,
                ContainerType.Stonecutter      =>  38,
                ContainerType.Lectern          =>  37,
                ContainerType.Cartography      =>  39,
                ContainerType.Grindstone       =>  39,
                ContainerType.Unknown          =>  0,
                _                              =>  0,
#pragma warning restore format // @formatter:on
            };
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
#pragma warning disable format // @formatter:off
                ContainerType.PlayerInventory  =>  DefaultConfigResource.ContainerType_PlayerInventory,
                ContainerType.Generic_9x3      =>  DefaultConfigResource.ContainerType_Generic_9x3,
                ContainerType.Generic_9x6      =>  DefaultConfigResource.ContainerType_Generic_9x6,
                ContainerType.Generic_3x3      =>  DefaultConfigResource.ContainerType_Generic_3x3,
                ContainerType.Crafting         =>  DefaultConfigResource.ContainerType_Crafting,
                ContainerType.BlastFurnace     =>  DefaultConfigResource.ContainerType_Furnace,
                ContainerType.Furnace          =>  DefaultConfigResource.ContainerType_Furnace,
                ContainerType.Smoker           =>  DefaultConfigResource.ContainerType_Furnace,
                ContainerType.Enchantment      =>  DefaultConfigResource.ContainerType_EnchantingTable,
                ContainerType.BrewingStand     =>  DefaultConfigResource.ContainerType_BrewingStand,
                ContainerType.Merchant         =>  null,
                ContainerType.Beacon           =>  null,
                ContainerType.Anvil            =>  null,
                ContainerType.Hopper           =>  DefaultConfigResource.ContainerType_Hopper,
                ContainerType.ShulkerBox       =>  DefaultConfigResource.ContainerType_Generic_9x3,
                ContainerType.Loom             =>  null,
                ContainerType.Stonecutter      =>  null,
                ContainerType.Lectern          =>  null,
                ContainerType.Cartography      =>  null,
                ContainerType.Grindstone       =>  DefaultConfigResource.ContainerType_Grindstone,
                ContainerType.Unknown          =>  null,
                ContainerType.Generic_9x1      =>  null,
                ContainerType.Generic_9x2      =>  null,
                ContainerType.Generic_9x4      =>  null,
                ContainerType.Generic_9x5      =>  null,
                ContainerType.SmightingTable   =>  null,
                _                              =>  null,
#pragma warning restore format // @formatter:on
            };
        }
    }
}
