using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Straight-down fall at the current X,Z position.
    /// Supports water landing and mid-fall ladder/vine grabbing.
    /// Used for drops where MoveDescend's 1-block horizontal offset doesn't apply.
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

            double costSoFar = 0;
            int effectiveStartHeight = y;

            for (int fallDist = 1; fallDist <= _maxScanDepth; fallDist++)
            {
                int landY = y - fallDist;
                if (landY < -64) break;

                Material ontoMat = ctx.GetMaterial(x, landY, z);
                int unprotectedFallHeight = fallDist - (y - effectiveStartHeight);

                // Water landing: safe regardless of height
                if (MoveHelper.IsWater(ontoMat))
                {
                    double waterCost = ActionCosts.FallCost(unprotectedFallHeight) + costSoFar;
                    result.Set(x, landY, z, waterCost);
                    return;
                }

                // Mid-fall ladder/vine grab (resets effective fall height)
                if (ctx.AllowLadderGrabDuringFall && unprotectedFallHeight <= 11
                    && ontoMat.CanBeClimbedOn())
                {
                    costSoFar += ActionCosts.FallCost(unprotectedFallHeight - 1);
                    costSoFar += ActionCosts.LadderDownOne;
                    effectiveStartHeight = landY;
                    continue;
                }

                if (ctx.CanWalkThrough(x, landY, z))
                    continue;

                // Hit something solid
                if (!ctx.CanWalkOn(x, landY, z))
                {
                    result.SetImpossible();
                    return;
                }

                if (MoveHelper.IsHazardous(ontoMat))
                {
                    result.SetImpossible();
                    return;
                }

                // Solid landing within safe height
                if (unprotectedFallHeight <= ctx.MaxFallHeight + 1)
                {
                    double cost = ActionCosts.FallCost(unprotectedFallHeight) + costSoFar;
                    result.Set(x, landY + 1, z, cost);
                    return;
                }

                // Too high for safe landing
                result.SetImpossible();
                return;
            }

            result.SetImpossible();
        }
    }
}
