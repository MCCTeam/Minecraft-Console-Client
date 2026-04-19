using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    internal sealed class SidewallParkourController
    {
        private enum PrepareJumpAirProfile
        {
            Hold,
            Coast,
            Release
        }

        private enum Phase
        {
            Approach,
            Airborne,
            Landing
        }

        internal Location ExpectedStart { get; }
        internal Location ExpectedEnd { get; }

        private readonly PathSegment _segment;
        private readonly PathSegment? _nextSegment;
        private readonly double _horizDist;
        private readonly double _dominantDist;
        private readonly float _takeoffYaw;
        private readonly float _nominalLandingYaw;

        private int _tickCount;
        private Phase _phase = Phase.Approach;
        private bool _leftGround;
        private bool _carriedGroundEntry;
        private bool _releaseForwardLatched;

        private const float YawToleranceDeg = 5f;
        private const float MaxYawStepPerTick = 20f;
        private const double CarryRunwayThreshold = 0.10;

        internal SidewallParkourController(PathSegment segment, PathSegment? nextSegment)
        {
            _segment = segment;
            _nextSegment = nextSegment;
            ExpectedStart = segment.Start;
            ExpectedEnd = segment.End;

            double dx = segment.End.X - segment.Start.X;
            double dz = segment.End.Z - segment.Start.Z;
            _horizDist = Math.Sqrt(dx * dx + dz * dz);
            _dominantDist = Math.Max(Math.Abs(dx), Math.Abs(dz));
            double dropHeight = segment.Start.Y - segment.End.Y;
            _nominalLandingYaw = TemplateHelper.CalculateYaw(dx, dz);
            _takeoffYaw = nextSegment is not null
                && _dominantDist >= 5.0
                && dropHeight > 0.0
                && dropHeight < 1.5
                ? TemplateHelper.GetApproachYaw(segment)
                : TemplateHelper.GetSidewallTakeoffYaw(segment);
        }

        internal TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            _tickCount++;

            if (_tickCount == 1 && TemplateHelper.GetHorizontalSpeed(physics) > 0.02)
                _carriedGroundEntry = true;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = ExpectedEnd.Y - pos.Y;
            double carryApproachProgress = _tickCount == 1
                ? TemplateHelper.ProgressAlongApproach(pos, _segment)
                : 0.0;

            if (_phase != Phase.Landing)
            {
                float activeYaw;
                if (_phase == Phase.Approach
                    && _carriedGroundEntry
                    && _tickCount == 1
                    && carryApproachProgress < CarryRunwayThreshold)
                {
                    activeYaw = TemplateHelper.GetApproachYaw(_segment);
                }
                else if (_phase == Phase.Airborne && ShouldUseLandingYaw(pos))
                {
                    activeYaw = GetLandingYaw(pos);
                }
                else
                {
                    activeYaw = _takeoffYaw;
                }

                float yawStep = _phase == Phase.Airborne ? 20f : MaxYawStepPerTick;
                physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, activeYaw, maxStep: yawStep);
                physics.Pitch = TemplateHelper.SmoothPitch(
                    physics.Pitch,
                    TemplateHelper.CalculatePitch(dx, dy, dz));
            }

            switch (_phase)
            {
                case Phase.Approach:
                    return TickApproach(pos, physics, input, world);

                case Phase.Airborne:
                    return TickAirborne(pos, physics, input, world);

                case Phase.Landing:
                    return TickLanding(pos, physics, input, world);

                default:
                    return TemplateState.Failed;
            }
        }

        private TemplateState TickApproach(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            if (!physics.OnGround && ShouldRecoverGroundApproach(pos, physics))
            {
                physics.OnGround = true;
                if (physics.DeltaMovement.Y < 0.0)
                    physics.DeltaMovement = new Vec3d(physics.DeltaMovement.X, 0.0, physics.DeltaMovement.Z);
            }

            if (!physics.OnGround)
            {
                _leftGround = true;
                _phase = Phase.Airborne;
                return TickAirborne(pos, physics, input, world);
            }

            float yawDelta = YawDifference(physics.Yaw, _takeoffYaw);
            bool turnInPlace = yawDelta > 35f;
            input.Forward = !turnInPlace;
            input.Sprint = !turnInPlace;

            double minApproachDistance = GetMinApproachDistance();
            double approachProgress = TemplateHelper.ProgressAlongApproach(pos, _segment);
            double approachSpeed = GetApproachSpeed(physics);
            if (_carriedGroundEntry && _tickCount == 1 && approachProgress < CarryRunwayThreshold)
            {
                input.Sprint = false;
                return TemplateState.InProgress;
            }

            if (yawDelta < YawToleranceDeg
                && approachProgress >= minApproachDistance
                && approachSpeed >= GetMinTakeoffApproachSpeed())
            {
                if (ShouldApplyLaunchStrafe())
                {
                    physics.Yaw = TemplateHelper.GetApproachYaw(_segment);
                    ApplyAirStrafe(physics, input);
                }

                if (ShouldSuppressSprintJumpTakeoff())
                    input.Sprint = false;
                input.Jump = true;
                _phase = Phase.Airborne;
            }

            if (_tickCount > 40)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }

        private TemplateState TickAirborne(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            if (!physics.OnGround)
                _leftGround = true;

            bool pastTarget = IsPastTarget(pos);
            if (ShouldApplyAirStrafe(pos))
                ApplyAirStrafe(physics, input);

            if (_nextSegment is null)
            {
                bool shouldRelease = ShouldReleaseInAir(pos, physics, world);
                _releaseForwardLatched |= shouldRelease;

                if (_releaseForwardLatched || pastTarget)
                {
                    input.Forward = false;
                    input.Sprint = false;
                }
                else
                {
                    input.Forward = true;
                    input.Sprint = true;
                }
            }
            else
            {
                switch (ChoosePrepareJumpAirProfile(pos, physics, world))
                {
                    case PrepareJumpAirProfile.Release:
                        input.Forward = false;
                        input.Sprint = false;
                        break;

                    case PrepareJumpAirProfile.Coast:
                        input.Forward = true;
                        input.Sprint = false;
                        break;

                    default:
                        input.Forward = true;
                        input.Sprint = true;
                        break;
                }
            }

            if (_leftGround && physics.OnGround)
            {
                _phase = Phase.Landing;
                return TickLanding(pos, physics, input, world);
            }

            if (pos.Y < ExpectedEnd.Y - 4.0 || _tickCount > 60)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }

        private bool ShouldApplyAirStrafe(Location pos)
        {
            if (ExpectedEnd.Y >= ExpectedStart.Y)
                return false;

            if (!NeedsAdditionalLateralBias(pos))
                return false;

            if (_dominantDist >= 5.0)
            {
                double dropHeight = ExpectedStart.Y - ExpectedEnd.Y;
                double lateStrafeThreshold = _nextSegment is not null
                    ? (dropHeight < 1.5 ? 2.30 : 1.55)
                    : 1.10;
                return TemplateHelper.RemainingDistanceAlongSegment(pos, _segment) <= lateStrafeThreshold;
            }

            if (_dominantDist < 3.0)
            {
                double shortDescendThreshold = _nextSegment is not null ? 1.15 : 0.95;
                return TemplateHelper.RemainingDistanceAlongSegment(pos, _segment) <= shortDescendThreshold;
            }

            double remaining = TemplateHelper.RemainingDistanceAlongSegment(pos, _segment);
            double threshold = _dominantDist >= 4.0
                ? Math.Max(2.4, _dominantDist * 0.65)
                : Math.Max(1.4, _dominantDist * 0.60);
            return remaining <= threshold;
        }

        private void ApplyAirStrafe(PlayerPhysics physics, MovementInput input)
        {
            GetAirLateralDirection(out int desiredX, out int desiredZ);
            if (desiredX == 0 && desiredZ == 0)
                return;

            double yawRad = physics.Yaw * (Math.PI / 180.0);
            double rightX = -Math.Cos(yawRad);
            double rightZ = -Math.Sin(yawRad);
            double projection = (desiredX * rightX) + (desiredZ * rightZ);
            if (projection >= 0.0)
                input.Right = true;
            else
                input.Left = true;
        }

        private void GetAirLateralDirection(out int desiredX, out int desiredZ)
        {
            TemplateHelper.GetApproachHeading(_segment, out int headingX, out int headingZ);
            if (headingX != 0)
            {
                desiredX = 0;
                desiredZ = Math.Sign(ExpectedEnd.Z - ExpectedStart.Z);
                return;
            }

            desiredX = Math.Sign(ExpectedEnd.X - ExpectedStart.X);
            desiredZ = 0;
        }

        private TemplateState TickLanding(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            if (!physics.OnGround)
            {
                _phase = Phase.Airborne;
                return TickAirborne(pos, physics, input, world);
            }

            if (_nextSegment is not null)
                return TickPrepareJumpLanding(pos, physics, input, world);

            return TickFinalStopLanding(pos, physics, input, world);
        }

        private TemplateState TickPrepareJumpLanding(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            if (!physics.OnGround)
                return TemplateState.InProgress;

            if (NeedsFootingRecovery(pos, physics))
            {
                ApplyFootingRecovery(pos, physics, input);
                if (TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, ExpectedEnd)
                    && !TemplateFootingHelper.WillLeaveTargetBlockNextTick(pos, physics, ExpectedEnd))
                {
                    return TemplateState.Complete;
                }

                return ContinueOrFail(pos);
            }

            GroundedSegmentController.Apply(_segment, _nextSegment, pos, physics, input, world);
            if (TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, ExpectedEnd)
                && GroundedSegmentController.ShouldComplete(_segment, pos, physics))
                return TemplateState.Complete;

            return ContinueOrFail(pos);
        }

        private TemplateState TickFinalStopLanding(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            if (!physics.OnGround)
            {
                return ContinueOrFail(pos);
            }

            if (NeedsFootingRecovery(pos, physics))
            {
                ApplyFootingRecovery(pos, physics, input);
                if (TemplateHelper.IsSettledOnTargetBlock(pos, ExpectedEnd, physics))
                    return TemplateState.Complete;
                return ContinueOrFail(pos);
            }

            GroundedSegmentController.Apply(_segment, _nextSegment, pos, physics, input, world);
            if (GroundedSegmentController.ShouldComplete(_segment, pos, physics))
                return TemplateState.Complete;

            return ContinueOrFail(pos);
        }

        private double GetMinApproachDistance()
        {
            if (_carriedGroundEntry)
                return _dominantDist >= 4.0 ? 0.18 : 0.08;

            if (_dominantDist >= 4.0)
                return 0.10;

            return 0.0;
        }

        private double GetMinTakeoffApproachSpeed()
        {
            if (_dominantDist >= 4.0)
                return _carriedGroundEntry ? 0.09 : 0.10;

            if (ExpectedEnd.Y != ExpectedStart.Y)
                return _carriedGroundEntry ? 0.08 : 0.09;

            if (_dominantDist >= 3.0)
                return _carriedGroundEntry ? 0.09 : 0.08;

            return _carriedGroundEntry ? 0.06 : 0.05;
        }

        private double GetApproachSpeed(PlayerPhysics physics)
        {
            TemplateHelper.GetApproachHeading(_segment, out int headingX, out int headingZ);
            return Math.Max(0.0, TemplateHelper.ProjectHorizontalSpeedAlongHeading(physics, headingX, headingZ));
        }

        private bool ShouldSuppressSprintJumpTakeoff()
        {
            if (_dominantDist >= 3.0)
                return false;

            if (ExpectedEnd.Y < ExpectedStart.Y)
                return true;

            return _nextSegment is null
                && ExpectedEnd.Y == ExpectedStart.Y;
        }

        private bool ShouldApplyLaunchStrafe()
        {
            return IsShallowLongDescendingPrepareJump();
        }

        private bool IsShallowLongDescendingPrepareJump()
        {
            double dropHeight = ExpectedStart.Y - ExpectedEnd.Y;
            return _nextSegment is not null
                && _dominantDist >= 5.0
                && dropHeight > 0.0
                && dropHeight < 1.5;
        }

        private bool NeedsAdditionalLateralBias(Location pos)
        {
            double halfWidth = PhysicsConsts.PlayerWidth / 2.0;
            double blockMinX = Math.Floor(ExpectedEnd.X);
            double blockMaxX = blockMinX + 1.0;
            double blockMinZ = Math.Floor(ExpectedEnd.Z);
            double blockMaxZ = blockMinZ + 1.0;

            TemplateHelper.GetApproachHeading(_segment, out int headingX, out int headingZ);
            if (headingZ != 0)
                return pos.X - halfWidth < blockMinX || pos.X + halfWidth > blockMaxX;

            return pos.Z - halfWidth < blockMinZ || pos.Z + halfWidth > blockMaxZ;
        }

        private bool ShouldRecoverGroundApproach(Location pos, PlayerPhysics physics)
        {
            if (pos.Y < ExpectedStart.Y - 0.05 || pos.Y > ExpectedStart.Y + 0.05)
                return false;

            if (Math.Abs(physics.DeltaMovement.Y) > 0.12)
                return false;

            return TemplateFootingHelper.IsCenterInsideTargetBlock(pos, ExpectedStart);
        }

        private bool ShouldUseLandingYaw(Location pos)
        {
            double approachProgress = TemplateHelper.ProgressAlongApproach(pos, _segment);
            double activationProgress;
            if (_carriedGroundEntry)
            {
                activationProgress = 0.15;
            }
            else if (_horizDist <= 2.5)
            {
                activationProgress = 0.30;
            }
            else if (_horizDist <= 3.5)
            {
                activationProgress = 0.45;
            }
            else
            {
                activationProgress = 0.60;
            }

            if (ExpectedEnd.Y > ExpectedStart.Y)
                activationProgress += 0.10;
            else if (ExpectedEnd.Y < ExpectedStart.Y)
            {
                activationProgress = Math.Max(0.20, activationProgress - 0.10);
                if (_nextSegment is not null && _dominantDist < 3.0)
                {
                    // Keep the short descending prepare-jump takeoff yaw longer so the late air
                    // strafe can shave south carry instead of rotating into a south-biased drift.
                    activationProgress = Math.Max(activationProgress, _dominantDist - 0.55);
                }

                if (_nextSegment is not null && _dominantDist >= 5.0)
                {
                    double dropHeight = ExpectedStart.Y - ExpectedEnd.Y;

                    // Long descending sidewall jumps only need a small west bias at entry. If we
                    // rotate into landing yaw too early, we hit the landing block's north face
                    // before we have enough south depth to climb onto the top.
                    activationProgress = Math.Max(
                        activationProgress,
                        dropHeight < 1.5 ? _dominantDist - 0.35 : _dominantDist - 0.35);
                }
            }
            else if (_dominantDist >= 4.0)
                activationProgress = Math.Max(0.45, activationProgress - 0.10);

            return approachProgress >= activationProgress;
        }

        private float GetLandingYaw(Location pos)
        {
            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            if ((dx * dx) + (dz * dz) < 1.0E-6)
                return _nominalLandingYaw;

            return TemplateHelper.CalculateYaw(dx, dz);
        }

        private bool NeedsFootingRecovery(Location pos, PlayerPhysics physics)
        {
            if (!physics.OnGround)
                return false;

            if (TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, ExpectedEnd))
                return TemplateFootingHelper.WillLeaveTargetBlockNextTick(pos, physics, ExpectedEnd);

            if (TemplateFootingHelper.IsCenterInsideTargetBlock(pos, ExpectedEnd))
                return true;

            if (_nextSegment is null
                && ExpectedEnd.Y < ExpectedStart.Y
                && _segment.ParkourProfile == ParkourProfile.Sidewall
                && TemplateHelper.HorizontalDistanceSq(pos, ExpectedEnd) <= 0.81)
            {
                return true;
            }

            return TemplateHelper.HorizontalDistanceSq(pos, ExpectedEnd) <= 0.49;
        }

        private void ApplyFootingRecovery(Location pos, PlayerPhysics physics, MovementInput input)
        {
            float recoveryYaw = GetLandingYaw(pos);
            float yawDelta = YawDifference(physics.Yaw, recoveryYaw);
            physics.DeltaMovement = new Vec3d(0.0, physics.DeltaMovement.Y, 0.0);
            physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, recoveryYaw, maxStep: MaxYawStepPerTick);

            input.Forward = yawDelta <= 12f;
            input.Sprint = false;
            input.Back = false;
            input.Left = false;
            input.Right = false;
        }

        private bool IsPastTarget(Location pos)
        {
            double dirX = ExpectedEnd.X - ExpectedStart.X;
            double dirZ = ExpectedEnd.Z - ExpectedStart.Z;
            double len = Math.Sqrt(dirX * dirX + dirZ * dirZ);
            if (len < 0.001)
                return false;

            dirX /= len;
            dirZ /= len;

            double relX = pos.X - ExpectedEnd.X;
            double relZ = pos.Z - ExpectedEnd.Z;
            return relX * dirX + relZ * dirZ > 0.0;
        }

        private TemplateState ContinueOrFail(Location pos)
        {
            if (pos.Y < ExpectedEnd.Y - 4.0 || _tickCount > 60)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }

        private bool ShouldReleaseInAir(Location pos, PlayerPhysics physics, World world)
        {
            bool heuristicRelease = _segment.ExitTransition == PathTransitionType.FinalStop
                && ShouldReleaseByRemainingLead(pos, physics);
            if (heuristicRelease)
                return true;

            Location? landingIfHolding = PredictLandingPosition(physics, world, holdForward: true, holdSprint: true);
            Location? landingIfReleased = PredictLandingPosition(physics, world, holdForward: false, holdSprint: false);
            if (landingIfHolding is null || landingIfReleased is null)
                return false;

            bool holdingStaysInside = TemplateFootingHelper.IsFootprintInsideTargetBlock(landingIfHolding.Value, ExpectedEnd);
            bool releasingStaysInside = TemplateFootingHelper.IsFootprintInsideTargetBlock(landingIfReleased.Value, ExpectedEnd);
            return !holdingStaysInside && releasingStaysInside;
        }

        private PrepareJumpAirProfile ChoosePrepareJumpAirProfile(Location pos, PlayerPhysics physics, World world)
        {
            double remaining = TemplateHelper.RemainingDistanceAlongSegment(pos, _segment);
            double releaseThreshold = _dominantDist >= 4.0 ? 1.05 : 0.55;
            double coastThreshold = _dominantDist >= 4.0 ? 1.50 : 0.90;

            if (ExpectedEnd.Y < ExpectedStart.Y)
            {
                if (_dominantDist < 3.0)
                {
                    releaseThreshold += 0.75;
                    coastThreshold += 0.75;
                }
                else if (_dominantDist >= 5.0)
                {
                    double dropHeight = ExpectedStart.Y - ExpectedEnd.Y;
                    if (dropHeight < 1.5)
                    {
                        releaseThreshold = Math.Max(0.50, releaseThreshold - 0.55);
                        coastThreshold = Math.Max(0.85, coastThreshold - 0.65);
                    }
                    else
                    {
                        releaseThreshold = Math.Max(0.60, releaseThreshold - 0.45);
                        coastThreshold = Math.Max(0.95, coastThreshold - 0.45);
                    }
                }
                else
                {
                    releaseThreshold += 0.20;
                    coastThreshold += 0.25;
                }
            }

            if (remaining <= releaseThreshold)
                return PrepareJumpAirProfile.Release;

            if (remaining <= coastThreshold)
                return PrepareJumpAirProfile.Coast;

            Location? landingIfHolding = PredictLandingPosition(physics, world, holdForward: true, holdSprint: true);
            Location? landingIfCoasting = PredictLandingPosition(physics, world, holdForward: true, holdSprint: false);
            Location? landingIfReleased = PredictLandingPosition(physics, world, holdForward: false, holdSprint: false);

            if (landingIfHolding is null || landingIfCoasting is null || landingIfReleased is null)
                return PrepareJumpAirProfile.Hold;

            bool holdingStaysInside = TemplateFootingHelper.IsFootprintInsideTargetBlock(landingIfHolding.Value, ExpectedEnd);
            bool coastingStaysInside = TemplateFootingHelper.IsFootprintInsideTargetBlock(landingIfCoasting.Value, ExpectedEnd);
            bool releasingStaysInside = TemplateFootingHelper.IsFootprintInsideTargetBlock(landingIfReleased.Value, ExpectedEnd);

            if (holdingStaysInside)
                return PrepareJumpAirProfile.Hold;

            if (coastingStaysInside)
                return PrepareJumpAirProfile.Coast;

            if (releasingStaysInside)
                return PrepareJumpAirProfile.Release;

            double holdDistance = TemplateHelper.HorizontalDistanceSq(landingIfHolding.Value, ExpectedEnd);
            double coastDistance = TemplateHelper.HorizontalDistanceSq(landingIfCoasting.Value, ExpectedEnd);
            double releaseDistance = TemplateHelper.HorizontalDistanceSq(landingIfReleased.Value, ExpectedEnd);
            if (coastDistance <= holdDistance && coastDistance <= releaseDistance)
                return PrepareJumpAirProfile.Coast;

            return releaseDistance < holdDistance
                ? PrepareJumpAirProfile.Release
                : PrepareJumpAirProfile.Hold;
        }

        private bool ShouldReleaseByRemainingLead(Location pos, PlayerPhysics physics)
        {
            double remaining = TemplateHelper.RemainingDistanceAlongSegment(pos, _segment);
            double forwardSpeed = Math.Max(
                0.0,
                TemplateHelper.ProjectHorizontalSpeedAlongHeading(physics, _segment.HeadingX, _segment.HeadingZ));
            double dropHeight = Math.Max(0.0, ExpectedStart.Y - ExpectedEnd.Y);
            double releaseLead = 0.14 + (Math.Max(0.0, dropHeight - 1.0) * 0.20);
            return remaining <= forwardSpeed + releaseLead;
        }

        private static Location? PredictLandingPosition(PlayerPhysics physics, World world, bool holdForward, bool holdSprint)
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

        private static float YawDifference(float current, float target)
        {
            float delta = target - current;
            while (delta > 180f) delta -= 360f;
            while (delta < -180f) delta += 360f;
            return Math.Abs(delta);
        }

    }
}
