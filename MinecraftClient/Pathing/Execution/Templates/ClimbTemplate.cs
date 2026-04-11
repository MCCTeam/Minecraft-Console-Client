using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Climb up or down a ladder/vine by 1 block.
    /// Up: pushes against the wall (Forward + face center) and jumps.
    /// Down: releases all input to let gravity + climbable friction handle descent.
    /// </summary>
    public sealed class ClimbTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private readonly bool _goingUp;
        private int _tickCount;

        public ClimbTemplate(Location start, Location end)
        {
            ExpectedStart = start;
            ExpectedEnd = end;
            _goingUp = end.Y > start.Y;
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input)
        {
            _tickCount++;

            double dy = ExpectedEnd.Y - pos.Y;
            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double horizDistSq = dx * dx + dz * dz;

            if (Math.Abs(dy) < 0.4 && horizDistSq < 0.5)
                return TemplateState.Complete;

            if (_tickCount > 120)
                return TemplateState.Failed;

            if (physics.OnClimbable)
            {
                if (_goingUp)
                {
                    input.Jump = true;
                    input.Forward = true;
                    if (horizDistSq > 0.01)
                        physics.Yaw = TemplateHelper.CalculateYaw(dx, dz);
                }
                else
                {
                    // Descending: release all input, gravity pulls down at clamped speed.
                    // Do NOT press Sneak (that would freeze position on ladders).
                    // Do NOT press Jump (that would push upward).
                    // Keep centered horizontally by gently steering if drifting.
                    if (horizDistSq > 0.15)
                    {
                        physics.Yaw = TemplateHelper.CalculateYaw(dx, dz);
                        input.Forward = true;
                    }
                }
            }
            else
            {
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
