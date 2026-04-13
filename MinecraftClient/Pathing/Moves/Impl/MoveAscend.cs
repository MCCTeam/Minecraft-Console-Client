using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Jump up 1 block in a cardinal direction.
    /// Requires: headroom at (x, y+2, z), body space at dest (y+1, y+2), ground at dest (y).
    /// </summary>
    public sealed class MoveAscend : IMove
    {
        public MoveType Type => MoveType.Ascend;
        public int XOffset { get; }
        public int ZOffset { get; }
        public bool DynamicY => false;

        public MoveAscend(int xOffset, int zOffset)
        {
            XOffset = xOffset;
            ZOffset = zOffset;
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
        {
            int destX = x + XOffset;
            int destZ = z + ZOffset;
            int destY = y + 1;

            if (!ctx.CanWalkThrough(x, y + 2, z))
            {
                result.SetImpossible();
                return;
            }

            if (!ctx.CanWalkThrough(destX, destY, destZ) || !ctx.CanWalkThrough(destX, destY + 1, destZ))
            {
                result.SetImpossible();
                return;
            }

            if (!ctx.CanWalkOn(destX, y, destZ))
            {
                result.SetImpossible();
                return;
            }

            double cost = ctx.SprintCost + ctx.JumpPenalty;
            result.Set(destX, destY, destZ, cost);
        }
    }
}
