using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Vertical free fall at the same X,Z. Waits for the player to land at the target Y.
    /// Supports both solid ground landings and water landings.
    /// </summary>
    public sealed class FallTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private int _tickCount;
        private bool _hasFallen;

        public FallTemplate(PathSegment segment, PathSegment? nextSegment)
        {
            ExpectedStart = segment.Start;
            ExpectedEnd = segment.End;
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            _tickCount++;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = pos.Y - ExpectedEnd.Y;
            double horizDistSq = dx * dx + dz * dz;

            if (!physics.OnGround)
                _hasFallen = true;

            // Solid ground landing near the target XZ
            if (_hasFallen && physics.OnGround && Math.Abs(dy) < 1.0 && horizDistSq < 1.0)
                return TemplateState.Complete;

            // Water landing near the target XZ
            if (_hasFallen && physics.InWater && Math.Abs(dy) < 2.0 && horizDistSq < 1.5)
                return TemplateState.Complete;

            if (_tickCount > 200)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }
    }
}
