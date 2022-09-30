namespace MinecraftClient.Inventory
{
    /// <summary>
    /// Represents a trade of a villager
    /// </summary>
    public class VillagerTrade
    {
        public Item InputItem1;
        public Item OutputItem;
        public Item? InputItem2;
        public bool TradeDisabled;
        public int NumberOfTradeUses;
        public int MaximumNumberOfTradeUses;
        public int Xp;
        public int SpecialPrice;
        public float PriceMultiplier;
        public int Demand;

        public VillagerTrade(Item inputItem1, Item outputItem, Item? inputItem2, bool tradeDisabled, int numberOfTradeUses, int maximumNumberOfTradeUses, int xp, int specialPrice, float priceMultiplier, int demand)
        {
            InputItem1 = inputItem1;
            OutputItem = outputItem;
            InputItem2 = inputItem2;
            TradeDisabled = tradeDisabled;
            NumberOfTradeUses = numberOfTradeUses;
            MaximumNumberOfTradeUses = maximumNumberOfTradeUses;
            Xp = xp;
            SpecialPrice = specialPrice;
            PriceMultiplier = priceMultiplier;
            Demand = demand;
        }
    }
}
