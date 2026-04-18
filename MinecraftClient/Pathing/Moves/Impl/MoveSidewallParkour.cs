using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Moves.Impl
{
    public sealed class MoveSidewallParkour : IMove
    {
        public MoveType Type => MoveType.Parkour;
        public int XOffset { get; }
        public int ZOffset { get; }
        public bool DynamicY => false;

        private readonly int _yDelta;

        public MoveSidewallParkour(int xOffset, int zOffset, int yDelta = 0)
        {
            XOffset = xOffset;
            ZOffset = zOffset;
            _yDelta = yDelta;
        }

        public void Calculate(CalculationContext ctx, int x, int y, int z, ref MoveResult result)
        {
            if (!ctx.AllowParkour || !ctx.CanSprint)
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

            if (!ParkourFeasibility.IsSidewallProfile(XOffset, ZOffset, _yDelta))
            {
                result.SetImpossible();
                return;
            }

            Material standingOn = ctx.GetMaterial(x, y - 1, z);
            if (standingOn.CanBeClimbedOn())
            {
                result.SetImpossible();
                return;
            }

            Material atFeet = ctx.GetMaterial(x, y, z);
            if (atFeet.IsLiquid())
            {
                result.SetImpossible();
                return;
            }

            ParkourFeasibility.GetSidewallAxes(XOffset, ZOffset, out int forwardX, out int forwardZ, out int lateralX, out int lateralZ);

            int destX = x + XOffset;
            int destY = y + _yDelta;
            int destZ = z + ZOffset;

            if (!ctx.CanWalkThrough(x, y + 2, z))
            {
                result.SetImpossible();
                return;
            }

            if (!ParkourFeasibility.HasDominantAxisRunUp(ctx, x, y, z, forwardX, forwardZ, XOffset, ZOffset, _yDelta))
            {
                result.SetImpossible();
                return;
            }

            if (!ParkourFeasibility.HasSidewallArcClearance(ctx, x, y, z, forwardX, forwardZ, lateralX, lateralZ, XOffset, ZOffset, _yDelta))
            {
                result.SetImpossible();
                return;
            }

            if (!ParkourFeasibility.HasSidewallLandingClearance(ctx, destX, destY, destZ, forwardX, forwardZ, lateralX, lateralZ))
            {
                result.SetImpossible();
                return;
            }

            double horizDist = Math.Sqrt((double)(XOffset * XOffset + ZOffset * ZOffset));
            double cost = _yDelta switch
            {
                > 0 => horizDist * ctx.SprintCost + ctx.JumpPenalty * 2,
                < 0 => horizDist * ctx.SprintCost + ctx.JumpPenalty + ActionCosts.FallCost(-_yDelta),
                _ => horizDist * ctx.SprintCost + ctx.JumpPenalty,
            };

            result.Set(destX, destY, destZ, cost, ParkourProfile.Sidewall);
        }
    }
}
