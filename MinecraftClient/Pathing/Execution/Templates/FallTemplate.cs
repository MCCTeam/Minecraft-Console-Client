using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Vertical free fall at the same X,Z. Waits for the player to land at the target Y.
    /// </summary>
    public sealed class FallTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private int _tickCount;
        private bool _hasFallen;

        public FallTemplate(Location start, Location end)
        {
            ExpectedStart = start;
            ExpectedEnd = end;
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input)
        {
            _tickCount++;

            double dy = pos.Y - ExpectedEnd.Y;

            if (!physics.OnGround)
                _hasFallen = true;

            if (_hasFallen && physics.OnGround && Math.Abs(dy) < 1.0)
                return TemplateState.Complete;

            if (_tickCount > 200)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }
    }
}
