namespace MinecraftClient.Inventory
{
    public class EnchantmentData
    {
        public Enchantment TopEnchantment { get; set; }
        public Enchantment MiddleEnchantment { get; set; }
        public Enchantment BottomEnchantment { get; set; }

        // Seed for rendering Standard Galactic Language (symbols in the enchanting table) (Useful for poeple who use MCC for the protocol)
        public short Seed { get; set; }

        /// Enchantment levels are the levels of enchantment (eg. I, II, III, IV, V) (eg. Smite IV, Power III, Knockback II ..)
        public short TopEnchantmentLevel { get; set; }
        public short MiddleEnchantmentLevel { get; set; }
        public short BottomEnchantmentLevel { get; set; }

        /// Enchantment level requirements are the levels that player needs to have in order to enchant the item
        public short TopEnchantmentLevelRequirement { get; set; }
        public short MiddleEnchantmentLevelRequirement { get; set; }
        public short BottomEnchantmentLevelRequirement { get; set; }
    }
}
