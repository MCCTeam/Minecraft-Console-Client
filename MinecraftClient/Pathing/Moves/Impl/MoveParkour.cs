using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Moves;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Sprint jump across a gap in cardinal or diagonal direction.
    /// Supports horizontal distances of 2-4 blocks, optional +1Y ascent,
    /// and -1/-2Y descent (land on a lower platform after the jump).
    /// Based on Baritone's MovementParkour design with diagonal extensions.
    /// </summary>
    public sealed class MoveParkour : IMove
    {
        public MoveType Type => MoveType.Parkour;
        public int XOffset { get; }
        public int ZOffset { get; }
        public bool DynamicY => false;

        private readonly int _yDelta;

        /// <summary>
        /// Create a parkour move with direct XZ offsets.
        /// For cardinal: one of xOff/zOff is 0, the other is 2..4.
        /// For diagonal: both non-zero, actual distance should be within sprint jump range.
        /// </summary>
        public MoveParkour(int xOff, int zOff, int yDelta = 0)
        {
            XOffset = xOff;
            ZOffset = zOff;
            _yDelta = yDelta;
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
        {
            if (!ctx.AllowParkour)
            {
                result.SetImpossible();
                return;
            }

            if (_yDelta > 0 && !ctx.AllowParkourAscend)
            {
                result.SetImpossible();
                return;
            }

            if (_yDelta < 0 && -_yDelta > ctx.MaxFallHeight)
            {
                result.SetImpossible();
                return;
            }

            if (!ctx.CanSprint)
            {
                result.SetImpossible();
                return;
            }

            bool cardinal = (XOffset == 0) != (ZOffset == 0);
            if (cardinal)
            {
                int distance = Math.Max(Math.Abs(XOffset), Math.Abs(ZOffset));
                int maxDistance = _yDelta switch
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

            // Don't parkour from climbable blocks (unreliable jump)
            Material standingOn = ctx.GetMaterial(x, y - 1, z);
            if (standingOn.CanBeClimbedOn())
            {
                result.SetImpossible();
                return;
            }

            if (!ParkourFeasibility.HasRunUp(ctx, x, y, z, XOffset, ZOffset, _yDelta))
            {
                result.SetImpossible();
                return;
            }

            int destX = x + XOffset;
            int destZ = z + ZOffset;
            int destY = y + _yDelta;

            // Head clearance at start (need room to jump)
            if (!ctx.CanWalkThrough(x, y + 2, z))
            {
                result.SetImpossible();
                return;
            }

            // Can't jump out of liquid
            Material atFeet = ctx.GetMaterial(x, y, z);
            if (atFeet.IsLiquid())
            {
                result.SetImpossible();
                return;
            }

            // Destination must be standable and passable
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

            if (ParkourFeasibility.HasIntermediateLandingConflict(ctx, x, y, z, XOffset, ZOffset, _yDelta))
            {
                result.SetImpossible();
                return;
            }

            int xSign = Math.Sign(XOffset);
            int zSign = Math.Sign(ZOffset);
            int xAbs = Math.Abs(XOffset);
            int zAbs = Math.Abs(ZOffset);

            // Check intermediate blocks along the flight path.
            // Cardinal: check all blocks in the column along the primary axis.
            // Diagonal: check blocks along the diagonal strip, not the full rectangle.
            // Player AABB is 0.6 wide, so only blocks near the diagonal line matter.
            if (!CheckFlightPath(ctx, x, y, z, xSign, zSign, xAbs, zAbs))
            {
                result.SetImpossible();
                return;
            }

            // Gap check: first block(s) adjacent to start must lack ground.
            // If ground exists there, A* can find a walking path instead.
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
                // Diagonal: the diagonally adjacent block must lack ground
                if (ctx.CanWalkOn(x + xSign, y - 1, z + zSign))
                {
                    result.SetImpossible();
                    return;
                }
            }

            if (!ParkourFeasibility.HasDiagonalShoulderClearance(ctx, x, y, z, XOffset, ZOffset))
            {
                result.SetImpossible();
                return;
            }

            if (!ParkourFeasibility.HasCardinalSideClearance(ctx, x, y, z, XOffset, ZOffset))
            {
                result.SetImpossible();
                return;
            }

            if (!ParkourFeasibility.HasLandingOvershootClearance(
                    ctx, destX, destY, destZ, xSign, zSign))
            {
                result.SetImpossible();
                return;
            }

            // Cost model following Baritone:
            // dist 2-3: walk speed * distance (jump is roughly time-neutral vs walking)
            // dist 4: sprint speed * distance (must sprint, covers ground faster)
            // ascend: always sprint speed (sprinting required)
            double horizDist = Math.Sqrt((double)(XOffset * XOffset + ZOffset * ZOffset));
            double cost;
            if (_yDelta > 0)
                cost = horizDist * ctx.SprintCost + ctx.JumpPenalty * 2;
            else if (_yDelta < 0)
                cost = horizDist * ctx.SprintCost + ctx.JumpPenalty
                     + ActionCosts.FallCost(-_yDelta);
            else if (horizDist >= 3.5)
                cost = horizDist * ctx.SprintCost + ctx.JumpPenalty;
            else
                cost = horizDist * ctx.WalkCost + ctx.JumpPenalty;

            result.Set(destX, destY, destZ, cost, ParkourProfile.Default);
        }

        /// <summary>
        /// Check body clearance along the flight path from start toward the destination.
        /// For cardinal moves, checks a straight line. For diagonal moves, checks
        /// only blocks near the actual diagonal trajectory rather than the full bounding
        /// rectangle, allowing jumps that pass a wall on one side.
        /// </summary>
        private bool CheckFlightPath(
            CalculationContext ctx, int x, int y, int z,
            int xSign, int zSign, int xAbs, int zAbs)
        {
            if (xAbs == 0 || zAbs == 0)
            {
                // Cardinal: single axis, check each block along the line
                for (int step = 1; step < Math.Max(xAbs, zAbs); step++)
                {
                    int gx = x + xSign * (xAbs > 0 ? step : 0);
                    int gz = z + zSign * (zAbs > 0 ? step : 0);
                    if (!ClearColumn(ctx, gx, y, gz))
                        return false;
                }
                return true;
            }

            // Diagonal: walk the diagonal and check each block the AABB touches.
            // At each step t along the diagonal, the player center is near
            // (x + t*xSign, z + t*zSign). The AABB extends 0.3 blocks each side,
            // so check the diagonal cell and one neighbor on each axis-aligned side
            // only when the trajectory is close to a cell boundary (always for short
            // diagonals). We enumerate cells by stepping through the longer axis
            // and computing the corresponding position on the shorter axis.
            int maxSteps = Math.Max(xAbs, zAbs);
            for (int step = 1; step < maxSteps; step++)
            {
                // Proportional position along each axis
                double fx = (double)step * xAbs / maxSteps;
                double fz = (double)step * zAbs / maxSteps;

                int ix = (int)Math.Round(fx);
                int iz = (int)Math.Round(fz);

                int gx = x + xSign * ix;
                int gz = z + zSign * iz;

                if (!ClearColumn(ctx, gx, y, gz))
                    return false;

                // Also check the neighboring cell across the shorter axis when close
                // to a cell boundary (player AABB overlaps adjacent cell)
                if (xAbs != zAbs)
                {
                    double fracX = fx - Math.Floor(fx);
                    double fracZ = fz - Math.Floor(fz);
                    if (fracX > 0.2 && fracX < 0.8 && ix > 0 && ix < xAbs)
                    {
                        if (!ClearColumn(ctx, x + xSign * (ix - 1), y, gz))
                            return false;
                    }
                    if (fracZ > 0.2 && fracZ < 0.8 && iz > 0 && iz < zAbs)
                    {
                        if (!ClearColumn(ctx, gx, y, z + zSign * (iz - 1)))
                            return false;
                    }
                }
            }

            return true;
        }

        private bool ClearColumn(CalculationContext ctx, int gx, int y, int gz)
        {
            if (!ctx.CanWalkThrough(gx, y, gz) ||
                !ctx.CanWalkThrough(gx, y + 1, gz) ||
                !ctx.CanWalkThrough(gx, y + 2, gz))
                return false;
            if (_yDelta > 0 && !ctx.CanWalkThrough(gx, y + 3, gz))
                return false;
            return true;
        }

        public override string ToString()
        {
            double dist = Math.Sqrt((double)(XOffset * XOffset + ZOffset * ZOffset));
            return $"MoveParkour(off=({XOffset},{ZOffset}), dy={_yDelta}, dist={dist:F1})";
        }
    }
}
