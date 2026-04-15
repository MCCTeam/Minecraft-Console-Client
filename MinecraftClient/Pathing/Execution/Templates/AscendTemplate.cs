using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Jump up 1 block while moving 1 block in a cardinal direction.
    /// Faces destination, sprints forward, and jumps when on ground.
    /// </summary>
    public sealed class AscendTemplate : IActionTemplate
    {
        public Location ExpectedStart { get; }
        public Location ExpectedEnd { get; }

        private readonly PathSegment _segment;
        private readonly PathSegment? _nextSegment;
        private int _tickCount;
        private Location _lastPos;
        private int _stuckTicks;
        private bool _initiatedJump;

        public AscendTemplate(PathSegment segment, PathSegment? nextSegment)
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
            double horizDistSq = dx * dx + dz * dz;

            bool groundedPrepareJumpHandoff = physics.OnGround
                && Math.Abs(dy) < 0.2
                && _segment.ExitTransition == PathTransitionType.PrepareJump
                && _segment.ExitHints.RequireJumpReady
                && TemplateFootingHelper.IsCenterInsideTargetBlock(pos, _segment.End);

            float targetYaw = TemplateHelper.CalculateYaw(dx, dz);
            float targetPitch = TemplateHelper.CalculatePitch(dx, dy, dz);
            if (!groundedPrepareJumpHandoff)
                physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
            physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);
            float headingPenalty = YawDifference(physics.Yaw, targetYaw);
            bool headingReady = headingPenalty <= 8.0;
            bool turnInPlace = !_initiatedJump && !headingReady;
            input.Forward = !turnInPlace;
            input.Sprint = !turnInPlace;

            bool diagonalAscend = _segment.HeadingX != 0 && _segment.HeadingZ != 0;
            bool jumpReady = headingReady
                && (diagonalAscend || TemplateHelper.RemainingDistanceAlongSegment(pos, _segment) <= 1.05);
            if (physics.OnGround && dy > 0.1 && jumpReady)
            {
                input.Jump = true;
                _initiatedJump = true;
            }

            if (physics.OnGround && Math.Abs(dy) < 0.2)
            {
                GroundedSegmentController.Apply(_segment, _nextSegment, pos, physics, input, world);
                if (GroundedSegmentController.ShouldComplete(_segment, pos, physics))
                    return TemplateState.Complete;
            }

            double movedSq = TemplateHelper.HorizontalDistanceSq(pos, _lastPos);
            double movedY = Math.Abs(pos.Y - _lastPos.Y);
            _stuckTicks = (movedSq < 0.0005 && movedY < 0.001) ? _stuckTicks + 1 : 0;
            _lastPos = pos;

            if (_stuckTicks > 40 || _tickCount > 80)
                return TemplateState.Failed;

            return TemplateState.InProgress;
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
