namespace MinecraftClient.Inventory
{
    /// <summary>
    /// Represents a trade of a villager
    /// </summary>
    public record VillagerTrade(
        Item InputItem1,
        Item OutputItem,
        Item? InputItem2,
        bool TradeDisabled,
        int NumberOfTradeUses,
        int MaximumNumberOfTradeUses,
        int Xp,
        int SpecialPrice,
        float PriceMultiplier,
        int Demand);
}
