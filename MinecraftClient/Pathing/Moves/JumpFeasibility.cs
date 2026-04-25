using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Moves;

/// <summary>
/// Single source of truth for jump-family feasibility and cost. Each
/// <see cref="JumpFlavor"/> selects one of the Evaluate* methods; the methods
/// share low-level primitives (head clearance, destination clearance,
/// flight-path sweep, run-up, cost) so that a physics rule is implemented
/// exactly once.
/// </summary>
internal static class JumpFeasibility
{
    public static void Evaluate(
        CalculationContext ctx,
        int x, int y, int z,
        JumpDescriptor desc,
        ref MoveResult result)
    {
        switch (desc.Flavor)
        {
            case JumpFlavor.Walk:
                EvaluateWalk(ctx, x, y, z, desc, ref result);
                return;
            case JumpFlavor.Step:
                EvaluateStep(ctx, x, y, z, desc, ref result);
                return;
            case JumpFlavor.SprintJump:
                EvaluateSprintJump(ctx, x, y, z, desc, ref result);
                return;
            case JumpFlavor.Sidewall:
                EvaluateSidewall(ctx, x, y, z, desc, ref result);
                return;
            default:
                result.SetImpossible();
                return;
        }
    }

    // ---------------------------------------------------------------------
    //   Walk (dy = 0, single block, cardinal or diagonal)
    // ---------------------------------------------------------------------

    private static void EvaluateWalk(
        CalculationContext ctx,
        int x, int y, int z,
        JumpDescriptor desc,
        ref MoveResult result)
    {
        int dx = desc.XOffset;
        int dz = desc.ZOffset;
        int destX = x + dx;
        int destZ = z + dz;

        if (!ctx.CanWalkThrough(destX, y, destZ) || !ctx.CanWalkThrough(destX, y + 1, destZ))
        {
            result.SetImpossible();
            return;
        }

        if (!ctx.CanWalkOn(destX, y - 1, destZ))
        {
            result.SetImpossible();
            return;
        }

        if (desc.IsCardinal)
        {
            double cost = ctx.SprintCost;
            Material destFloor = ctx.GetMaterial(destX, y - 1, destZ);
            if (destFloor == Material.SoulSand)
                cost *= 1.0 / PhysicsConsts.SoulSandSpeedFactor;
            result.Set(destX, y, destZ, cost);
            return;
        }

        // Diagonal corner walk: need at least one passable side cardinal.
        bool sideX = ctx.CanWalkThrough(x + dx, y, z) &&
                     ctx.CanWalkThrough(x + dx, y + 1, z);
        bool sideZ = ctx.CanWalkThrough(x, y, z + dz) &&
                     ctx.CanWalkThrough(x, y + 1, z + dz);

        if (!sideX && !sideZ)
        {
            result.SetImpossible();
            return;
        }

        double diagCost = ctx.SprintCost * ActionCosts.DiagonalMultiplier;
        if (!sideX || !sideZ)
            diagCost = ctx.WalkCost * ActionCosts.DiagonalMultiplier;

        result.Set(destX, y, destZ, diagCost);
    }

    // ---------------------------------------------------------------------
    //   Step (dy = +1 ascend, dy = -1 descend, cardinal or diagonal)
    // ---------------------------------------------------------------------

    private static void EvaluateStep(
        CalculationContext ctx,
        int x, int y, int z,
        JumpDescriptor desc,
        ref MoveResult result)
    {
        if (desc.YDelta == 1)
            EvaluateStepAscend(ctx, x, y, z, desc, ref result);
        else if (desc.YDelta == -1)
            EvaluateStepDescend(ctx, x, y, z, desc, ref result);
        else
            result.SetImpossible();
    }

    private static void EvaluateStepAscend(
        CalculationContext ctx,
        int x, int y, int z,
        JumpDescriptor desc,
        ref MoveResult result)
    {
        int dx = desc.XOffset;
        int dz = desc.ZOffset;
        int destX = x + dx;
        int destZ = z + dz;
        int destY = y + 1;

        if (!ctx.CanWalkThrough(x, y + 2, z))
        {
            result.SetImpossible();
            return;
        }

        if (!ctx.CanWalkOn(destX, y, destZ))
        {
            result.SetImpossible();
            return;
        }

        if (!ctx.CanWalkThrough(destX, destY, destZ) ||
            !ctx.CanWalkThrough(destX, destY + 1, destZ))
        {
            result.SetImpossible();
            return;
        }

        if (desc.IsCardinal)
        {
            double cost = ctx.SprintCost + ctx.JumpPenalty;
            result.Set(destX, destY, destZ, cost);
            return;
        }

        bool pathViaX = ctx.CanWalkThrough(x + dx, y, z) &&
                        ctx.CanWalkThrough(x + dx, y + 1, z) &&
                        ctx.CanWalkThrough(x + dx, y + 2, z);
        bool pathViaZ = ctx.CanWalkThrough(x, y, z + dz) &&
                        ctx.CanWalkThrough(x, y + 1, z + dz) &&
                        ctx.CanWalkThrough(x, y + 2, z + dz);

        if (!pathViaX && !pathViaZ)
        {
            result.SetImpossible();
            return;
        }

        // Baritone-parity gate (MovementDiagonal.cost @197-200): when either
        // cardinal shoulder also has solid ground below (i.e. the bot could
        // walk that way first and then do a plain cardinal Ascend), refuse
        // the diagonal Ascend.  Executing a diagonal Ascend requires the
        // bot's ground-speed momentum to already point along the diagonal at
        // the moment of takeoff; when the preceding segment is a cardinal
        // Walk the momentum is axis-aligned and the 2-tick yaw/input rotation
        // during the handoff cannot redirect enough horizontal motion, so the
        // bot consistently overshoots the target block.  Forcing A* to spend
        // the extra ~0.4 cost of a cardinal Walk + cardinal Ascend pair
        // eliminates that execution failure while still leaving true "only
        // reachable diagonally" setups (no cardinal floor support) on the
        // table for scenarios that explicitly test the diagonal Step graph.
        bool cardinalWalkableViaX = pathViaX && ctx.CanWalkOn(x + dx, y - 1, z);
        bool cardinalWalkableViaZ = pathViaZ && ctx.CanWalkOn(x, y - 1, z + dz);
        if (cardinalWalkableViaX || cardinalWalkableViaZ)
        {
            result.SetImpossible();
            return;
        }

        double diagCost = ctx.SprintCost * ActionCosts.DiagonalMultiplier + ctx.JumpPenalty;
        result.Set(destX, destY, destZ, diagCost);
    }

    private static void EvaluateStepDescend(
        CalculationContext ctx,
        int x, int y, int z,
        JumpDescriptor desc,
        ref MoveResult result)
    {
        int dx = desc.XOffset;
        int dz = desc.ZOffset;
        int destX = x + dx;
        int destZ = z + dz;
        int destY = y - 1;

        if (!ctx.CanWalkOn(destX, destY - 1, destZ))
        {
            result.SetImpossible();
            return;
        }

        if (!ctx.CanWalkThrough(destX, destY, destZ) ||
            !ctx.CanWalkThrough(destX, destY + 1, destZ))
        {
            result.SetImpossible();
            return;
        }

        Material landOn = ctx.GetMaterial(destX, destY - 1, destZ);
        if (MoveHelper.IsHazardous(landOn))
        {
            result.SetImpossible();
            return;
        }

        Material fromDown = ctx.GetMaterial(x, y - 1, z);
        if (fromDown.CanBeClimbedOn())
        {
            result.SetImpossible();
            return;
        }

        if (!desc.IsDiagonal)
        {
            // Currently only diagonal descend steps exist; cardinal descend is
            // served by MoveDescend which supports dynamic fall depth.
            result.SetImpossible();
            return;
        }

        bool pathViaX = ctx.CanWalkThrough(x + dx, y, z) &&
                        ctx.CanWalkThrough(x + dx, y + 1, z);
        bool pathViaZ = ctx.CanWalkThrough(x, y, z + dz) &&
                        ctx.CanWalkThrough(x, y + 1, z + dz);

        // A diagonal Step descend forces the player off the corner of the
        // current standing block. Vanilla axis-separated collision resolves
        // -X and -Z movement independently: when ONE cardinal shoulder is
        // blocked by a wall, the matching velocity component is zeroed and
        // the bot slides along the open axis only. If the open-axis column
        // (e.g. (x, y-1, z+dz) when only pathViaZ is clear) lacks a floor,
        // the bot falls straight down past the intended landing block at
        // (x+dx, y-2, z+dz) into whatever solid surface lies further below
        // — exactly the multi-block fall-through observed on the 251→244
        // route around (249,136,207). Require BOTH shoulder columns to be
        // passable so the bot can actually clear the corner diagonally.
        if (!pathViaX || !pathViaZ)
        {
            result.SetImpossible();
            return;
        }

        double cost = ActionCosts.WalkOffBlock * ActionCosts.DiagonalMultiplier
                    + ActionCosts.FallCost(1);
        result.Set(destX, destY, destZ, cost);
    }

    // ---------------------------------------------------------------------
    //   SprintJump (parkour, horiz >= 2, dy in -2..+1)
    //   Ported 1:1 from MoveParkour.Calculate.
    // ---------------------------------------------------------------------

    private static void EvaluateSprintJump(
        CalculationContext ctx,
        int x, int y, int z,
        JumpDescriptor desc,
        ref MoveResult result)
    {
        int xOffset = desc.XOffset;
        int zOffset = desc.ZOffset;
        int yDelta = desc.YDelta;

        if (!ctx.AllowParkour)
        {
            result.SetImpossible();
            return;
        }

        if (yDelta > 0 && !ctx.AllowParkourAscend)
        {
            result.SetImpossible();
            return;
        }

        if (yDelta < 0 && -yDelta > ctx.MaxFallHeight)
        {
            result.SetImpossible();
            return;
        }

        if (!ctx.CanSprint)
        {
            result.SetImpossible();
            return;
        }

        bool cardinal = (xOffset == 0) != (zOffset == 0);
        if (cardinal)
        {
            int distance = Math.Max(Math.Abs(xOffset), Math.Abs(zOffset));
            int maxDistance = yDelta switch
            {
                > 0 => 3,
                < 0 => 5,
                _ => 5,
            };

            if (distance > maxDistance)
            {
                result.SetImpossible();
                return;
            }
        }

        Material standingOn = ctx.GetMaterial(x, y - 1, z);
        if (standingOn.CanBeClimbedOn())
        {
            result.SetImpossible();
            return;
        }

        if (!ParkourFeasibility.HasRunUp(ctx, x, y, z, xOffset, zOffset, yDelta))
        {
            result.SetImpossible();
            return;
        }

        int destX = x + xOffset;
        int destZ = z + zOffset;
        int destY = y + yDelta;

        if (!ctx.CanWalkThrough(x, y + 2, z))
        {
            result.SetImpossible();
            return;
        }

        Material atFeet = ctx.GetMaterial(x, y, z);
        if (atFeet.IsLiquid())
        {
            result.SetImpossible();
            return;
        }

        if (!ctx.CanWalkOn(destX, destY - 1, destZ))
        {
            result.SetImpossible();
            return;
        }

        if (!ctx.CanWalkThrough(destX, destY, destZ) ||
            !ctx.CanWalkThrough(destX, destY + 1, destZ))
        {
            result.SetImpossible();
            return;
        }

        if (ParkourFeasibility.HasIntermediateLandingConflict(ctx, x, y, z, xOffset, zOffset, yDelta))
        {
            result.SetImpossible();
            return;
        }

        int xSign = Math.Sign(xOffset);
        int zSign = Math.Sign(zOffset);
        int xAbs = Math.Abs(xOffset);
        int zAbs = Math.Abs(zOffset);

        if (!CheckSprintJumpFlightPath(ctx, x, y, z, xSign, zSign, xAbs, zAbs, yDelta))
        {
            result.SetImpossible();
            return;
        }

        // Gap check: first block(s) adjacent to start must lack ground so A*
        // cannot take a cheaper walking path.
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
        else if (ctx.CanWalkOn(x + xSign, y - 1, z + zSign))
        {
            result.SetImpossible();
            return;
        }

        if (!ParkourFeasibility.HasDiagonalShoulderClearance(ctx, x, y, z, xOffset, zOffset))
        {
            result.SetImpossible();
            return;
        }

        if (!ParkourFeasibility.HasCardinalSideClearance(ctx, x, y, z, xOffset, zOffset))
        {
            result.SetImpossible();
            return;
        }

        if (!ParkourFeasibility.HasLandingOvershootClearance(ctx, destX, destY, destZ, xSign, zSign))
        {
            result.SetImpossible();
            return;
        }

        double horizDist = Math.Sqrt((double)((xOffset * xOffset) + (zOffset * zOffset)));
        double cost;
        if (yDelta > 0)
            cost = horizDist * ctx.SprintCost + ctx.JumpPenalty * 2;
        else if (yDelta < 0)
            cost = horizDist * ctx.SprintCost + ctx.JumpPenalty
                 + ActionCosts.FallCost(-yDelta);
        else if (horizDist >= 3.5)
            cost = horizDist * ctx.SprintCost + ctx.JumpPenalty;
        else
            cost = horizDist * ctx.WalkCost + ctx.JumpPenalty;

        result.Set(destX, destY, destZ, cost, ParkourProfile.Default);
    }

    private static bool CheckSprintJumpFlightPath(
        CalculationContext ctx,
        int x, int y, int z,
        int xSign, int zSign, int xAbs, int zAbs,
        int yDelta)
    {
        if (xAbs == 0 || zAbs == 0)
        {
            for (int step = 1; step < Math.Max(xAbs, zAbs); step++)
            {
                int gx = x + xSign * (xAbs > 0 ? step : 0);
                int gz = z + zSign * (zAbs > 0 ? step : 0);
                if (!ClearSprintJumpColumn(ctx, gx, y, gz, yDelta))
                    return false;
            }
            return true;
        }

        int maxSteps = Math.Max(xAbs, zAbs);
        for (int step = 1; step < maxSteps; step++)
        {
            double fx = (double)step * xAbs / maxSteps;
            double fz = (double)step * zAbs / maxSteps;

            int ix = (int)Math.Round(fx);
            int iz = (int)Math.Round(fz);

            int gx = x + xSign * ix;
            int gz = z + zSign * iz;

            if (!ClearSprintJumpColumn(ctx, gx, y, gz, yDelta))
                return false;

            if (xAbs != zAbs)
            {
                double fracX = fx - Math.Floor(fx);
                double fracZ = fz - Math.Floor(fz);
                if (fracX > 0.2 && fracX < 0.8 && ix > 0 && ix < xAbs)
                {
                    if (!ClearSprintJumpColumn(ctx, x + xSign * (ix - 1), y, gz, yDelta))
                        return false;
                }
                if (fracZ > 0.2 && fracZ < 0.8 && iz > 0 && iz < zAbs)
                {
                    if (!ClearSprintJumpColumn(ctx, gx, y, z + zSign * (iz - 1), yDelta))
                        return false;
                }
            }
        }

        return true;
    }

    private static bool ClearSprintJumpColumn(CalculationContext ctx, int gx, int y, int gz, int yDelta)
    {
        if (!ctx.CanWalkThrough(gx, y, gz) ||
            !ctx.CanWalkThrough(gx, y + 1, gz) ||
            !ctx.CanWalkThrough(gx, y + 2, gz))
            return false;
        if (yDelta > 0 && !ctx.CanWalkThrough(gx, y + 3, gz))
            return false;
        return true;
    }

    // ---------------------------------------------------------------------
    //   Sidewall (dominant-axis sprint jump with an inner-wall constraint).
    //   Ported 1:1 from MoveSidewallParkour.Calculate.
    // ---------------------------------------------------------------------

    private static void EvaluateSidewall(
        CalculationContext ctx,
        int x, int y, int z,
        JumpDescriptor desc,
        ref MoveResult result)
    {
        int xOffset = desc.XOffset;
        int zOffset = desc.ZOffset;
        int yDelta = desc.YDelta;

        if (!ctx.AllowParkour || !ctx.CanSprint)
        {
            result.SetImpossible();
            return;
        }

        if (yDelta > 0 && !ctx.AllowParkourAscend)
        {
            result.SetImpossible();
            return;
        }

        if (yDelta < 0 && -yDelta > ctx.MaxFallHeight)
        {
            result.SetImpossible();
            return;
        }

        if (!ParkourFeasibility.IsSidewallProfile(xOffset, zOffset, yDelta))
        {
            result.SetImpossible();
            return;
        }

        Material standingOn = ctx.GetMaterial(x, y - 1, z);
        if (standingOn.CanBeClimbedOn())
        {
            result.SetImpossible();
            return;
        }

        Material atFeet = ctx.GetMaterial(x, y, z);
        if (atFeet.IsLiquid())
        {
            result.SetImpossible();
            return;
        }

        ParkourFeasibility.GetSidewallAxes(xOffset, zOffset, out int forwardX, out int forwardZ, out int lateralX, out int lateralZ);

        int destX = x + xOffset;
        int destY = y + yDelta;
        int destZ = z + zOffset;

        if (!ctx.CanWalkThrough(x, y + 2, z))
        {
            result.SetImpossible();
            return;
        }

        if (ParkourFeasibility.TryGetRequiredStaticEntryRunupSteps(ctx.PreviousMoveType, xOffset, zOffset, yDelta, out int requiredSteps))
        {
            if (!ParkourFeasibility.HasPreparedRunup(ctx.CurrentEntryPreparation, x, y, z, forwardX, forwardZ, requiredSteps))
            {
                result.SetImpossible();
                return;
            }
        }
        else if (!ParkourFeasibility.HasDominantAxisRunUp(ctx, x, y, z, forwardX, forwardZ, xOffset, zOffset, yDelta))
        {
            result.SetImpossible();
            return;
        }

        if (!ParkourFeasibility.HasSidewallArcClearance(ctx, x, y, z, forwardX, forwardZ, lateralX, lateralZ, xOffset, zOffset, yDelta))
        {
            result.SetImpossible();
            return;
        }

        if (!ParkourFeasibility.HasSidewallLandingClearance(ctx, destX, destY, destZ, forwardX, forwardZ, lateralX, lateralZ))
        {
            result.SetImpossible();
            return;
        }

        double horizDist = Math.Sqrt((double)((xOffset * xOffset) + (zOffset * zOffset)));
        double cost = yDelta switch
        {
            > 0 => horizDist * ctx.SprintCost + ctx.JumpPenalty * 2,
            < 0 => horizDist * ctx.SprintCost + ctx.JumpPenalty + ActionCosts.FallCost(-yDelta),
            _ => horizDist * ctx.SprintCost + ctx.JumpPenalty,
        };

        result.Set(destX, destY, destZ, cost, ParkourProfile.Sidewall);
    }
}
