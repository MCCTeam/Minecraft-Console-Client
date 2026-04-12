using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Jump across a gap. Uses a phase-based state machine:
    /// Approach -> Jump -> Airborne -> Landing.
    ///
    /// All parkour jumps use sprint-jumping (vanilla optimal horizontal distance).
    /// The key to landing on small platforms is releasing forward/sprint input mid-air
    /// once the player is close to or past the target, letting drag decelerate them
    /// onto the block.
    ///
    /// During Approach, the template waits for the yaw to be within 5 degrees of
    /// the target direction before jumping. For medium/long jumps, it also builds
    /// momentum by sprinting toward the block edge.
    /// </summary>
    public sealed class SprintJumpTemplate : IActionTemplate
    {
        private enum Phase { Approach, Airborne, Landing }

        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private readonly PathSegment _segment;
        private readonly PathSegment? _nextSegment;
        private readonly double _horizDist;
        private int _tickCount;
        private Phase _phase = Phase.Approach;
        private bool _leftGround;

        private const float YawToleranceDeg = 5f;

        public SprintJumpTemplate(PathSegment segment, PathSegment? nextSegment)
        {
            _segment = segment;
            _nextSegment = nextSegment;
            ExpectedStart = segment.Start;
            ExpectedEnd = segment.End;
            double dx = segment.End.X - segment.Start.X;
            double dz = segment.End.Z - segment.Start.Z;
            _horizDist = Math.Sqrt(dx * dx + dz * dz);
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            _tickCount++;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = ExpectedEnd.Y - pos.Y;
            double horizDistSq = dx * dx + dz * dz;

            float targetYaw = TemplateHelper.CalculateYaw(dx, dz);
            float targetPitch = TemplateHelper.CalculatePitch(dx, dy, dz);
            physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
            physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);

            switch (_phase)
            {
                case Phase.Approach:
                    input.Forward = true;
                    input.Sprint = true;

                    if (physics.OnGround)
                    {
                        double fromStartSq = TemplateHelper.HorizontalDistanceSq(pos, ExpectedStart);
                        float yawDelta = YawDifference(physics.Yaw, targetYaw);

                        // Build momentum before jumping. Sprint speed is ~5.6 m/s
                        // (0.28 blocks/tick). More run-up = more airtime distance.
                        // Standing sprint jump (0t): ~3.6 blocks horizontal
                        // 2-tick sprint (0.56m):    ~4.3 blocks horizontal
                        // 4-tick sprint (1.1m):     ~5.0 blocks horizontal
                        double minApproachSq;
                        if (_horizDist >= 5.0)
                            minApproachSq = 0.64; // 0.8 blocks - 3+ ticks of sprint
                        else if (_horizDist >= 4.0)
                            minApproachSq = 0.36; // 0.6 blocks - 2-3 ticks of sprint
                        else if (_horizDist > 2.5)
                            minApproachSq = 0.09; // 0.3 blocks - 1-2 ticks of sprint
                        else
                            minApproachSq = 0.0;

                        bool yawAligned = yawDelta < YawToleranceDeg;
                        bool posReady = fromStartSq >= minApproachSq;

                        if (yawAligned && posReady)
                        {
                            input.Jump = true;
                            _phase = Phase.Airborne;
                        }
                    }
                    if (_tickCount > 40)
                        return TemplateState.Failed;
                    break;

                case Phase.Airborne:
                {
                    if (!physics.OnGround)
                        _leftGround = true;

                    bool pastTarget = IsPastTarget(pos);
                    bool releaseInAir = TransitionBrakingPlanner.ShouldReleaseForwardInAir(_segment, _nextSegment, pos, physics);

                    if (releaseInAir || pastTarget)
                    {
                        input.Forward = false;
                        input.Sprint = false;
                    }
                    else
                    {
                        input.Forward = true;
                        input.Sprint = true;
                    }

                    if (_leftGround && physics.OnGround)
                    {
                        _phase = Phase.Landing;
                        goto case Phase.Landing;
                    }
                    break;
                }

                case Phase.Landing:
                    TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
                    TemplateHelper.ApplyDecision(input, decision);
                    if (decision.HoldBack)
                        TemplateHelper.FaceSegmentHeading(physics, _segment);

                    double horizToleranceLinear = _horizDist >= 3.5 ? 1.5 : 1.0;
                    double horizToleranceSq = horizToleranceLinear * horizToleranceLinear;
                    double vertTolerance = Math.Abs(ExpectedEnd.Y - ExpectedStart.Y) > 0.5 ? 1.5 : 1.0;
                    if (_segment.ExitTransition == PathTransitionType.ContinueStraight
                        && horizDistSq < horizToleranceSq && Math.Abs(dy) < vertTolerance)
                        return TemplateState.Complete;

                    if (_segment.ExitTransition != PathTransitionType.ContinueStraight
                        && TemplateHelper.IsSettledAtEnd(pos, ExpectedEnd, physics, horizThresholdSq: 0.0025))
                    {
                        return TemplateState.Complete;
                    }
                    break;
            }

            if (pos.Y < ExpectedEnd.Y - 4.0)
                return TemplateState.Failed;

            if (_tickCount > 60)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }

        private bool IsPastTarget(Location pos)
        {
            double dirX = ExpectedEnd.X - ExpectedStart.X;
            double dirZ = ExpectedEnd.Z - ExpectedStart.Z;
            double len = Math.Sqrt(dirX * dirX + dirZ * dirZ);
            if (len < 0.001) return false;
            dirX /= len;
            dirZ /= len;

            double relX = pos.X - ExpectedEnd.X;
            double relZ = pos.Z - ExpectedEnd.Z;
            double dot = relX * dirX + relZ * dirZ;
            return dot > 0.0;
        }

        private static float YawDifference(float current, float target)
        {
            float delta = target - current;
            while (delta > 180f) delta -= 360f;
            while (delta < -180f) delta += 360f;
            return Math.Abs(delta);
        }
    }
}
