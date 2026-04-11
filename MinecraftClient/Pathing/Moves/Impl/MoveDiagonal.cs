using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Diagonal walk (1 block in both X and Z, same Y).
    /// Allows corner walks: if one intermediate cardinal is blocked by a wall
    /// but the other is clear, the player can hug the open side to cut the
    /// corner. Both sides blocked is impossible (player AABB too wide).
    /// </summary>
    public sealed class MoveDiagonal : IMove
    {
        public MoveType Type => MoveType.Diagonal;
        public int XOffset { get; }
        public int ZOffset { get; }
        public bool DynamicY => false;

        public MoveDiagonal(int xOffset, int zOffset)
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

            if (!ctx.CanWalkOn(destX, y - 1, destZ))
            {
                result.SetImpossible();
                return;
            }

            bool sideX = ctx.CanWalkThrough(x + XOffset, y, z) &&
                         ctx.CanWalkThrough(x + XOffset, y + 1, z);
            bool sideZ = ctx.CanWalkThrough(x, y, z + ZOffset) &&
                         ctx.CanWalkThrough(x, y + 1, z + ZOffset);

            if (!sideX && !sideZ)
            {
                result.SetImpossible();
                return;
            }

            double cost = ctx.SprintCost * ActionCosts.DiagonalMultiplier;
            if (!sideX || !sideZ)
                cost = ctx.WalkCost * ActionCosts.DiagonalMultiplier;

            result.Set(destX, y, destZ, cost);
        }
    }
}
