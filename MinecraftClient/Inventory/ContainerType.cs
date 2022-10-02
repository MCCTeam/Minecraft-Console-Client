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
        SmightingTable,
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
            return type switch
            {
#pragma warning disable format // @formatter:off
                ContainerTypeOld.CONTAINER         =>  ContainerType.Unknown,
                ContainerTypeOld.CHEST             =>  ContainerType.Generic_9x3,
                ContainerTypeOld.CRAFTING_TABLE    =>  ContainerType.Crafting,
                ContainerTypeOld.FURNACE           =>  ContainerType.Furnace,
                ContainerTypeOld.DISPENSER         =>  ContainerType.Generic_3x3,
                ContainerTypeOld.ENCHANTING_TABLE  =>  ContainerType.Enchantment,
                ContainerTypeOld.BREWING_STAND     =>  ContainerType.BrewingStand,
                ContainerTypeOld.VILLAGER          =>  ContainerType.Merchant,
                ContainerTypeOld.HOPPER            =>  ContainerType.Hopper,
                ContainerTypeOld.DROPPER           =>  ContainerType.Generic_3x3,
                ContainerTypeOld.SHULKER_BOX       =>  ContainerType.ShulkerBox,
                ContainerTypeOld.ENTITYHORSE       =>  ContainerType.Unknown,
                _                                  =>  ContainerType.Unknown,
#pragma warning restore format // @formatter:on
            };
        }
    }
}
