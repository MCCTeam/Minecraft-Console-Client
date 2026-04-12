using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Walk off a ledge and drop 1-N blocks to a landing spot.
    /// Walks toward the destination; gravity handles the fall.
    /// Sprints when the horizontal distance is large (> 1.5 blocks).
    /// Supports solid landings, water landings, and mid-fall vine/ladder grabs.
    /// </summary>
    public sealed class DescendTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private readonly PathSegment _segment;
        private readonly PathSegment? _nextSegment;
        private int _tickCount;
        private bool _hasFallen;
        private readonly bool _needsSprint;

        public DescendTemplate(PathSegment segment, PathSegment? nextSegment)
        {
            _segment = segment;
            _nextSegment = nextSegment;
            ExpectedStart = segment.Start;
            ExpectedEnd = segment.End;
            double hdx = segment.End.X - segment.Start.X;
            double hdz = segment.End.Z - segment.Start.Z;
            _needsSprint = (hdx * hdx + hdz * hdz) > 2.25;
        }

        public TemplateState Tick(Location pos, PlayerPhysics physics, MovementInput input, World world)
        {
            _tickCount++;

            double dx = ExpectedEnd.X - pos.X;
            double dz = ExpectedEnd.Z - pos.Z;
            double dy = ExpectedEnd.Y - pos.Y;
            double horizDistSq = dx * dx + dz * dz;

            if (!physics.OnGround)
                _hasFallen = true;

            // Completion: landed in water near destination
            if (_hasFallen && physics.InWater && horizDistSq < 0.5 && Math.Abs(dy) < 2.0)
                return TemplateState.Complete;

            // Fail if climbing up instead of descending
            if (pos.Y > ExpectedStart.Y + 2.0)
                return TemplateState.Failed;

            if (_tickCount > 200)
                return TemplateState.Failed;

            float targetYaw = TemplateHelper.CalculateYaw(dx, dz);
            float targetPitch = TemplateHelper.CalculatePitch(dx, dy, dz);
            physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);

            if (physics.OnGround && Math.Abs(dy) < (_hasFallen ? 0.8 : 0.5))
            {
                if (horizDistSq > 0.01)
                    physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);

                TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
                TemplateHelper.ApplyDecision(input, decision);
                if (decision.HoldBack)
                    TemplateHelper.FaceSegmentHeading(physics, _segment);

                if (_segment.ExitTransition == PathTransitionType.ContinueStraight)
                {
                    double completionThreshold = _hasFallen ? 0.5 : 0.25;
                    if (horizDistSq < completionThreshold)
                        return TemplateState.Complete;
                }
                else if (TemplateHelper.IsSettledAtEnd(pos, ExpectedEnd, physics, horizThresholdSq: 0.0025))
                {
                    return TemplateState.Complete;
                }
            }
            else if (physics.OnClimbable)
            {
                if (horizDistSq > 0.25)
                {
                    physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
                    input.Forward = true;
                }
            }
            else if (horizDistSq > 0.01)
            {
                physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
                input.Forward = true;
                if (_needsSprint)
                    input.Sprint = true;
            }

            return TemplateState.InProgress;
        }
    }
}
