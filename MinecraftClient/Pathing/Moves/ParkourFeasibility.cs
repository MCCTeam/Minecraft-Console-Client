using System;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves;

internal static class ParkourFeasibility
{
    public static bool HasRunUp(
        CalculationContext ctx,
        int x,
        int y,
        int z,
        int xOffset,
        int zOffset,
        int yDelta)
    {
        double horiz = Math.Sqrt(xOffset * xOffset + zOffset * zOffset);
        double threshold = yDelta > 0 ? 2.5 : 3.5;
        if (horiz < threshold)
            return true;

        int backX = x - Math.Sign(xOffset);
        int backZ = z - Math.Sign(zOffset);
        if (!ctx.CanWalkOn(backX, y - 1, backZ))
            return false;
        return IsColumnPassable(ctx, backX, y, backZ);
    }

    public static bool HasDiagonalShoulderClearance(
        CalculationContext ctx,
        int x,
        int y,
        int z,
        int xOffset,
        int zOffset)
    {
        if (xOffset == 0 || zOffset == 0)
            return true;

        return IsColumnPassable(ctx, x + Math.Sign(xOffset), y, z)
            && IsColumnPassable(ctx, x, y, z + Math.Sign(zOffset));
    }

    public static bool HasLandingOvershootClearance(
        CalculationContext ctx,
        int destX,
        int destY,
        int destZ,
        int xSign,
        int zSign)
    {
        if (xSign == 0 && zSign == 0)
            return true;

        return IsColumnPassable(ctx, destX + xSign, destY, destZ + zSign);
    }

    public static bool HasCardinalSideClearance(
        CalculationContext ctx,
        int x,
        int y,
        int z,
        int xOffset,
        int zOffset)
    {
        if ((xOffset == 0) == (zOffset == 0))
            return true;

        if (xOffset != 0)
        {
            int xSign = Math.Sign(xOffset);
            for (int step = 1; step <= Math.Abs(xOffset); step++)
            {
                int gx = x + xSign * step;
                if (!IsColumnPassable(ctx, gx, y, z - 1)
                    || !IsColumnPassable(ctx, gx, y, z + 1))
                {
                    return false;
                }
            }

            return true;
        }

        int zSign = Math.Sign(zOffset);
        for (int step = 1; step <= Math.Abs(zOffset); step++)
        {
            int gz = z + zSign * step;
            if (!IsColumnPassable(ctx, x - 1, y, gz)
                || !IsColumnPassable(ctx, x + 1, y, gz))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsColumnPassable(CalculationContext ctx, int x, int y, int z)
    {
        return ctx.CanWalkThrough(x, y, z)
            && ctx.CanWalkThrough(x, y + 1, z);
    }
}
