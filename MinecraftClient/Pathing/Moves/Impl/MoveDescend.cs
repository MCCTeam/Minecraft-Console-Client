using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Walk off a ledge and drop 1-N blocks in a cardinal direction.
    /// For short drops (1-MaxFallHeight), uses simple scan.
    /// For longer drops, delegates to DynamicFallCost which supports:
    /// - Water/liquid safe landing
    /// - Mid-fall ladder/vine grabbing (resets effective fall height if ≤ 11 blocks)
    /// Based on Baritone's MovementDescend.dynamicFallCost design.
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

            // Don't descend from ladder/vine (unreliable)
            Material fromDown = ctx.GetMaterial(x, y - 1, z);
            if (fromDown.CanBeClimbedOn())
            {
                result.SetImpossible();
                return;
            }

            // Check for simple 1-block descend first (most common case)
            if (ctx.CanWalkOn(destX, y - 2, destZ))
            {
                Material landOn = ctx.GetMaterial(destX, y - 2, destZ);
                if (MoveHelper.IsHazardous(landOn))
                {
                    result.SetImpossible();
                    return;
                }
                if (ctx.GetMaterial(destX, y - 1, destZ).CanBeClimbedOn())
                {
                    result.SetImpossible();
                    return;
                }

                double cost = ActionCosts.WalkOffBlock + ActionCosts.FallCost(1);
                result.Set(destX, y - 1, destZ, cost);
                return;
            }

            // Not a simple 1-block drop, try dynamic fall
            DynamicFallCost(ctx, x, y, z, destX, destZ, ref result);
        }

        /// <summary>
        /// Scan downward for a safe landing, supporting water, ladder grabs, and
        /// configurable max heights. Based on Baritone's dynamicFallCost.
        /// </summary>
        private static void DynamicFallCost(
            CalculationContext ctx, int x, int y, int z,
            int destX, int destZ, ref MoveResult result)
        {
            if (!ctx.CanWalkThrough(destX, y - 2, destZ))
            {
                result.SetImpossible();
                return;
            }

            double costSoFar = 0;
            int effectiveStartHeight = y;

            // Scan starts from fallHeight=3 (2 blocks below the ledge)
            // because fallHeight=1 and =2 were already checked above
            int maxScan = ctx.MaxFallHeightWater > ctx.MaxFallHeight
                ? ctx.MaxFallHeightWater
                : ctx.MaxFallHeight;

            for (int fallHeight = 3; fallHeight <= maxScan; fallHeight++)
            {
                int newY = y - fallHeight;
                if (newY < -64) break;

                Material ontoMat = ctx.GetMaterial(destX, newY, destZ);

                int unprotectedFallHeight = fallHeight - (y - effectiveStartHeight);
                double tentativeCost = ActionCosts.WalkOffBlock
                    + ActionCosts.FallCost(unprotectedFallHeight) + costSoFar;

                // Water landing: safe regardless of height (water absorbs all fall damage)
                if (MoveHelper.IsWater(ontoMat))
                {
                    result.Set(destX, newY, destZ, tentativeCost);
                    return;
                }

                // Mid-fall ladder/vine grab: resets effective fall height.
                // Vanilla: player grabs ladders/vines if falling speed is low enough
                // (roughly ≤ 11 blocks of unprotected free fall).
                if (ctx.AllowLadderGrabDuringFall && unprotectedFallHeight <= 11
                    && ontoMat.CanBeClimbedOn())
                {
                    costSoFar += ActionCosts.FallCost(unprotectedFallHeight - 1);
                    costSoFar += ActionCosts.LadderDownOne;
                    effectiveStartHeight = newY;
                    continue;
                }

                // Air or passable: continue falling
                if (ctx.CanWalkThrough(destX, newY, destZ))
                    continue;

                // Hit something solid
                if (MoveHelper.IsHazardous(ontoMat))
                {
                    result.SetImpossible();
                    return;
                }

                if (!ctx.CanWalkOn(destX, newY, destZ))
                {
                    result.SetImpossible();
                    return;
                }

                // Solid landing: allowed if within safe fall height
                if (unprotectedFallHeight <= ctx.MaxFallHeight + 1)
                {
                    result.Set(destX, newY + 1, destZ, tentativeCost);
                    return;
                }

                result.SetImpossible();
                return;
            }

            result.SetImpossible();
        }
    }
}
