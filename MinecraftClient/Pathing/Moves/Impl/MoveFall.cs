using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Straight-down fall at the current X,Z position, for drops greater than MaxFallHeight
    /// that MoveDescend won't cover. Scans downward for a safe landing.
    /// </summary>
    public sealed class MoveFall : IMove
    {
        public MoveType Type => MoveType.Fall;
        public int XOffset => 0;
        public int ZOffset => 0;
        public bool DynamicY => true;

        private readonly int _maxScanDepth;

        public MoveFall(int maxScanDepth = 256)
        {
            _maxScanDepth = maxScanDepth;
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
        {
            if (!ctx.CanWalkThrough(x, y - 1, z))
            {
                result.SetImpossible();
                return;
            }

            for (int fallDist = 1; fallDist <= _maxScanDepth; fallDist++)
            {
                int landY = y - fallDist;

                if (ctx.CanWalkOn(x, landY - 1, z))
                {
                    if (!ctx.CanWalkThrough(x, landY, z))
                    {
                        result.SetImpossible();
                        return;
                    }

                    if (MoveHelper.IsHazardous(ctx.GetMaterial(x, landY - 1, z)))
                    {
                        result.SetImpossible();
                        return;
                    }

                    double fallDamageThreshold = 3;
                    double cost = ActionCosts.FallCost(fallDist);

                    if (fallDist > fallDamageThreshold)
                        cost += (fallDist - fallDamageThreshold) * 5.0;

                    result.Set(x, landY, z, cost);
                    return;
                }

                if (!ctx.CanWalkThrough(x, landY, z))
                {
                    result.SetImpossible();
                    return;
                }
            }

            result.SetImpossible();
        }
    }
}
