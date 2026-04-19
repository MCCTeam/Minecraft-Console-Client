using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
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
        private bool _carriedGroundEntry;
        private bool _releaseForwardLatched;
        private readonly SidewallParkourController? _sidewallController;

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

            if (segment.ParkourProfile == ParkourProfile.Sidewall)
                _sidewallController = new SidewallParkourController(segment, nextSegment);
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            _tickCount++;

            if (_sidewallController is not null)
                return _sidewallController.Tick(pos, physics, input, world);

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = ExpectedEnd.Y - pos.Y;
            double horizDistSq = dx * dx + dz * dz;
            bool prepareJumpTouchdown = _phase == Phase.Airborne && _leftGround && physics.OnGround;
            bool groundedPrepareJumpHandoff = (_phase == Phase.Landing || prepareJumpTouchdown)
                && physics.OnGround
                && _segment.ExitTransition == PathTransitionType.PrepareJump
                && _segment.ExitHints.RequireJumpReady
                && (TemplateFootingHelper.IsCenterInsideTargetBlock(pos, _segment.End)
                    || TemplateHelper.HasReachedSegmentEndPlane(pos, _segment));

            float targetYaw = TemplateHelper.CalculateYaw(dx, dz);
            float targetPitch = TemplateHelper.CalculatePitch(dx, dy, dz);
            physics.Yaw = groundedPrepareJumpHandoff
                ? TemplateHelper.SmoothYaw(physics.Yaw, TemplateHelper.GetExitHeadingYaw(_segment))
                : TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
            physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);

            switch (_phase)
            {
                case Phase.Approach:
                    if (physics.OnGround)
                    {
                        if (_tickCount == 1 && TemplateHelper.GetHorizontalSpeed(physics) > 0.02)
                            _carriedGroundEntry = true;

                        double approachProgress = ((pos.X - ExpectedStart.X) * _segment.HeadingX)
                            + ((pos.Z - ExpectedStart.Z) * _segment.HeadingZ);
                        float yawDelta = YawDifference(physics.Yaw, targetYaw);
                        bool turnInPlace = yawDelta > 35f;
                        input.Forward = !turnInPlace;
                        input.Sprint = !turnInPlace;

                        bool carriedShortFinalStopJump = _carriedGroundEntry
                            && _segment.ExitTransition == PathTransitionType.FinalStop
                            && _horizDist <= 2.5;
                        bool carriedDescendingFinalStopJump = _carriedGroundEntry
                            && _segment.ExitTransition == PathTransitionType.FinalStop
                            && ExpectedEnd.Y < ExpectedStart.Y
                            && _horizDist <= 3.5;
                        bool carriedDescendingParkourJump = _carriedGroundEntry
                            && _segment.ExitTransition == PathTransitionType.PrepareJump
                            && ExpectedEnd.Y < ExpectedStart.Y
                            && _horizDist <= 3.5;
                        if (carriedShortFinalStopJump || carriedDescendingFinalStopJump || carriedDescendingParkourJump)
                            input.Sprint = false;

                        // Build momentum before jumping. Sprint speed is ~5.6 m/s
                        // (0.28 blocks/tick). More run-up = more airtime distance.
                        // Standing sprint jump (0t): ~3.6 blocks horizontal
                        // 2-tick sprint (0.56m):    ~4.3 blocks horizontal
                        // 4-tick sprint (1.1m):     ~5.0 blocks horizontal
                        double minApproachDistance;
                        bool carriedLongDescendingJump = _carriedGroundEntry
                            && ExpectedEnd.Y < ExpectedStart.Y
                            && _horizDist >= 5.0;
                        if (carriedLongDescendingJump)
                            minApproachDistance = 0.8; // use nearly the full landing block to preserve long-jump carry
                        else if (_horizDist >= 5.0)
                            minApproachDistance = 0.8; // 3+ ticks of sprint
                        else if (_horizDist >= 4.0)
                            minApproachDistance = 0.6; // 2-3 ticks of sprint
                        else if (_horizDist > 3.5)
                            minApproachDistance = 0.3; // 1-2 ticks of sprint
                        else
                            minApproachDistance = 0.0;

                        bool posReady = approachProgress >= minApproachDistance;
                        // Baritone snaps rotation to the exact target every tick and
                        // the server accepts it without disconnection. For our smooth
                        // yaw model we only need it at the takeoff tick: snap yaw the
                        // moment we are positionally ready, so the jump vector is
                        // aligned regardless of how many ticks we had to turn. This
                        // prevents mis-jumps when the approach was short and the
                        // 35 deg/tick smoothing had not finished by the jump tick.
                        if (posReady)
                        {
                            physics.Yaw = targetYaw;
                            input.Jump = true;
                            _phase = Phase.Airborne;
                        }
                    }
                    else
                    {
                        float yawDelta = YawDifference(physics.Yaw, targetYaw);
                        bool turnInPlace = yawDelta > 35f;
                        input.Forward = !turnInPlace;
                        input.Sprint = !turnInPlace;
                    }
                    // Widen the approach/run-up budget. With snap-to-target yaw the
                    // player aligns in a single tick, but servers may still need a few
                    // extra ticks of sprint acceleration on long jumps.
                    if (_tickCount > 80)
                        return TemplateState.Failed;
                    break;

                case Phase.Airborne:
                {
                    if (!physics.OnGround)
                        _leftGround = true;

                    bool pastTarget = IsPastTarget(pos);
                    bool parkourOnOrPastTarget = _segment.MoveType == MoveType.Parkour
                        && (TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, ExpectedEnd)
                            || TemplateHelper.HasReachedSegmentEndPlane(pos, _segment)
                            || pastTarget);
                    bool biasTowardExitInAir = _segment.ExitTransition == PathTransitionType.LandingRecovery
                        ? TemplateHelper.ShouldBiasTowardExitHeading(pos, _segment, distanceThreshold: 1.5)
                        : TemplateHelper.ShouldBiasTowardExitHeading(pos, _segment);
                    if (parkourOnOrPastTarget || biasTowardExitInAir)
                        TemplateHelper.FaceExitHeading(physics, _segment);

                    bool lookaheadAirBrake = TransitionBrakingPlanner.ShouldReleaseForwardInAir(
                        _segment, _nextSegment, pos, physics, world);
                    bool releaseInAir = ShouldReleaseInAir(pos, physics, world);
                    _releaseForwardLatched |= releaseInAir;
                    bool earlySoftBrake = _segment.ExitTransition == PathTransitionType.LandingRecovery
                        && lookaheadAirBrake
                        && !releaseInAir;

                    if (_releaseForwardLatched || pastTarget)
                    {
                        input.Forward = false;
                        input.Sprint = false;
                    }
                    else if (earlySoftBrake)
                    {
                        input.Forward = true;
                        input.Sprint = false;
                    }
                    else
                    {
                        input.Forward = true;
                        input.Sprint = !(_segment.ExitTransition == PathTransitionType.FinalStop
                            && ExpectedEnd.Y < ExpectedStart.Y);
                    }

                    if (_leftGround && physics.OnGround)
                    {
                        _phase = Phase.Landing;
                        goto case Phase.Landing;
                    }
                    break;
                }

                case Phase.Landing:
                    bool descendingPrepareJump = _segment.ExitTransition == PathTransitionType.PrepareJump
                        && ExpectedEnd.Y < ExpectedStart.Y;
                    bool descendingPrepareJumpOnSupport = descendingPrepareJump
                        && physics.OnGround
                        && TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, _segment.End);
                    bool descendingPrepareJumpPastSupport = descendingPrepareJump
                        && physics.OnGround
                        && TemplateHelper.HasReachedSegmentEndPlane(pos, _segment)
                        && !descendingPrepareJumpOnSupport;

                    if (descendingPrepareJumpPastSupport)
                    {
                        input.Forward = false;
                        input.Sprint = false;
                        input.Back = true;
                        TemplateHelper.FaceExitHeading(physics, _segment);
                    }
                    else
                    {
                        TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
                        TemplateHelper.ApplyDecision(input, decision);
                        if (decision.HoldBack)
                            TemplateHelper.FaceSegmentHeading(physics, _segment);
                        else if (TemplateHelper.ShouldBiasTowardExitHeading(pos, _segment))
                            TemplateHelper.FaceExitHeading(physics, _segment);
                    }

                    if (_segment.ExitTransition == PathTransitionType.PrepareJump
                        && physics.OnGround
                        && (!descendingPrepareJump || descendingPrepareJumpOnSupport)
                        && GroundedSegmentController.ShouldComplete(_segment, pos, physics))
                    {
                        return TemplateState.Complete;
                    }

                    double horizToleranceLinear = _horizDist >= 3.5 ? 1.5 : 1.0;
                    double horizToleranceSq = horizToleranceLinear * horizToleranceLinear;
                    double vertTolerance = Math.Abs(ExpectedEnd.Y - ExpectedStart.Y) > 0.5 ? 1.5 : 1.0;
                    if (_segment.ExitTransition == PathTransitionType.ContinueStraight
                        && horizDistSq < horizToleranceSq && Math.Abs(dy) < vertTolerance)
                        return TemplateState.Complete;

                    if (_segment.ExitTransition != PathTransitionType.ContinueStraight
                        && physics.OnGround
                        && (TemplateHelper.IsSettledOnTargetBlock(pos, ExpectedEnd, physics)
                            || IsSettledOnTurnEntryStrip(pos, physics)))
                    {
                        return TemplateState.Complete;
                    }

                    // Baritone-style lenient success: the moment the player is standing
                    // on the target block (floor center matches), the jump is done. We
                    // keep the stricter settle checks above as the primary path because
                    // they capture momentum continuity for downstream segments, but the
                    // lenient check prevents spurious failures when the player touches
                    // down slightly off-center or with a small residual slide.
                    if (_segment.ExitTransition != PathTransitionType.ContinueStraight
                        && physics.OnGround
                        && LandedInsideTargetBlock(pos))
                    {
                        return TemplateState.Complete;
                    }
                    break;
            }

            if (pos.Y < ExpectedEnd.Y - 4.0)
                return TemplateState.Failed;

            // Baritone's MAX_TICKS_AWAY is 200 ticks (10 seconds) before it gives up
            // on a single movement. Short 60-tick windows were too tight for jumps
            // that include a run-up, a long airtime, and landing drag settling.
            if (_tickCount > 200)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }

        private bool LandedInsideTargetBlock(Location pos)
        {
            if (!TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, ExpectedEnd))
                return false;

            // Floor Y must match the target block. Accept anywhere within the block.
            return Math.Floor(pos.Y + 1.0E-4) == Math.Floor(ExpectedEnd.Y + 1.0E-4);
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

        private double GetLateralOffsetFromSegmentLine(Location pos)
        {
            double dirX = ExpectedEnd.X - ExpectedStart.X;
            double dirZ = ExpectedEnd.Z - ExpectedStart.Z;
            double len = Math.Sqrt(dirX * dirX + dirZ * dirZ);
            if (len < 0.001)
                return 0.0;

            dirX /= len;
            dirZ /= len;

            double relX = pos.X - ExpectedStart.X;
            double relZ = pos.Z - ExpectedStart.Z;
            return Math.Abs((-dirZ * relX) + (dirX * relZ));
        }

        private bool ShouldReleaseInAir(Location pos, PlayerPhysics physics, World world)
        {
            if (_segment.ExitTransition == PathTransitionType.ContinueStraight || physics.OnGround)
                return false;

            bool heuristicFinalStopRelease = _segment.ExitTransition == PathTransitionType.FinalStop
                && ShouldReleaseByRemainingLead(pos, physics);
            if (heuristicFinalStopRelease)
                return true;

            bool heuristicDescendingPrepareJumpRelease = _segment.ExitTransition == PathTransitionType.PrepareJump
                && ExpectedEnd.Y < ExpectedStart.Y
                && ShouldReleaseByRemainingLead(pos, physics);
            if (heuristicDescendingPrepareJumpRelease)
                return true;

            bool plannerWantsRelease = TransitionBrakingPlanner.ShouldReleaseForwardInAir(
                _segment, _nextSegment, pos, physics, world);
            double remaining = TemplateHelper.RemainingDistanceAlongSegment(pos, _segment);
            bool centeredOverLandingBlock = remaining <= 1.2;
            if (plannerWantsRelease && centeredOverLandingBlock)
                return true;

            Location? landingIfHolding = PredictLandingPosition(physics, world, holdForward: true, holdSprint: true);
            Location? landingIfReleased = PredictLandingPosition(physics, world, holdForward: false, holdSprint: false);
            if (landingIfHolding is null || landingIfReleased is null)
                return false;

            bool holdingStaysInside = TemplateFootingHelper.IsFootprintInsideTargetBlock(landingIfHolding.Value, ExpectedEnd);
            bool releasingStaysInside = TemplateFootingHelper.IsFootprintInsideTargetBlock(landingIfReleased.Value, ExpectedEnd);

            if (plannerWantsRelease && releasingStaysInside)
            {
                return true;
            }

            return !holdingStaysInside && releasingStaysInside;
        }

        private bool ShouldReleaseByRemainingLead(Location pos, PlayerPhysics physics)
        {
            double remaining = TemplateHelper.RemainingDistanceAlongSegment(pos, _segment);
            double forwardSpeed = Math.Max(0.0,
                TemplateHelper.ProjectHorizontalSpeedAlongHeading(physics, _segment.HeadingX, _segment.HeadingZ));
            double dropHeight = Math.Max(0.0, ExpectedStart.Y - ExpectedEnd.Y);
            double releaseLead = 0.14 + (Math.Max(0.0, dropHeight - 1.0) * 0.20);
            return remaining <= forwardSpeed + releaseLead;
        }

        private Location? PredictLandingPosition(PlayerPhysics physics, World world, bool holdForward, bool holdSprint)
        {
            PlayerPhysics sim = TemplateHelper.ClonePhysicsForPlanning(physics);
            var input = new MovementInput
            {
                Forward = holdForward,
                Sprint = holdSprint
            };

            for (int tick = 0; tick < 16; tick++)
            {
                sim.ApplyInput(input);
                sim.Tick(world);
                if (sim.OnGround)
                    return new Location(sim.Position.X, sim.Position.Y, sim.Position.Z);
            }

            return null;
        }

        private bool IsSettledOnTurnEntryStrip(Location pos, PlayerPhysics physics)
        {
            if (_segment.ExitTransition != PathTransitionType.LandingRecovery || _nextSegment is null)
                return false;

            if (_segment.HeadingX == _nextSegment.HeadingX && _segment.HeadingZ == _nextSegment.HeadingZ)
                return false;

            double horizontalSpeedSq = physics.DeltaMovement.X * physics.DeltaMovement.X
                + physics.DeltaMovement.Z * physics.DeltaMovement.Z;
            return TemplateFootingHelper.IsCenterInsideSupportStrip(pos, ExpectedEnd, _nextSegment.End)
                && !TemplateFootingHelper.WillCenterLeaveSupportStripNextTick(pos, physics, ExpectedEnd, _nextSegment.End)
                && horizontalSpeedSq <= 0.0016;
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
