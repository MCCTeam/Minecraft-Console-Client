using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    internal static class GroundedSegmentController
    {
        private const double FinalStopFastCompleteSpeed = 0.08;
        private const double PrepareJumpHandoffDistance = 0.40;

        internal static void Apply(PathSegment segment, PathSegment? nextSegment, Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            if (segment.ExitTransition == PathTransitionType.PrepareJump
                && segment.ExitHints.RequireJumpReady
                && physics.OnGround
                && TemplateFootingHelper.IsCenterInsideTargetBlock(pos, segment.End)
                && IsReadyToFreezeForTurn(segment, pos)
                && TemplateHelper.HeadingPenaltyDegrees(physics.Yaw, segment) > 8.0)
            {
                input.Forward = false;
                input.Sprint = false;
                input.Back = false;
                TemplateHelper.FaceExitHeading(physics, segment);
                return;
            }

            // Compute the braking decision first so rotation and input stay
            // consistent.  Applying the exit-heading bias while we are still
            // braking causes the Back input (which acts opposite to yaw) to
            // push the bot perpendicular to the segment line, which on narrow
            // 1-block walkways turns into a side-off-the-edge step.  Stay on
            // the segment heading for as long as we are braking and only let
            // the bias rotate us once the brake has released.
            TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(segment, nextSegment, pos, physics, world);
            TemplateHelper.ApplyDecision(input, decision);
            if (decision.HoldBack)
            {
                TemplateHelper.FaceSegmentHeading(physics, segment);
                return;
            }

            // On stable-footing Turn exits (the next segment is a walk-like
            // move, not a jump) the next template snaps yaw instantly on
            // its first tick, so pre-rotating here is unnecessary.  On
            // narrow 1-block walkways the bias combined with along-segment
            // momentum pushes the bot perpendicular to the walkway and
            // walks it off the edge (the bot sprint-drifts diagonally
            // while yaw rotates ~45 deg mid-stride).  For Turn exits into
            // a jump (RequireJumpReady) we still need to align yaw before
            // takeoff, so keep the bias there.
            bool suppressBiasForSafeTurn = segment.ExitTransition == PathTransitionType.Turn
                && segment.ExitHints.RequireStableFooting
                && !segment.ExitHints.RequireJumpReady;
            if (!suppressBiasForSafeTurn
                && TemplateHelper.ShouldBiasTowardExitHeading(pos, segment))
                TemplateHelper.FaceExitHeading(physics, segment);
        }

        internal static bool ShouldComplete(PathSegment segment, Location pos, PlayerPhysics physics)
        {
            if (segment.ExitHints.RequireGrounded && !physics.OnGround)
                return false;

            if (segment.ExitTransition == PathTransitionType.ContinueStraight
                && physics.OnGround)
            {
                if (TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, segment.End)
                    && !TemplateFootingHelper.WillLeaveTargetBlockNextTick(pos, physics, segment.End))
                {
                    return true;
                }

                if (TemplateHelper.IsSettledAtEnd(pos, segment.End, physics))
                    return true;
            }

            if (segment.ExitTransition == PathTransitionType.FinalStop
                && physics.OnGround
                && TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, segment.End)
                && !TemplateFootingHelper.WillLeaveTargetBlockNextTick(pos, physics, segment.End))
            {
                return TemplateHelper.GetHorizontalSpeed(physics) <= FinalStopFastCompleteSpeed;
            }

            double exitSpeed = TemplateHelper.ProjectHorizontalSpeedAlongHint(physics, segment);
            // On Turn exits we deliberately do NOT pre-rotate yaw toward the
            // next segment's heading (see Apply() above).  The next segment's
            // template snaps yaw on its first tick, so measuring heading
            // readiness against the exit heading here would deadlock the
            // handoff (bot is still facing segment heading, would never pass
            // the 15 deg gate).  Measure against segment heading for Turn
            // exits with stable footing where yaw will be snapped anyway.
            bool headingReady;
            if (segment.ExitTransition == PathTransitionType.Turn
                && segment.ExitHints.RequireStableFooting
                && !segment.ExitHints.RequireJumpReady)
            {
                headingReady = TemplateHelper.HeadingPenaltyDegrees(physics.Yaw, segment.HeadingX, segment.HeadingZ) <= 25.0
                    || TemplateHelper.HeadingPenaltyDegrees(physics.Yaw, segment) <= 15.0;
            }
            else
            {
                headingReady = TemplateHelper.HeadingPenaltyDegrees(physics.Yaw, segment)
                    <= (segment.ExitHints.RequireJumpReady ? 8.0 : 15.0);
            }

            if (!headingReady)
                return false;

            if (segment.ExitTransition == PathTransitionType.PrepareJump
                && physics.OnGround
                && segment.ExitHints.RequireJumpReady
                && TemplateFootingHelper.IsCenterInsideTargetBlock(pos, segment.End))
            {
                if (segment.MoveType == MoveType.Ascend)
                    return true;

                double handoffDistance = segment.MoveType == MoveType.Parkour
                    ? 0.55
                    : PrepareJumpHandoffDistance;

                return TemplateHelper.RemainingDistanceAlongSegment(pos, segment) <= handoffDistance;
            }

            // LandingRecovery accepts a fully-decelerated handoff: once the bot
            // has reached the target block on the ground, the segment has done
            // its job. Apply this before the MinExitSpeed gate so a Descend
            // that lands and naturally settles to zero speed (e.g. when the
            // following segment is a fresh Traverse rather than a chained
            // Parkour) can hand off cleanly. Without this early-out the bot
            // would idle inside the destination block until the segment timed
            // out, triggering an unnecessary replan.
            if (segment.ExitTransition == PathTransitionType.LandingRecovery
                && physics.OnGround
                && !segment.ExitHints.RequireStableFooting
                && TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, segment.End))
            {
                return true;
            }

            if (exitSpeed < segment.ExitHints.MinExitSpeed)
                return false;

            if (exitSpeed > segment.ExitHints.MaxExitSpeed)
                return false;

            if (segment.ExitHints.RequireStableFooting)
            {
                return physics.OnGround
                    && (segment.ExitTransition == PathTransitionType.FinalStop
                        ? TemplateHelper.IsSettledAtEnd(pos, segment.End, physics)
                        : TemplateHelper.IsSettledOnTargetBlock(pos, segment.End, physics));
            }

            if (segment.ExitHints.RequireJumpReady)
            {
                return physics.OnGround
                    && TemplateHelper.HasReachedSegmentEndPlane(pos, segment)
                    && exitSpeed >= segment.ExitHints.MinExitSpeed;
            }

            return segment.ExitTransition switch
            {
                PathTransitionType.ContinueStraight => TemplateHelper.IsNear(pos, segment.End, horizThresholdSq: 0.09),
                PathTransitionType.PrepareJump => TemplateHelper.HasReachedSegmentEndPlane(pos, segment)
                    && exitSpeed > 0.02,
                PathTransitionType.FinalStop => physics.OnGround && TemplateHelper.IsSettledAtEnd(pos, segment.End, physics),
                _ => physics.OnGround && TemplateHelper.IsSettledOnTargetBlock(pos, segment.End, physics)
            };
        }

        private static bool IsReadyToFreezeForTurn(PathSegment segment, Location pos)
        {
            if (segment.MoveType == MoveType.Ascend)
                return true;

            if (segment.MoveType == MoveType.Parkour
                || (segment.HeadingX != 0 && segment.HeadingZ != 0))
            {
                return TemplateHelper.RemainingDistanceAlongSegment(pos, segment) <= PrepareJumpHandoffDistance;
            }

            return true;
        }
    }
}
