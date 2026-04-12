using System;
using MinecraftClient.Mapping;
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
            if (current.ExitTransition is PathTransitionType.ContinueStraight or PathTransitionType.PrepareJump)
                return TransitionBrakingDecision.CarryMomentum(current.PreserveSprint);

            double remaining = RemainingDistanceAlongSegment(current, pos);
            double forwardSpeed = Math.Max(0.0, ProjectHorizontalSpeedAlongHeading(physics, current.HeadingX, current.HeadingZ));
            double coastStopDistance = EstimateGroundStopDistance(physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: false);
            double hardBrakeDistance = EstimateGroundStopDistance(physics, world, current.HeadingX, current.HeadingZ, applyBackBrake: true);

            if (current.ExitTransition == PathTransitionType.FinalStop)
            {
                if (remaining < 0.0)
                    return TransitionBrakingDecision.Brake;

                if (forwardSpeed > GroundSpeedThreshold && remaining <= hardBrakeDistance + FinalBrakeLead)
                    return TransitionBrakingDecision.Brake;

                if (forwardSpeed <= GroundSpeedThreshold && remaining > 0.0)
                    return TransitionBrakingDecision.CarryMomentum(preserveSprint: false);
            }

            if (current.ExitTransition == PathTransitionType.Turn && remaining <= hardBrakeDistance + TurnBrakeLead)
            {
                return TransitionBrakingDecision.Brake;
            }

            if (remaining <= coastStopDistance + FinalStopLead)
                return TransitionBrakingDecision.Coast;

            return TransitionBrakingDecision.CarryMomentum(current.PreserveSprint);
        }

        public static bool ShouldReleaseForwardInAir(PathSegment current, PathSegment? next, Location pos, PlayerPhysics physics)
        {
            if (current.ExitTransition is not (PathTransitionType.FinalStop or PathTransitionType.Turn or PathTransitionType.LandingRecovery))
                return false;

            double remaining = RemainingDistanceAlongSegment(current, pos);
            double forwardSpeed = Math.Max(0.0, ProjectHorizontalSpeedAlongHeading(physics, current.HeadingX, current.HeadingZ));

            return remaining <= forwardSpeed + AirReleaseLead;
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
    }
}
