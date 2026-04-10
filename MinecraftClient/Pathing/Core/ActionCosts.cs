namespace MinecraftClient.Pathing.Core
{
    /// <summary>
    /// All pathfinding movement costs in ticks, derived from vanilla walking/sprinting speeds.
    /// Mirrors Baritone's ActionCosts design.
    /// </summary>
    public static class ActionCosts
    {
        public const double WalkOneBlock = 20.0 / 4.317;
        public const double SprintOneBlock = 20.0 / 5.612;
        public const double SneakOneBlock = 20.0 / 1.3;
        public const double LadderUpOne = 20.0 / 2.35;
        public const double LadderDownOne = 20.0 / 3.0;
        public const double WalkOffBlock = WalkOneBlock * 0.8;
        public const double SprintMultiplier = SprintOneBlock / WalkOneBlock;
        public const double DiagonalMultiplier = 1.4142135623730951;
        public const double CostInf = 1_000_000;

        public const double JumpPenalty = 2.0;

        public static readonly double[] FallNBlocksCost = BuildFallTable(257);

        private static double[] BuildFallTable(int maxBlocks)
        {
            var table = new double[maxBlocks];
            table[0] = 0;

            double velocity = 0;
            double distance = 0;
            int ticks = 0;
            int blockIndex = 1;

            while (blockIndex < maxBlocks)
            {
                velocity += 0.08;
                velocity *= 0.98;
                distance += velocity;
                ticks++;

                while (blockIndex < maxBlocks && distance >= blockIndex)
                {
                    table[blockIndex] = ticks;
                    blockIndex++;
                }

                if (ticks > 10000)
                    break;
            }

            for (int i = blockIndex; i < maxBlocks; i++)
                table[i] = CostInf;

            return table;
        }

        public static double FallCost(int blocks)
        {
            if (blocks < 0 || blocks >= FallNBlocksCost.Length)
                return CostInf;
            return FallNBlocksCost[blocks];
        }
    }
}
