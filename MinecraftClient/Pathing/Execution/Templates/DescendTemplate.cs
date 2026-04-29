using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Execution;
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
        private const float PreDropYawToleranceDeg = 12f;

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

            // Snap yaw on the first tick to avoid a few ticks of sideways drift
            // when the bot enters this segment with a stale orientation (e.g.
            // just after a teleport or after a turn). Ledge-adjacent descends
            // cannot tolerate drift without falling off the wrong side.
            if (_tickCount == 1)
                physics.Yaw = targetYaw;

            if (physics.OnGround && Math.Abs(dy) < (_hasFallen ? 1.0 : 0.6))
            {
                TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
                bool onOrPastTarget = TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, ExpectedEnd)
                    || TemplateHelper.HasReachedSegmentEndPlane(pos, _segment);

                // Fallback: after a diagonal descend landing the bot can end
                // up on a support block that is not yet the target block
                // (footprint still off the landing column).  The braking
                // planner reads "remaining <= coastStop + lead" and returns
                // Coast, which zeroes every input - if the bot has already
                // come to rest this means the segment hangs forever and the
                // pathing manager replans.  When we are stopped, not inside
                // the target block, and not being asked to brake, walk
                // toward the target instead of coasting so the landing
                // resolves in one tick-window.
                if (!decision.HoldBack
                    && !TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, ExpectedEnd)
                    && horizDistSq > 0.01
                    && TemplateHelper.GetHorizontalSpeed(physics) < 0.03)
                {
                    float walkYaw = TemplateHelper.CalculateYaw(dx, dz);
                    physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, walkYaw);
                    input.Forward = true;
                    input.Sprint = _needsSprint;

                    if (GroundedSegmentController.ShouldComplete(_segment, pos, physics))
                        return TemplateState.Complete;
                    return TemplateState.InProgress;
                }

                if (horizDistSq > 0.01 && !decision.HoldBack)
                {
                    float groundedYaw = onOrPastTarget
                        ? TemplateHelper.GetExitHeadingYaw(_segment)
                        : TemplateHelper.ShouldBiasTowardExitHeading(pos, _segment)
                        ? TemplateHelper.GetExitHeadingYaw(_segment)
                        : targetYaw;
                    physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, groundedYaw);
                }

                TemplateHelper.ApplyDecision(input, decision);
                if (decision.HoldBack)
                    TemplateHelper.FaceSegmentHeading(physics, _segment);

                if (GroundedSegmentController.ShouldComplete(_segment, pos, physics))
                    return TemplateState.Complete;
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
                bool onOrPastTarget = TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, ExpectedEnd)
                    || TemplateHelper.HasReachedSegmentEndPlane(pos, _segment);
                // Airborne bias toward the exit heading is only safe when
                // the bot has effectively finished the current segment's
                // horizontal travel: either the footprint is inside the
                // landing block, or the vertical drop is small enough that
                // lateral drift cannot miss the 1x1 landing column.  For
                // multi-block drops the bot is in the air for 8+ ticks;
                // rotating yaw mid-fall (e.g. after crossing the end plane
                // but still 1-2 blocks above landing) pushes sprint/walk
                // momentum perpendicular to the segment and drifts the bot
                // off the landing column into the void.  Keep yaw pointed
                // at the landing center through the whole fall on multi-Y
                // descends; GroundedSegmentController rotates yaw once the
                // bot is actually standing on the landing column.
                double segmentYDrop = _segment.Start.Y - _segment.End.Y;
                bool isSingleStepDescend = segmentYDrop <= 1.0;
                bool footInsideTarget = TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, ExpectedEnd);

                // Long-descend lateral drift guard. When the bot's footprint
                // enters the landing block at the very start of a multi-block
                // fall (e.g. a 22-block water drop where target X/Z column
                // matches the launch column), `biasTowardExitInAir` would
                // immediately rotate yaw to the next segment's heading. With
                // Forward held during the entire fall, the perpendicular air
                // drift accumulates ~0.05 m/tick and over 20+ airborne ticks
                // walks the bot a full block out of the landing column, so it
                // misses the water/landing target and dies on the rim. Only
                // permit exit-heading bias for non-single-step descends once
                // the bot is within ~1.5 m of the landing Y (~3 ticks of
                // free-fall), so any exit-heading drift cannot displace the
                // landing footprint by more than a fraction of a block.
                double remainingFallY = pos.Y - _segment.End.Y;
                bool nearLanding = remainingFallY <= 1.5;

                bool biasTowardExitInAir = (footInsideTarget && (isSingleStepDescend || nearLanding))
                    || (isSingleStepDescend
                        && (onOrPastTarget
                            || (_hasFallen
                                && TemplateHelper.ShouldBiasTowardExitHeading(pos, _segment, distanceThreshold: 1.5))));
                float airborneYaw;
                if (biasTowardExitInAir)
                {
                    airborneYaw = TemplateHelper.GetExitHeadingYaw(_segment);
                }
                else if (!isSingleStepDescend)
                {
                    // Multi-block descend: target-tracking yaw rotates as
                    // the bot drifts past the landing column mid-fall (e.g.
                    // a diagonal 3 c2c drop with dx=-1, dz=-1 starts at
                    // yaw=135, the relative bearing to End flips through
                    // 90 -> 0 -> 315 in 6 air ticks). With Forward input
                    // held, the rotating yaw pushes air-control momentum
                    // perpendicular to the planned trajectory, drifting
                    // the bot ~0.5 m past the landing column and onto an
                    // adjacent block one tier below. Lock airborne yaw to
                    // the segment's start->end heading so air drift stays
                    // aligned with the planned diagonal; the GroundedSegment
                    // controller takes over once the bot is on the landing.
                    airborneYaw = TemplateHelper.CalculateYaw(_segment.HeadingX, _segment.HeadingZ);
                }
                else
                {
                    airborneYaw = targetYaw;
                }

                physics.Yaw = TemplateHelper.SmoothYaw(physics.Yaw, airborneYaw);
                if (_hasFallen || YawDifference(physics.Yaw, airborneYaw) <= PreDropYawToleranceDeg)
                {
                    if (!_hasFallen && !_needsSprint && ShouldCoastOffLedge(pos))
                    {
                        // For short descends into a stop or turn, release forward near the lip
                        // so the landing stays on the intended support instead of overshooting it.
                    }
                    else if (!_hasFallen && !_needsSprint)
                    {
                        GroundedSegmentController.Apply(_segment, _nextSegment, pos, physics, input, world);
                    }
                    else
                    {
                        TransitionBrakingDecision decision = TransitionBrakingPlanner.Plan(_segment, _nextSegment, pos, physics, world);
                        // Multi-block descend overshoot guard: when the
                        // fall spans 2+ Y blocks, sprint momentum will
                        // carry the bot roughly one extra horizontal
                        // block past the planned landing.  If the next
                        // segment prepares a jump (PrepareJump exit) the
                        // bot MUST land inside the planned 1x1 landing
                        // column so the jump takeoff has a valid footing;
                        // overshooting drops into the void or onto a
                        // block 1-2 tiers below, breaking the jump.
                        // Once airborne and past the landing end-plane,
                        // release forward input so sprint momentum decays
                        // via air drag over the final 1-2 ticks of fall,
                        // pulling the bot back into the landing column.
                        //
                        // The same guard applies to long water/landing
                        // drops with any non-PrepareJump exit. A 22-block
                        // fall lasts 20+ airborne ticks; at ~0.2 m/tick
                        // peak air-control velocity, holding Forward for
                        // the entire fall accumulates 4+ m of horizontal
                        // drift past the start ledge and the bot lands
                        // outside the 1x1 water column. Once the
                        // footprint is inside the target column, brake
                        // horizontal velocity so the bot falls straight
                        // down into the water/landing block.
                        bool riskyOvershoot = _hasFallen
                            && segmentYDrop >= 2.0
                            && onOrPastTarget
                            && _segment.ExitTransition == PathTransitionType.PrepareJump;
                        bool longFallFootprintLanding = _hasFallen
                            && segmentYDrop >= 2.0
                            && footInsideTarget;
                        if (riskyOvershoot || longFallFootprintLanding)
                        {
                            input.Forward = false;
                            input.Sprint = false;
                            input.Back = true;
                        }
                        else if (_segment.ExitHints.AllowAirBrake)
                        {
                            TemplateHelper.ApplyDecision(input, decision);
                            if (decision.HoldForward && _needsSprint)
                                input.Sprint = true;
                        }
                        else
                        {
                            input.Forward = true;
                            if (_needsSprint)
                                input.Sprint = true;
                        }
                    }
                }
            }

            return TemplateState.InProgress;
        }

        private bool ShouldCoastOffLedge(Location pos)
        {
            if (_segment.ExitTransition == PathTransitionType.ContinueStraight)
                return false;

            double remaining = (_segment.End.X - pos.X) * _segment.HeadingX
                + (_segment.End.Z - pos.Z) * _segment.HeadingZ;
            return remaining <= 0.55
                && TemplateFootingHelper.IsFootprintInsideTargetBlock(pos, _segment.End);
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
