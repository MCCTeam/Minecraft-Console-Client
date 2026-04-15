using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    internal static class GroundedSegmentController
    {
        private const double FinalStopFastCompleteSpeed = 0.08;

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

            if (TemplateHelper.ShouldBiasTowardExitHeading(pos, segment))
                TemplateHelper.FaceExitHeading(physics, segment);

            TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(segment, nextSegment, pos, physics, world);
            TemplateHelper.ApplyDecision(input, decision);
            if (decision.HoldBack)
                TemplateHelper.FaceSegmentHeading(physics, segment);
        }

        internal static bool ShouldComplete(PathSegment segment, Location pos, PlayerPhysics physics)
        {
            if (segment.ExitHints.RequireGrounded && !physics.OnGround)
                return false;

            if (segment.ExitTransition == PathTransitionType.ContinueStraight
                && physics.OnGround
                && TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, segment.End)
                && !TemplateFootingHelper.WillLeaveTargetBlockNextTick(pos, physics, segment.End))
            {
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
            bool headingReady = TemplateHelper.HeadingPenaltyDegrees(physics.Yaw, segment)
                <= (segment.ExitHints.RequireJumpReady ? 8.0 : 15.0);

            if (!headingReady)
                return false;

            if (segment.ExitTransition == PathTransitionType.PrepareJump
                && physics.OnGround
                && segment.ExitHints.RequireJumpReady
                && TemplateFootingHelper.IsCenterInsideTargetBlock(pos, segment.End))
            {
                if (segment.MoveType == MoveType.Ascend)
                    return true;

                if (segment.MoveType == MoveType.Parkour
                    || (segment.HeadingX != 0 && segment.HeadingZ != 0))
                {
                    return TemplateHelper.RemainingDistanceAlongSegment(pos, segment) <= 0.30;
                }

                if (segment.MoveType is not MoveType.Parkour)
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
                return TemplateHelper.RemainingDistanceAlongSegment(pos, segment) <= 0.30;
            }

            return true;
        }
    }
}
