using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Sprint jump across a gap of 1-3 blocks (total distance 2-4 blocks forward).
    /// Optionally ascends 1 block during the jump (distance 2 only).
    /// Requires AllowParkour in context; the first block forward must lack ground.
    /// </summary>
    public sealed class MoveParkour : IMove
    {
        public MoveType Type => MoveType.Parkour;
        public int XOffset { get; }
        public int ZOffset { get; }
        public bool DynamicY => false;

        private readonly int _distance;
        private readonly int _yDelta;
        private readonly int _xDir;
        private readonly int _zDir;

        public MoveParkour(int xDir, int zDir, int distance, int yDelta = 0)
        {
            _xDir = xDir;
            _zDir = zDir;
            _distance = distance;
            _yDelta = yDelta;
            XOffset = xDir * distance;
            ZOffset = zDir * distance;
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

            int destX = x + _xDir * _distance;
            int destZ = z + _zDir * _distance;
            int destY = y + _yDelta;

            if (!ctx.CanWalkThrough(x, y + 2, z))
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

            for (int i = 1; i < _distance; i++)
            {
                int gx = x + _xDir * i;
                int gz = z + _zDir * i;

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

            int firstGapX = x + _xDir;
            int firstGapZ = z + _zDir;
            if (ctx.CanWalkOn(firstGapX, y - 1, firstGapZ))
            {
                result.SetImpossible();
                return;
            }

            double cost = _distance * ctx.SprintCost + ctx.JumpPenalty;
            if (_yDelta > 0)
                cost += ctx.JumpPenalty;

            result.Set(destX, destY, destZ, cost);
        }

        public override string ToString() =>
            $"MoveParkour(dir=({_xDir},{_zDir}), dist={_distance}, dy={_yDelta})";
    }
}
