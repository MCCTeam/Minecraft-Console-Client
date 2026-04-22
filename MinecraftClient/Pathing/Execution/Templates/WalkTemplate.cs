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
        private int _airborneTicks;

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
            // While approaching the end, steer via pos->end so lateral drift self-
            // corrects. Once the center has entered the target block the pos->end
            // vector becomes tiny/negative and flips yaw by ~180 degrees, which
            // fights GroundedSegmentController's exit-heading rotation and locks
            // yaw at a local equilibrium (e.g. 333 deg on a 1,1 diagonal) where
            // HeadingPenalty never drops below the 8 deg ShouldComplete gate.
            // Fall back to the stable quantized segment heading once inside the
            // target block so the completion check and exit rotation converge.
            // Skip the exit-heading bias on stable-footing Turn exits: it
            // rotates yaw mid-segment while the bot still has along-segment
            // momentum, which on a 1-block walkway drifts the bot
            // perpendicular and walks it off the edge.  The next segment's
            // template snaps yaw on its first tick, so nothing is lost by
            // deferring the rotation.  Keep the bias when the next segment
            // is a jump (RequireJumpReady): we need yaw aligned before
            // takeoff or the jump direction will be off.
            bool suppressBiasForSafeTurn = _segment.ExitTransition == PathTransitionType.Turn
                && _segment.ExitHints.RequireStableFooting
                && !_segment.ExitHints.RequireJumpReady;
            float targetYaw;
            if (!suppressBiasForSafeTurn
                && TemplateHelper.ShouldBiasTowardExitHeading(pos, _segment))
                targetYaw = TemplateHelper.GetExitHeadingYaw(_segment);
            else if (TemplateFootingHelper.IsCenterInsideTargetBlock(pos, _segment.End))
                targetYaw = TemplateHelper.CalculateYaw(_segment.HeadingX, _segment.HeadingZ);
            else
                targetYaw = TemplateHelper.CalculateYaw(dx, dz);
            float targetPitch = TemplateHelper.CalculatePitch(dx, dy, dz);
            // Snap yaw on the first tick so we don't push forward input while the
            // bot is still rotating from whatever yaw it had before this segment
            // started (e.g. a random post-teleport orientation). Baritone-style:
            // the server accepts instant yaw updates and the narrow 1-block lanes
            // in parkour courses don't tolerate 3 ticks of sideways drift.
            physics.Yaw = _tickCount == 1
                ? targetYaw
                : TemplateHelper.SmoothYaw(physics.Yaw, targetYaw);
            physics.Pitch = TemplateHelper.SmoothPitch(physics.Pitch, targetPitch);

            GroundedSegmentController.Apply(_segment, _nextSegment, pos, physics, input, world);

            if (GroundedSegmentController.ShouldComplete(_segment, pos, physics))
                return TemplateState.Complete;

            double movedSq = TemplateHelper.HorizontalDistanceSq(pos, _lastPos);
            _stuckTicks = movedSq < 0.0005 ? _stuckTicks + 1 : 0;
            _lastPos = pos;

            // Walk/Diagonal is a grounded move: if the bot is airborne for more
            // than a handful of ticks the platform is gone beneath us (e.g. we
            // rotated toward an exit heading on a narrow 1-block walkway and
            // stepped off the edge). Fail fast so the replanner can recover
            // before gravity carries the bot 10+ blocks out of position.
            _airborneTicks = physics.OnGround ? 0 : _airborneTicks + 1;
            if (_airborneTicks > 8)
                return TemplateState.Failed;

            int maxTicks = _segment.ExitTransition switch
            {
                PathTransitionType.ContinueStraight => 100,
                PathTransitionType.PrepareJump => 80,
                _ => 140
            };
            if (_stuckTicks > 40 || _tickCount > maxTicks)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }
    }
}
