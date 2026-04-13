using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Jump diagonally (1 block in X and Z) and land 1 block higher.
    /// Handles the "corner jump" pattern: jump around a wall edge and land
    /// one block higher on a platform that is diagonally adjacent.
    /// </summary>
    public sealed class MoveDiagonalAscend : IMove
    {
        public MoveType Type => MoveType.Ascend;
        public int XOffset { get; }
        public int ZOffset { get; }
        public bool DynamicY => false;

        public MoveDiagonalAscend(int xOffset, int zOffset)
        {
            XOffset = xOffset;
            ZOffset = zOffset;
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
        {
            int destX = x + XOffset;
            int destZ = z + ZOffset;
            int destY = y + 1;

            // Need headroom to jump (y+2 at start)
            if (!ctx.CanWalkThrough(x, y + 2, z))
            {
                result.SetImpossible();
                return;
            }

            // Destination: solid ground, body passable, head passable
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

            // At least one of the two intermediate cardinal directions must be passable
            // at both the current and destination height (player sweeps through).
            bool pathViaX = ctx.CanWalkThrough(x + XOffset, y, z) &&
                            ctx.CanWalkThrough(x + XOffset, y + 1, z) &&
                            ctx.CanWalkThrough(x + XOffset, y + 2, z);
            bool pathViaZ = ctx.CanWalkThrough(x, y, z + ZOffset) &&
                            ctx.CanWalkThrough(x, y + 1, z + ZOffset) &&
                            ctx.CanWalkThrough(x, y + 2, z + ZOffset);

            if (!pathViaX && !pathViaZ)
            {
                result.SetImpossible();
                return;
            }

            double cost = ctx.SprintCost * ActionCosts.DiagonalMultiplier + ctx.JumpPenalty;
            result.Set(destX, destY, destZ, cost);
        }
    }
}
