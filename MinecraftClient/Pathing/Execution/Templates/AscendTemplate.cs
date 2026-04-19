using System;
using MinecraftClient.Mapping;
using MinecraftClient.Physics;

namespace MinecraftClient.Pathing.Execution.Templates
{
    /// <summary>
    /// Jump up 1 block while moving 1 block in a cardinal direction.
    /// Faces destination, sprints forward, and jumps when on ground.
    ///
    /// Follows Baritone's MovementAscend.updateState gating:
    ///   - jump immediately when headBonkClear (no low-ceiling hazard above source)
    ///   - otherwise wait until close to the destination edge (flatDistToNext &lt;= 1.2)
    ///     and laterally lined up (sideDist &lt;= 0.2) before firing the jump
    /// This avoids bonking the ceiling on short staircases and avoids jumping while
    /// still too far away (which causes the short-hop to stall against the riser).
    /// </summary>
    public sealed class AscendTemplate : IActionTemplate
    {
        private const double EdgeCloseDistance = 1.2;
        private const double LateralAlignmentTolerance = 0.2;

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

            if (physics.OnGround && dy > 0.1)
            {
                bool diagonalAscend = _segment.HeadingX != 0 && _segment.HeadingZ != 0;
                double flatDistToNext = TemplateHelper.RemainingDistanceAlongSegment(pos, _segment);
                double sideDist = TemplateHelper.LateralOffsetFromSegmentLine(pos, _segment);

                bool closeToEdge = flatDistToNext <= EdgeCloseDistance;
                bool laterallyAligned = sideDist <= LateralAlignmentTolerance;

                bool jumpReady;
                if (HasHeadBonkClear(world))
                {
                    // Vertical head-room above the source block is clear, so starting the
                    // jump early is safe and actually makes the short hop more reliable
                    // (matches Baritone's "headBonkClear" shortcut).
                    jumpReady = headingReady;
                }
                else if (diagonalAscend)
                {
                    jumpReady = headingReady;
                }
                else
                {
                    // Mirror Baritone's gate: only jump when close to the riser and
                    // laterally lined up; otherwise we end up banging the side of the
                    // block without gaining height.
                    jumpReady = headingReady && closeToEdge && laterallyAligned;
                }

                if (jumpReady)
                {
                    // Snap rotation to the target direction on the takeoff tick so
                    // the sprint-jump boost goes along the segment line regardless of
                    // how many ticks the smoothing had to consume. Baritone sets
                    // rotation directly every tick and the server accepts it.
                    physics.Yaw = targetYaw;
                    input.Jump = true;
                    _initiatedJump = true;
                }
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

            // Baritone tolerates up to 200 ticks (MAX_TICKS_AWAY) before abandoning a
            // movement. We mirror that budget so the template does not fail spuriously
            // during normal run-up / jump / landing settle flows.
            if (_stuckTicks > 120 || _tickCount > 200)
                return TemplateState.Failed;

            return TemplateState.InProgress;
        }

        /// <summary>
        /// True when no solid block sits two cells above the source ascent position
        /// in any cardinal direction the player might nick while rising. Mirrors
        /// Baritone's MovementAscend.headBonkClear.
        /// </summary>
        private bool HasHeadBonkClear(World world)
        {
            int sx = (int)Math.Floor(ExpectedStart.X);
            int sy = (int)Math.Floor(ExpectedStart.Y);
            int sz = (int)Math.Floor(ExpectedStart.Z);

            // Directly above the source block and each cardinal neighbour at head
            // height must be walkable-through so the player never catches a corner.
            if (!IsWalkThroughAt(world, sx, sy + 2, sz))
                return false;

            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };
            for (int i = 0; i < 4; i++)
            {
                if (!IsWalkThroughAt(world, sx + dx[i], sy + 2, sz + dz[i]))
                    return false;
            }

            return true;
        }

        private static bool IsWalkThroughAt(World world, int x, int y, int z)
        {
            Block block = world.GetBlock(new Location(x, y, z));
            return !block.Type.IsSolid();
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
