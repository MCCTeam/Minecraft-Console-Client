using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Walk off a ledge and drop 1-N blocks in a cardinal direction.
    /// Scans downward for a landing spot within MaxFallHeight.
    /// </summary>
    public sealed class MoveDescend : IMove
    {
        public MoveType Type => MoveType.Descend;
        public int XOffset { get; }
        public int ZOffset { get; }
        public bool DynamicY => true;

        public MoveDescend(int xOffset, int zOffset)
        {
            XOffset = xOffset;
            ZOffset = zOffset;
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
        {
            int destX = x + XOffset;
            int destZ = z + ZOffset;

            if (!ctx.CanWalkThrough(destX, y, destZ) || !ctx.CanWalkThrough(destX, y + 1, destZ))
            {
                result.SetImpossible();
                return;
            }

            for (int fallDist = 1; fallDist <= ctx.MaxFallHeight; fallDist++)
            {
                int landY = y - fallDist;

                if (ctx.CanWalkOn(destX, landY - 1, destZ))
                {
                    if (!ctx.CanWalkThrough(destX, landY, destZ))
                    {
                        result.SetImpossible();
                        return;
                    }

                    double cost = ActionCosts.WalkOffBlock + ActionCosts.FallCost(fallDist);
                    if (MoveHelper.IsHazardous(ctx.GetMaterial(destX, landY - 1, destZ)))
                    {
                        result.SetImpossible();
                        return;
                    }

                    result.Set(destX, landY, destZ, cost);
                    return;
                }

                if (!ctx.CanWalkThrough(destX, landY, destZ))
                {
                    result.SetImpossible();
                    return;
                }
            }

            result.SetImpossible();
        }
    }
}
