using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution.Templates;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution
{
    public static class TransitionLookaheadEvaluator
    {
        public static TransitionInputProfile ChooseGroundProfile(PathSegment segment, Location pos, PlayerPhysics physics, World world)
        {
            double remaining = TemplateHelper.RemainingDistanceAlongSegment(pos, segment);
            double forwardSpeed = Math.Max(0.0,
                TemplateHelper.ProjectHorizontalSpeedAlongHeading(physics, segment.HeadingX, segment.HeadingZ));

            bool requiresJumpEntry = segment.ExitHints.RequireJumpReady
                || segment.ExitTransition == PathTransitionType.PrepareJump;

            if (segment.ExitTransition == PathTransitionType.ContinueStraight && !requiresJumpEntry)
                return TransitionInputProfile.Carry;

            if (requiresJumpEntry)
                return TransitionInputProfile.Carry;

            bool requiresSlowEntry = segment.ExitHints.RequireStableFooting
                || segment.ExitTransition is PathTransitionType.FinalStop or PathTransitionType.Turn
                || (segment.ExitTransition == PathTransitionType.LandingRecovery
                    && (segment.ExitHints.AllowAirBrake || IsFiniteSpeedCap(segment)));

            if (!requiresSlowEntry)
                return TransitionInputProfile.Carry;

            double maxExitSpeed = GetTargetMaxExitSpeed(segment);
            double hardBrakeDistance = TransitionBrakingPlanner.EstimateGroundStopDistance(
                physics, world, segment.HeadingX, segment.HeadingZ, applyBackBrake: true);
            double coastStopDistance = TransitionBrakingPlanner.EstimateGroundStopDistance(
                physics, world, segment.HeadingX, segment.HeadingZ, applyBackBrake: false);

            if (remaining < 0.0)
                return TransitionInputProfile.Brake;

            if (forwardSpeed > maxExitSpeed && remaining <= hardBrakeDistance + 0.10)
                return TransitionInputProfile.Brake;

            if (forwardSpeed <= maxExitSpeed && remaining > 0.0)
                return TransitionInputProfile.Carry;

            if (remaining <= coastStopDistance + 0.06)
                return TransitionInputProfile.Coast;

            return TransitionInputProfile.Carry;
        }

        public static TransitionInputProfile ChooseAirProfile(PathSegment segment, Location pos, PlayerPhysics physics, World world)
        {
            if (!segment.ExitHints.AllowAirBrake)
                return TransitionInputProfile.AirHoldForward;

            TransitionInputProfile[] candidates =
            [
                TransitionInputProfile.AirHoldForward,
                TransitionInputProfile.AirRelease,
                TransitionInputProfile.AirBrake
            ];

            return ChooseBest(segment, pos, physics, world, candidates);
        }

        private static TransitionInputProfile ChooseBest(PathSegment segment, Location pos, PlayerPhysics physics, World world,
            TransitionInputProfile[] candidates)
        {
            TransitionInputProfile best = candidates[0];
            double bestScore = double.PositiveInfinity;

            foreach (TransitionInputProfile candidate in candidates)
            {
                double score = Score(segment, pos, physics, world, candidate);
                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private static double Score(PathSegment segment, Location pos, PlayerPhysics physics, World world, TransitionInputProfile candidate)
        {
            PlayerPhysics sim = TemplateHelper.ClonePhysicsForPlanning(physics);
            sim.Position = new Vec3d(pos.X, pos.Y, pos.Z);

            var input = new MovementInput();
            Location simPos = pos;

            for (int tick = 0; tick < segment.ExitHints.HorizonTicks; tick++)
            {
                if (TemplateHelper.ShouldBiasTowardExitHeading(simPos, segment))
                    TemplateHelper.FaceExitHeading(sim, segment);

                input.Reset();
                ApplyCandidateInput(input, candidate, segment);
                sim.ApplyInput(input);
                sim.Tick(world);
                simPos = new Location(sim.Position.X, sim.Position.Y, sim.Position.Z);
            }

            double score = 0.0;
            double exitSpeed = TemplateHelper.ProjectHorizontalSpeedAlongHint(sim, segment);
            double horizontalSpeed = TemplateHelper.GetHorizontalSpeed(sim);

            if (segment.ExitHints.RequireGrounded && !sim.OnGround)
                score += 1000.0;

            if (segment.ExitHints.RequireStableFooting
                && !TemplateHelper.IsSettledOnTargetBlock(simPos, segment.End, sim))
            {
                score += 1000.0;
            }

            if (segment.ExitHints.RequireStableFooting && !sim.OnGround)
            {
                double remaining = TemplateHelper.RemainingDistanceAlongSegment(simPos, segment);
                if (remaining > 0.0)
                    score += 2000.0 + remaining * 500.0;

                if (simPos.Y < segment.End.Y)
                    score += 2000.0 + (segment.End.Y - simPos.Y) * 500.0;
            }

            if (exitSpeed < segment.ExitHints.MinExitSpeed)
                score += (segment.ExitHints.MinExitSpeed - exitSpeed) * 200.0;

            if (exitSpeed > segment.ExitHints.MaxExitSpeed)
                score += (exitSpeed - segment.ExitHints.MaxExitSpeed) * 200.0;

            score += TemplateHelper.HeadingPenaltyDegrees(sim.Yaw, segment);

            if (segment.ExitHints.RequireStableFooting)
            {
                double dx = segment.End.X - simPos.X;
                double dz = segment.End.Z - simPos.Z;
                score += (dx * dx + dz * dz) * 20.0;
            }
            else
            {
                score += Math.Abs(TemplateHelper.RemainingDistanceAlongSegment(simPos, segment)) * 10.0;
            }

            if (segment.ExitHints.RequireJumpReady && horizontalSpeed < segment.ExitHints.MinExitSpeed)
                score += 250.0;

            return score;
        }

        private static void ApplyCandidateInput(MovementInput input, TransitionInputProfile candidate, PathSegment segment)
        {
            switch (candidate)
            {
                case TransitionInputProfile.Carry:
                case TransitionInputProfile.AirHoldForward:
                    input.Forward = true;
                    input.Sprint = segment.PreserveSprint || segment.ExitHints.RequireJumpReady;
                    break;

                case TransitionInputProfile.Brake:
                case TransitionInputProfile.AirBrake:
                    input.Back = true;
                    break;

                case TransitionInputProfile.Coast:
                case TransitionInputProfile.AirRelease:
                default:
                    break;
            }
        }

        private static bool IsFiniteSpeedCap(PathSegment segment)
        {
            return !double.IsPositiveInfinity(segment.ExitHints.MaxExitSpeed);
        }

        private static double GetTargetMaxExitSpeed(PathSegment segment)
        {
            if (IsFiniteSpeedCap(segment))
                return segment.ExitHints.MaxExitSpeed;

            return segment.ExitTransition switch
            {
                PathTransitionType.FinalStop => 0.03,
                PathTransitionType.Turn or PathTransitionType.LandingRecovery => 0.035,
                _ => double.PositiveInfinity
            };
        }
    }
}
