namespace MinecraftClient.Inventory
{    /// <summary>
     /// Represents a trade of a villager
     /// </summary>
    public class Trade
    {
        public Item inputItem1;
        public Item outputItem;
        public bool hasSecondItem;
        public Item inputItem2;
        public bool tradeDisabled;
        public int numberOfTradeUses;
        public int maximumNumberOfTradeUses;
        public int xp;
        public int specialPrice;
        public float priceMultiplier;
        public int demand;

        public Trade(Item inputItem1, Item outputItem, bool hasSecondItem, Item inputItem2, bool tradeDisabled, int numberOfTradeUses, int maximumNumberOfTradeUses, int xp, int specialPrice, float priceMultiplier, int demand)
        {
            this.inputItem1 = inputItem1;
            this.outputItem = outputItem;
            this.hasSecondItem = hasSecondItem;
            this.inputItem2 = inputItem2;
            this.tradeDisabled = tradeDisabled;
            this.numberOfTradeUses = numberOfTradeUses;
            this.maximumNumberOfTradeUses = maximumNumberOfTradeUses;
            this.xp = xp;
            this.specialPrice = specialPrice;
            this.priceMultiplier = priceMultiplier;
            this.demand = demand;
        }
    }
}