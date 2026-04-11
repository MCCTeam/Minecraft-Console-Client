using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Climb up or down a ladder/vine by 1 block.
    /// Pushes against the wall (Forward + face center) and jumps for upward movement.
    /// </summary>
    public sealed class ClimbTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private int _tickCount;

        public ClimbTemplate(Location start, Location end)
        {
            ExpectedStart = start;
            ExpectedEnd = end;
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input)
        {
            _tickCount++;

            double dy = ExpectedEnd.Y - pos.Y;
            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double horizDistSq = dx * dx + dz * dz;

            if (Math.Abs(dy) < 0.3 && horizDistSq < 0.5)
                return TemplateState.Complete;

            if (_tickCount > 100)
                return TemplateState.Failed;

            if (physics.OnClimbable)
            {
                if (dy > 0)
                {
                    input.Jump = true;
                    input.Forward = true;
                    if (horizDistSq > 0.01)
                        physics.Yaw = TemplateHelper.CalculateYaw(dx, dz);
                }
                // Going down: don't press anything, gravity + climbable friction handles it
            }
            else
            {
                // Left the climbable area -- walk toward destination
                if (horizDistSq > 0.01)
                {
                    physics.Yaw = TemplateHelper.CalculateYaw(dx, dz);
                    input.Forward = true;
                }
            }

            return TemplateState.InProgress;
        }
    }
}
