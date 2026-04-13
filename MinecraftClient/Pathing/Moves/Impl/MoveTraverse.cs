using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Flat cardinal walk (1 block in +/-X or +/-Z, same Y).
    /// Checks body+head passable and ground below destination.
    /// </summary>
    public sealed class MoveTraverse : IMove
    {
        public MoveType Type => MoveType.Traverse;
        public int XOffset { get; }
        public int ZOffset { get; }
        public bool DynamicY => false;

        public MoveTraverse(int xOffset, int zOffset)
        {
            XOffset = xOffset;
            ZOffset = zOffset;
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
        {
            int destX = x + XOffset;
            int destZ = z + ZOffset;

            if (!ctx.CanWalkThrough(destX, y, destZ))
            {
                result.SetImpossible();
                return;
            }

            if (!ctx.CanWalkThrough(destX, y + 1, destZ))
            {
                result.SetImpossible();
                return;
            }

            if (!ctx.CanWalkOn(destX, y - 1, destZ))
            {
                result.SetImpossible();
                return;
            }

            double cost = ctx.SprintCost;

            var destFloorMat = ctx.GetMaterial(destX, y - 1, destZ);
            if (destFloorMat == Mapping.Material.SoulSand)
                cost *= 1.0 / Physics.PhysicsConsts.SoulSandSpeedFactor;

            result.Set(destX, y, destZ, cost);
        }
    }
}
