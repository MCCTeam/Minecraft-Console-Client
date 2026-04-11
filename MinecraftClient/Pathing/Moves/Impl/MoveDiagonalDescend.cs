using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Walk diagonally (1 block in X and Z) and drop 1 block.
    /// Handles the "corner drop" pattern: step around a wall edge and land
    /// one block lower on a platform that is diagonally adjacent.
    /// </summary>
    public sealed class MoveDiagonalDescend : IMove
    {
        public MoveType Type => MoveType.Descend;
        public int XOffset { get; }
        public int ZOffset { get; }
        public bool DynamicY => false;

        public MoveDiagonalDescend(int xOffset, int zOffset)
        {
            XOffset = xOffset;
            ZOffset = zOffset;
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
        {
            int destX = x + XOffset;
            int destZ = z + ZOffset;
            int destY = y - 1;

            // Destination must have ground, body space, and head space
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

            // Don't descend from climbable blocks
            Material fromDown = ctx.GetMaterial(x, y - 1, z);
            if (fromDown.CanBeClimbedOn())
            {
                result.SetImpossible();
                return;
            }

            // At least one of the two intermediate cardinal directions must be passable
            // (player needs clearance to cut the corner).
            bool pathViaX = ctx.CanWalkThrough(x + XOffset, y, z) &&
                            ctx.CanWalkThrough(x + XOffset, y + 1, z);
            bool pathViaZ = ctx.CanWalkThrough(x, y, z + ZOffset) &&
                            ctx.CanWalkThrough(x, y + 1, z + ZOffset);

            if (!pathViaX && !pathViaZ)
            {
                result.SetImpossible();
                return;
            }

            double cost = ActionCosts.WalkOffBlock * ActionCosts.DiagonalMultiplier
                          + ActionCosts.FallCost(1);
            result.Set(destX, destY, destZ, cost);
        }
    }
}
