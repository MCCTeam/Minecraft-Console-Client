using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Sprint-jump across a gap. Uses a phase-based state machine:
    /// Approach -> jump on first available ground tick -> Airborne -> Landing check.
    /// </summary>
    public sealed class SprintJumpTemplate : IActionTemplate
    {
        private enum Phase { Approach, Airborne, Landing }

        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private int _tickCount;
        private Phase _phase = Phase.Approach;
        private readonly int _distance;

        public SprintJumpTemplate(Location start, Location end)
        {
            ExpectedStart = start;
            ExpectedEnd = end;

            double dx = Math.Abs(end.X - start.X);
            double dz = Math.Abs(end.Z - start.Z);
            _distance = (int)Math.Round(Math.Max(dx, dz));
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input)
        {
            _tickCount++;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = ExpectedEnd.Y - pos.Y;
            double horizDistSq = dx * dx + dz * dz;

            physics.Yaw = TemplateHelper.CalculateYaw(dx, dz);
            input.Forward = true;
            input.Sprint = true;

            switch (_phase)
            {
                case Phase.Approach:
                    if (physics.OnGround)
                    {
                        input.Jump = true;
                        _phase = Phase.Airborne;
                    }
                    if (_tickCount > 20)
                        return TemplateState.Failed;
                    break;

                case Phase.Airborne:
                    if (!physics.OnGround)
                        break;
                    // Landed
                    _phase = Phase.Landing;
                    goto case Phase.Landing;

                case Phase.Landing:
                    if (horizDistSq < 2.0 && Math.Abs(dy) < 1.0)
                        return TemplateState.Complete;
                    return TemplateState.Failed;
            }

            if (pos.Y < ExpectedEnd.Y - 4.0)
                return TemplateState.Failed;

            if (_tickCount > 60)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }
    }
}
