using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    internal static class GroundedSegmentController
    {
        internal static void Apply(PathSegment segment, PathSegment? nextSegment, Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(segment, nextSegment, pos, physics, world);
            TemplateHelper.ApplyDecision(input, decision);
            if (decision.HoldBack)
                TemplateHelper.FaceSegmentHeading(physics, segment);
        }

        internal static bool ShouldComplete(PathSegment segment, Location pos, PlayerPhysics physics)
        {
            return segment.ExitTransition switch
            {
                PathTransitionType.ContinueStraight => TemplateHelper.IsNear(pos, segment.End, horizThresholdSq: 0.09),
                PathTransitionType.PrepareJump => TemplateHelper.HasReachedSegmentEndPlane(pos, segment)
                    && TemplateHelper.ProjectHorizontalSpeedAlongSegment(physics, segment) > 0.02,
                _ => physics.OnGround && TemplateHelper.IsSettledOnTargetBlock(pos, segment.End, physics)
            };
        }
    }
}
