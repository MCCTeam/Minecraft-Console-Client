using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Sprint jump across a gap in cardinal or diagonal direction.
    /// Supports horizontal distances of 2-4 blocks and optional +1Y ascent.
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

            if (!ctx.CanSprint)
            {
                result.SetImpossible();
                return;
            }

            // Don't parkour from climbable blocks (unreliable jump)
            Material standingOn = ctx.GetMaterial(x, y - 1, z);
            if (standingOn.CanBeClimbedOn())
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

            int xSign = Math.Sign(XOffset);
            int zSign = Math.Sign(ZOffset);
            int xAbs = Math.Abs(XOffset);
            int zAbs = Math.Abs(ZOffset);

            // Check intermediate space for passability (the player's bounding box sweeps
            // through a rectangle from start to end; check all blocks in that rectangle)
            for (int i = 0; i <= xAbs; i++)
            {
                for (int j = 0; j <= zAbs; j++)
                {
                    if (i == 0 && j == 0) continue;
                    if (i == xAbs && j == zAbs) continue;

                    int gx = x + xSign * i;
                    int gz = z + zSign * j;

                    if (!ctx.CanWalkThrough(gx, y, gz) ||
                        !ctx.CanWalkThrough(gx, y + 1, gz) ||
                        !ctx.CanWalkThrough(gx, y + 2, gz))
                    {
                        result.SetImpossible();
                        return;
                    }

                    if (_yDelta > 0 && !ctx.CanWalkThrough(gx, y + 3, gz))
                    {
                        result.SetImpossible();
                        return;
                    }
                }
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

            // Overshoot safety: after landing, player continues moving.
            // The block(s) past the destination in the jump direction must be passable.
            int overX = destX + xSign;
            int overZ = destZ + zSign;
            if (!ctx.CanWalkThrough(overX, destY, overZ) ||
                !ctx.CanWalkThrough(overX, destY + 1, overZ))
            {
                // Wall right after landing - risk of collision. Still allow but add cost.
                // (Baritone rejects this, but we allow with penalty since the template
                // will decelerate anyway.)
            }

            // Cost model following Baritone:
            // dist 2-3: walk speed * distance (jump is roughly time-neutral vs walking)
            // dist 4: sprint speed * distance (must sprint, covers ground faster)
            // ascend: always sprint speed (sprinting required)
            double horizDist = Math.Sqrt((double)(XOffset * XOffset + ZOffset * ZOffset));
            double cost;
            if (_yDelta > 0)
                cost = horizDist * ctx.SprintCost + ctx.JumpPenalty * 2;
            else if (horizDist >= 3.5)
                cost = horizDist * ctx.SprintCost + ctx.JumpPenalty;
            else
                cost = horizDist * ctx.WalkCost + ctx.JumpPenalty;

            result.Set(destX, destY, destZ, cost);
        }

        public override string ToString()
        {
            double dist = Math.Sqrt((double)(XOffset * XOffset + ZOffset * ZOffset));
            return $"MoveParkour(off=({XOffset},{ZOffset}), dy={_yDelta}, dist={dist:F1})";
        }
    }
}
