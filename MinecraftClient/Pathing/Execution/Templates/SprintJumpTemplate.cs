using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Sprint-jump across a gap. Uses a phase-based state machine:
    /// Approach -> jump when ready -> Airborne -> Landing check.
    /// For long jumps (>= 3.5 blocks), delays the jump until the player
    /// has moved toward the edge of the starting block for maximum distance.
    /// </summary>
    public sealed class SprintJumpTemplate : IActionTemplate
    {
        private enum Phase { Approach, Airborne, Landing }

        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private readonly double _horizDist;
        private int _tickCount;
        private Phase _phase = Phase.Approach;

        public SprintJumpTemplate(Location start, Location end)
        {
            ExpectedStart = start;
            ExpectedEnd = end;
            double dx = end.X - start.X;
            double dz = end.Z - start.Z;
            _horizDist = Math.Sqrt(dx * dx + dz * dz);
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
                        double fromStartSq = TemplateHelper.HorizontalDistanceSq(pos, ExpectedStart);

                        // For long jumps, delay the jump until the player has sprinted
                        // toward the block edge. Baritone waits until playerFeet is in
                        // the next block (~0.5 blocks from center) for dist >= 4.
                        // For medium jumps (dist 3), wait 0.35 blocks (Baritone: 0.7).
                        double minApproachSq;
                        if (_horizDist >= 3.5)
                            minApproachSq = 0.25; // 0.5 blocks
                        else if (_horizDist >= 2.5)
                            minApproachSq = 0.12; // ~0.35 blocks
                        else
                            minApproachSq = 0.0;

                        if (fromStartSq >= minApproachSq)
                        {
                            input.Jump = true;
                            _phase = Phase.Airborne;
                        }
                    }
                    if (_tickCount > 30)
                        return TemplateState.Failed;
                    break;

                case Phase.Airborne:
                    if (!physics.OnGround)
                        break;
                    _phase = Phase.Landing;
                    goto case Phase.Landing;

                case Phase.Landing:
                    // Tolerance scales with jump distance
                    double horizTolerance = _horizDist >= 3.5 ? 3.0 : 2.0;
                    if (horizDistSq < horizTolerance && Math.Abs(dy) < 1.0)
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
