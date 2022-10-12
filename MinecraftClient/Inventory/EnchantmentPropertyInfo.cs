namespace MinecraftClient.Inventory
{
    public enum EnchantmentPropertyInfo
    {
        // Levels that are required to enchant an item
        TopEnchantmentLevelRequirement = 0,
        MiddleEnchantmentLevelRequirement,
        BottomEnchantmentLevelRequirement,

        // Seed
        EnchantmentSeed,

        // Enchantment ids
        TopEnchantmentId,
        MiddleEnchantmentId,
        BottomEnchantmentId,

        // Shown on mouse hover over the top, middle and bottom slot
        TopEnchantmentLevel,
        MiddleEnchantmentLevel,
        BottomEnchantmentLevel
    }
}
