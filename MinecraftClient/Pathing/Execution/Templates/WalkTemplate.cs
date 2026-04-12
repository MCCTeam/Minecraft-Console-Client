using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Walk/sprint toward a destination on the same Y level.
    /// Used for Traverse and Diagonal moves.
    /// </summary>
    public sealed class WalkTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private readonly PathSegment _segment;
        private readonly PathSegment? _nextSegment;
        private int _tickCount;
        private Location _lastPos;
        private int _stuckTicks;

        public WalkTemplate(PathSegment segment, PathSegment? nextSegment)
        {
            _segment = segment;
            _nextSegment = nextSegment;
            ExpectedStart = segment.Start;
            ExpectedEnd = segment.End;
            _lastPos = segment.Start;
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            _tickCount++;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = ExpectedEnd.Y - pos.Y;
            float targetYaw = TemplateHelper.CalculateYaw(dx, dz);
            float targetPitch = TemplateHelper.CalculatePitch(dx, dy, dz);
            physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
            physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);

            TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
            TemplateHelper.ApplyDecision(input, decision);
            if (decision.HoldBack)
                TemplateHelper.FaceSegmentHeading(physics, _segment);

            if (_segment.ExitTransition == PathTransitionType.ContinueStraight && TemplateHelper.IsNear(pos, ExpectedEnd, horizThresholdSq: 0.09))
                return TemplateState.Complete;

            if (_segment.ExitTransition != PathTransitionType.ContinueStraight && TemplateHelper.IsSettledAtEnd(pos, ExpectedEnd, physics))
                return TemplateState.Complete;

            double movedSq = TemplateHelper.HorizontalDistanceSq(pos, _lastPos);
            _stuckTicks = movedSq < 0.0005 ? _stuckTicks + 1 : 0;
            _lastPos = pos;

            int maxTicks = _segment.ExitTransition == PathTransitionType.ContinueStraight ? 100 : 140;
            if (_stuckTicks > 40 || _tickCount > maxTicks)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }
    }
}
