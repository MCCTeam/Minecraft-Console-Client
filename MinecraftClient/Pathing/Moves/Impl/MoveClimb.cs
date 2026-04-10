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
                if (MoveHelper.IsClimbable(aboveMat) || !ctx.GetMaterial(x, destY, z).IsSolid())
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
