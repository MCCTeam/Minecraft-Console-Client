using System;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves;

internal static class ParkourFeasibility
{
    public static bool IsSidewallProfile(int xOffset, int zOffset, int yDelta)
    {
        int absX = Math.Abs(xOffset);
        int absZ = Math.Abs(zOffset);
        int major = Math.Max(absX, absZ);
        int minor = Math.Min(absX, absZ);

        return minor == 1
            && major >= 2
            && major <= 5
            && yDelta is >= -2 and <= 1;
    }

    public static void GetSidewallAxes(int xOffset, int zOffset, out int forwardX, out int forwardZ, out int lateralX, out int lateralZ)
    {
        if (Math.Abs(xOffset) > Math.Abs(zOffset))
        {
            forwardX = Math.Sign(xOffset);
            forwardZ = 0;
            lateralX = 0;
            lateralZ = Math.Sign(zOffset);
        }
        else
        {
            forwardX = 0;
            forwardZ = Math.Sign(zOffset);
            lateralX = Math.Sign(xOffset);
            lateralZ = 0;
        }
    }

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
        bool carriedEntry = ctx.PreviousMoveType is MoveType.Parkour or MoveType.Descend;

        // Sprint-momentum carry semantics for flat (yDelta == 0) parkour:
        //   * Parkour-carry: the previous move's airborne sprint is preserved
        //     cleanly through landing, so a chained 5 c2c flat parkour can
        //     fire immediately from the takeoff edge.
        //   * Descend-carry: the previous Descend often overshoots the
        //     takeoff block by ~0.5 m (the bot lands inside the takeoff
        //     block but already past the leading edge), eating the runway
        //     the SprintJumpTemplate needs to spin sprint back up. In
        //     practice this lets the bot launch with sub-12-tick momentum
        //     and short-fall the 5 c2c gap by ~0.7 m. Treat Descend-carry
        //     as a cold start for flat run-up so the planner inserts an
        //     explicit traverse runway or picks a shorter parkour.
        bool parkourCarry = ctx.PreviousMoveType == MoveType.Parkour;
        double threshold = yDelta switch
        {
            > 0 when carriedEntry => 4.5,
            > 0 => 2.5,
            < 0 when carriedEntry => 5.5,
            < 0 => 3.5,
            _ when parkourCarry => 5.5,
            _ => 3.5,
        };
        if (horiz < threshold)
            return true;

        if (carriedEntry && yDelta < 0)
            return true;

        int xSign = Math.Sign(xOffset);
        int zSign = Math.Sign(zOffset);

        // Long flat sprint parkour (5 c2c, horiz~5) requires the player to be
        // launched at full vanilla sprint velocity (~12 momentum ticks). A
        // standing-jump cold start only reaches gap=3 (=4 c2c). Without a
        // clean Parkour-carry, one back block of runway is not enough to
        // spin sprint up; demand at least two aligned back blocks so the
        // executor has a real run-up window.
        // tools/sim_jump_reach.py "Standing sprint jump (0t momentum)" matrix
        // shows gap=4 dy=0 is unreachable, while 12t-momentum gap=4 reaches
        // 5.1075 m.
        int requiredBackBlocks = (yDelta == 0 && !parkourCarry && horiz >= 4.5) ? 2 : 1;

        for (int i = 1; i <= requiredBackBlocks; i++)
        {
            int backX = x - xSign * i;
            int backZ = z - zSign * i;
            if (!ctx.CanWalkOn(backX, y - 1, backZ))
                return false;
            if (!IsColumnPassable(ctx, backX, y, backZ))
                return false;
        }

        return true;
    }

    public static bool TryGetRequiredStaticEntryRunupSteps(
        MoveType previousMoveType,
        int xOffset,
        int zOffset,
        int yDelta,
        out int requiredSteps)
    {
        requiredSteps = 0;

        if (previousMoveType is MoveType.Parkour or MoveType.Descend)
            return false;

        int major = Math.Max(Math.Abs(xOffset), Math.Abs(zOffset));
        if (yDelta == -1 && major == 5)
        {
            requiredSteps = 1;
            return true;
        }

        return false;
    }

    public static bool HasPreparedRunup(
        EntryPreparationState state,
        int x,
        int y,
        int z,
        int forwardX,
        int forwardZ,
        int requiredSteps)
    {
        return state.Kind == EntryPreparationKind.SidewallRunup
            && state.IsPrepared
            && state.OriginX == x
            && state.OriginY == y
            && state.OriginZ == z
            && state.ForwardX == forwardX
            && state.ForwardZ == forwardZ
            && state.RequiredSteps == requiredSteps;
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

    public static bool HasIntermediateLandingConflict(
        CalculationContext ctx,
        int x,
        int y,
        int z,
        int xOffset,
        int zOffset,
        int yDelta)
    {
        if (yDelta >= 0)
            return false;

        bool cardinal = (xOffset == 0) != (zOffset == 0);
        int distance = Math.Max(Math.Abs(xOffset), Math.Abs(zOffset));
        if (!cardinal || distance < 6)
            return false;

        int destY = y + yDelta;
        int xSign = Math.Sign(xOffset);
        int zSign = Math.Sign(zOffset);

        for (int step = 1; step < distance; step++)
        {
            int gx = x + (xOffset != 0 ? xSign * step : 0);
            int gz = z + (zOffset != 0 ? zSign * step : 0);

            for (int candidateY = y - 1; candidateY >= destY; candidateY--)
            {
                if (ctx.CanWalkOn(gx, candidateY - 1, gz) && IsColumnPassable(ctx, gx, candidateY, gz))
                    return true;
            }
        }

        return false;
    }

    public static bool HasDominantAxisRunUp(
        CalculationContext ctx,
        int x,
        int y,
        int z,
        int forwardX,
        int forwardZ,
        int xOffset,
        int zOffset,
        int yDelta)
    {
        int major = Math.Max(Math.Abs(xOffset), Math.Abs(zOffset));
        int maxMajor = yDelta switch
        {
            > 0 => 3,
            < 0 => 5,
            _ => 4,
        };

        if (major > maxMajor)
            return false;

        bool carriedEntry = ctx.PreviousMoveType is MoveType.Parkour or MoveType.Descend;
        if (carriedEntry)
            return true;

        // Cold-start sprint-jump reaches ~3.1-3.5 blocks horizontally without
        // any pre-existing momentum, so short sidewall jumps remain feasible
        // from a lone overhang block even when no 2-block runway is available
        // behind the start (matches the staircase/step-pyramid cases seen in
        // the wild, and Baritone's MomentumBehavior.ALLOWED contract).
        double horiz = Math.Sqrt((xOffset * xOffset) + (zOffset * zOffset));
        double coldStartReach = yDelta switch
        {
            > 0 => 2.5,
            < 0 => 3.3,
            _ => 3.2,
        };
        if (horiz <= coldStartReach)
            return true;

        for (int i = 1; i <= 2; i++)
        {
            int rx = x - (forwardX * i);
            int rz = z - (forwardZ * i);
            if (!ctx.CanWalkOn(rx, y - 1, rz) || !IsColumnPassable(ctx, rx, y, rz))
                return false;
        }

        return true;
    }

    public static bool HasSidewallArcClearance(
        CalculationContext ctx,
        int x,
        int y,
        int z,
        int forwardX,
        int forwardZ,
        int lateralX,
        int lateralZ,
        int xOffset,
        int zOffset,
        int yDelta)
    {
        int major = Math.Max(Math.Abs(xOffset), Math.Abs(zOffset));
        int insideWallDepth = 0;

        // Probe up to MaxProbeDepth cells along the forward axis at the lateral
        // column to measure how thick the inner wall is. A 1- or 2-thick wall
        // was the original supported case; thicker walls (3) still let the
        // sidewall arc play out because the wall only provides lateral support
        // during the sprint-jump — the player brushes the wall longer but the
        // forward reach is unchanged. Walls thicker than MaxProbeDepth are
        // rejected because they either bury the landing column or leave no
        // open air for the arc to complete.
        const int MaxProbeDepth = 3;
        for (int step = 0; step < MaxProbeDepth; step++)
        {
            int wx = x + lateralX + (forwardX * step);
            int wz = z + lateralZ + (forwardZ * step);
            if (ctx.CanWalkThrough(wx, y, wz) && ctx.CanWalkThrough(wx, y + 1, wz))
                break;
            insideWallDepth++;
        }

        if (insideWallDepth is < 1 or > MaxProbeDepth)
            return false;

        for (int step = 1; step <= major; step++)
        {
            int cx = x + (forwardX * step);
            int cz = z + (forwardZ * step);
            if (!IsColumnPassable(ctx, cx, y, cz))
                return false;
        }

        int outsideX = x - lateralX;
        int outsideZ = z - lateralZ;
        return IsColumnPassable(ctx, outsideX, y, outsideZ);
    }

    public static bool HasSidewallLandingClearance(
        CalculationContext ctx,
        int destX,
        int destY,
        int destZ,
        int forwardX,
        int forwardZ,
        int lateralX,
        int lateralZ)
    {
        if (!ctx.CanWalkOn(destX, destY - 1, destZ))
            return false;

        if (!IsColumnPassable(ctx, destX, destY, destZ))
            return false;

        if (!IsColumnPassable(ctx, destX + forwardX, destY, destZ + forwardZ))
            return false;

        if (!IsColumnPassable(ctx, destX - lateralX, destY, destZ - lateralZ))
            return false;

        return true;
    }

    private static bool IsColumnPassable(CalculationContext ctx, int x, int y, int z)
    {
        return ctx.CanWalkThrough(x, y, z)
            && ctx.CanWalkThrough(x, y + 1, z);
    }
}
