using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Sprint off a ledge and land 2 blocks away horizontally while dropping 1-3 blocks.
    /// At sprint speed (~5.6 blocks/s), falling 1-3 blocks gives enough airtime to
    /// cover 2 horizontal blocks without needing a jump.
    /// Supports cardinal (2,0)/(0,2) and diagonal (1,1) offsets.
    /// </summary>
    public sealed class MoveSprintDescend : IMove
    {
        public MoveType Type => MoveType.Descend;
        public int XOffset { get; }
        public int ZOffset { get; }
        public bool DynamicY => true;

        public MoveSprintDescend(int xOffset, int zOffset)
        {
            XOffset = xOffset;
            ZOffset = zOffset;
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
        {
            if (!ctx.CanSprint)
            {
                result.SetImpossible();
                return;
            }

            int destX = x + XOffset;
            int destZ = z + ZOffset;

            Material fromDown = ctx.GetMaterial(x, y - 1, z);
            if (fromDown.CanBeClimbedOn())
            {
                result.SetImpossible();
                return;
            }

            int xSign = Math.Sign(XOffset);
            int zSign = Math.Sign(ZOffset);
            int xAbs = Math.Abs(XOffset);
            int zAbs = Math.Abs(ZOffset);

            // The flight path sweeps through intermediate blocks at current Y.
            // Check body clearance for all intermediate and destination columns.
            for (int i = 0; i <= xAbs; i++)
            {
                for (int j = 0; j <= zAbs; j++)
                {
                    if (i == 0 && j == 0) continue;
                    int gx = x + xSign * i;
                    int gz = z + zSign * j;
                    if (!ctx.CanWalkThrough(gx, y, gz) || !ctx.CanWalkThrough(gx, y + 1, gz))
                    {
                        result.SetImpossible();
                        return;
                    }
                }
            }

            // The first step in the primary direction must lack ground (this IS a drop).
            if (xAbs > 0 && zAbs == 0)
            {
                if (ctx.CanWalkOn(x + xSign, y - 1, z))
                {
                    result.SetImpossible();
                    return;
                }
            }
            else if (xAbs == 0 && zAbs > 0)
            {
                if (ctx.CanWalkOn(x, y - 1, z + zSign))
                {
                    result.SetImpossible();
                    return;
                }
            }
            else
            {
                if (ctx.CanWalkOn(x + xSign, y - 1, z + zSign))
                {
                    result.SetImpossible();
                    return;
                }
            }

            // Scan downward from destination column for a landing spot.
            double horizDist = Math.Sqrt((double)(XOffset * XOffset + ZOffset * ZOffset));
            for (int drop = 1; drop <= ctx.MaxFallHeight; drop++)
            {
                int landY = y - drop - 1;
                if (landY < -64) break;

                if (!ctx.CanWalkOn(destX, landY, destZ))
                    continue;

                Material landMat = ctx.GetMaterial(destX, landY, destZ);
                if (MoveHelper.IsHazardous(landMat))
                {
                    result.SetImpossible();
                    return;
                }

                // Body space at landing
                if (!ctx.CanWalkThrough(destX, landY + 1, destZ) ||
                    !ctx.CanWalkThrough(destX, landY + 2, destZ))
                    continue;

                double cost = horizDist * ctx.SprintCost + ActionCosts.FallCost(drop);
                result.Set(destX, landY + 1, destZ, cost);
                return;
            }

            result.SetImpossible();
        }
    }
}
