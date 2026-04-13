using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    /// <summary>
    /// Climb up or down a ladder/vine at the current X,Z position.
    /// </summary>
    public sealed class MoveClimb : IMove
    {
        public MoveType Type => MoveType.Climb;
        public int XOffset => 0;
        public int ZOffset => 0;
        public bool DynamicY => false;

        private readonly bool _up;

        public MoveClimb(bool up)
        {
            _up = up;
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
        {
            var currentMat = ctx.GetMaterial(x, y, z);
            if (!MoveHelper.IsClimbable(currentMat))
            {
                result.SetImpossible();
                return;
            }

            if (_up)
            {
                int destY = y + 1;
                if (!ctx.CanWalkThrough(x, destY + 1, z))
                {
                    result.SetImpossible();
                    return;
                }

                var aboveMat = ctx.GetMaterial(x, destY, z);
                if (MoveHelper.IsClimbable(aboveMat))
                {
                    result.Set(x, destY, z, ActionCosts.LadderUpOne);
                    return;
                }

                // Top of climbable: only allow if we can transition to a solid
                // surface nearby (ladders have wall collision, vines don't).
                // Check if the destination block itself is walkable-through and
                // there's solid ground at (x, destY-1, z) -- meaning we can
                // stand at destY. This handles ladder-tops where the ladder ends
                // but the block above is air and we can step onto the floor.
                if (!aboveMat.IsSolid() && ctx.CanWalkOn(x, destY - 1, z))
                {
                    result.Set(x, destY, z, ActionCosts.LadderUpOne);
                    return;
                }

                result.SetImpossible();
            }
            else
            {
                int destY = y - 1;
                var belowMat = ctx.GetMaterial(x, destY, z);
                if (MoveHelper.IsClimbable(belowMat) || !belowMat.IsSolid())
                {
                    result.Set(x, destY, z, ActionCosts.LadderDownOne);
                    return;
                }

                result.SetImpossible();
            }
        }
    }
}
