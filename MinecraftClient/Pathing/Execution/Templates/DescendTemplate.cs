using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Walk off a ledge and drop 1-N blocks to a landing spot.
    /// Walks toward the destination; gravity handles the fall.
    /// Supports both solid landings and water landings.
    /// </summary>
    public sealed class DescendTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private int _tickCount;
        private bool _hasFallen;

        public DescendTemplate(Location start, Location end)
        {
            ExpectedStart = start;
            ExpectedEnd = end;
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input)
        {
            _tickCount++;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = ExpectedEnd.Y - pos.Y;
            double horizDistSq = dx * dx + dz * dz;

            if (!physics.OnGround)
                _hasFallen = true;

            // Completion: landed on ground near destination
            if (_hasFallen && physics.OnGround && horizDistSq < 0.5 && Math.Abs(dy) < 0.8)
                return TemplateState.Complete;

            // Completion: already at destination without falling (e.g., single step down)
            if (horizDistSq < 0.25 && Math.Abs(dy) < 0.5 && physics.OnGround)
                return TemplateState.Complete;

            // Completion: landed in water near destination
            if (_hasFallen && physics.InWater && horizDistSq < 0.5 && Math.Abs(dy) < 2.0)
                return TemplateState.Complete;

            // Fail if climbing up instead of descending
            if (pos.Y > ExpectedStart.Y + 2.0)
                return TemplateState.Failed;

            if (_tickCount > 200)
                return TemplateState.Failed;

            if (horizDistSq > 0.01)
            {
                physics.Yaw = TemplateHelper.CalculateYaw(dx, dz);
                input.Forward = true;
                if (physics.OnClimbable)
                    input.Forward = false;
            }

            return TemplateState.InProgress;
        }
    }
}
