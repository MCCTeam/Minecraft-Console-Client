using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Jump up 1 block while moving 1 block in a cardinal direction.
    /// Faces destination, sprints forward, and jumps when on ground.
    /// </summary>
    public sealed class AscendTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private int _tickCount;
        private Location _lastPos;
        private int _stuckTicks;

        public AscendTemplate(Location start, Location end)
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
            double dy = ExpectedEnd.Y - pos.Y;
            double horizDistSq = dx * dx + dz * dz;

            // Complete when close to destination. Sprint bouncing can leave the player
            // slightly above ground, so we don't require OnGround here.
            if (horizDistSq < 0.25 && Math.Abs(dy) < 0.8)
                return TemplateState.Complete;

            double movedSq = TemplateHelper.HorizontalDistanceSq(pos, _lastPos);
            double movedY = Math.Abs(pos.Y - _lastPos.Y);
            _stuckTicks = (movedSq < 0.0005 && movedY < 0.001) ? _stuckTicks + 1 : 0;
            _lastPos = pos;

            if (_stuckTicks > 40 || _tickCount > 80)
                return TemplateState.Failed;

            physics.Yaw = TemplateHelper.CalculateYaw(dx, dz);
            input.Forward = true;
            input.Sprint = true;

            if (physics.OnGround && dy > 0.1)
                input.Jump = true;

            return TemplateState.InProgress;
        }
    }
}
