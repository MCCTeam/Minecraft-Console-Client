using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Walk/sprint toward a destination on the same Y level.
    /// Used for Traverse and Diagonal moves.
    /// </summary>
    public sealed class WalkTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private int _tickCount;
        private Location _lastPos;
        private int _stuckTicks;

        public WalkTemplate(Location start, Location end)
        {
            ExpectedStart = start;
            ExpectedEnd = end;
            _lastPos = start;
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input)
        {
            _tickCount++;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            physics.Yaw = TemplateHelper.CalculateYaw(dx, dz);
            input.Forward = true;

            if (physics.OnClimbable)
            {
                input.Jump = true;
                input.Sprint = false;
            }
            else
            {
                input.Sprint = true;
            }

            if (TemplateHelper.IsNear(pos, ExpectedEnd, horizThresholdSq: 0.20))
                return TemplateState.Complete;

            double movedSq = TemplateHelper.HorizontalDistanceSq(pos, _lastPos);
            int stuckThreshold = physics.OnClimbable ? 80 : 40;
            _stuckTicks = movedSq < 0.0005 ? _stuckTicks + 1 : 0;
            _lastPos = pos;

            int tickLimit = physics.OnClimbable ? 160 : 100;
            if (_stuckTicks > stuckThreshold || _tickCount > tickLimit)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }
    }
}
