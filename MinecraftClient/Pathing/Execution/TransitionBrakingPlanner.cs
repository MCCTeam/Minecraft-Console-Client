using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution
{
    public static class TransitionBrakingPlanner
    {
        private const double GroundSpeedThreshold = 0.025;
        private const int MaxSimulationTicks = 14;
        private const double FinalStopLead = 0.06;
        private const double FinalBrakeLead = 0.04;
        private const double TurnBrakeLead = 0.10;
        private const double AirReleaseLead = 0.14;

        public static TransitionBrakingDecision Plan(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics, World world)
        {
            if (physics.OnGround
                && current.ExitTransition == PathTransitionType.LandingRecovery
                && next is not null
                && !HasSameHeading(current, next))
            {
                double remaining = RemainingDistanceAlongSegment(current, pos);
                double forwardSpeed = Math.Max(0.0,
                    ProjectHorizontalSpeedAlongHeading(physics, current.HeadingX, current.HeadingZ));
                double maxExitSpeed = !double.IsPositiveInfinity(current.ExitHints.MaxExitSpeed)
                    ? current.ExitHints.MaxExitSpeed
                    : 0.035;
                double coastStopDistance = EstimateGroundStopDistance(
                    physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: false);
                double hardBrakeDistance = EstimateGroundStopDistance(
                    physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: true);

                if (TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, current.End)
                    && forwardSpeed <= maxExitSpeed)
                {
                    return TransitionBrakingDecision.Coast;
                }

                if (remaining < 0.0 && forwardSpeed > maxExitSpeed)
                    return TransitionBrakingDecision.Brake;

                if (forwardSpeed > GroundSpeedThreshold && remaining <= hardBrakeDistance + TurnBrakeLead)
                    return TransitionBrakingDecision.Brake;

                if (remaining <= coastStopDistance + FinalStopLead)
                    return TransitionBrakingDecision.Coast;
            }

            TransitionInputProfile profile;
            if (physics.OnGround)
            {
                profile = TransitionLookaheadEvaluator.ChooseGroundProfile(current, pos, physics, world);
            }
            else
            {
                if (!current.ExitHints.AllowAirBrake)
                    return TransitionBrakingDecision.CarryMomentum(current.PreserveSprint);

                profile = TransitionLookaheadEvaluator.ChooseAirProfile(current, pos, physics, world);
            }

            return profile switch
            {
                TransitionInputProfile.Carry => TransitionBrakingDecision.CarryMomentum(current.PreserveSprint),
                TransitionInputProfile.Coast => TransitionBrakingDecision.Coast,
                TransitionInputProfile.Brake => TransitionBrakingDecision.Brake,
                TransitionInputProfile.AirHoldForward => TransitionBrakingDecision.CarryMomentum(current.PreserveSprint),
                TransitionInputProfile.AirRelease => TransitionBrakingDecision.Coast,
                TransitionInputProfile.AirBrake => TransitionBrakingDecision.Brake,
                _ => TransitionBrakingDecision.Coast
            };
        }

        public static bool ShouldReleaseForwardInAir(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics)
        {
            if (current.ExitTransition is not (PathTransitionType.FinalStop or PathTransitionType.Turn or PathTransitionType.LandingRecovery))
                return false;

            double remaining = RemainingDistanceAlongSegment(current, pos);
            double forwardSpeed = Math.Max(0.0, ProjectHorizontalSpeedAlongHeading(physics, current.HeadingX, current.HeadingZ));

            return remaining <= forwardSpeed + AirReleaseLead;
        }

        public static bool ShouldReleaseForwardInAir(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics, World world)
        {
            if (!current.ExitHints.AllowAirBrake)
                return false;

            TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseAirProfile(current, pos, physics, world);
            return profile is TransitionInputProfile.AirRelease or TransitionInputProfile.AirBrake;
        }

        public static double EstimateGroundStopDistance(PlayerPhysics physics, World world, int headingX, int headingZ, bool applyBackBrake)
        {
            if (!physics.OnGround)
                return 0.0;

            double forwardSpeed = Math.Max(0.0, ProjectHorizontalSpeedAlongHeading(physics, headingX, headingZ));
            if (forwardSpeed <= GroundSpeedThreshold)
                return 0.0;

            float blockFriction = PlayerPhysics.GetMaterialFriction(
                world.GetBlock(new Location(physics.Position.X, physics.Position.Y - 0.5000010, physics.Position.Z)).Type);
            double drag = blockFriction * PhysicsConsts.FrictionMultiplier;
            double acceleration = physics.MovementSpeed
                                  * (PhysicsConsts.GroundAccelerationFactor / (drag * drag * drag))
                                  * PhysicsConsts.InputFriction;

            if (applyBackBrake)
                acceleration *= 0.98;

            double distance = 0.0;
            double speed = forwardSpeed;
            for (int tick = 0; tick < MaxSimulationTicks; tick++)
            {
                distance += speed;
                speed = applyBackBrake
                    ? Math.Max(0.0, (speed - acceleration) * drag)
                    : speed * drag;

                if (speed <= GroundSpeedThreshold)
                    break;
            }

            return distance;
        }

        private static double RemainingDistanceAlongSegment(PathSegment current, Location pos)
        {
            double dx = current.End.X - pos.X;
            double dz = current.End.Z - pos.Z;
            return dx * current.HeadingX + dz * current.HeadingZ;
        }

        private static double ProjectHorizontalSpeedAlongHeading(PlayerPhysics physics, int headingX, int headingZ)
        {
            return physics.DeltaMovement.X * headingX + physics.DeltaMovement.Z * headingZ;
        }

        private static bool HasSameHeading(PathSegment current, PathSegment next)
        {
            return current.HeadingX == next.HeadingX && current.HeadingZ == next.HeadingZ;
        }
    }
}
