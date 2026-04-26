using System;
using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Execution
{
    public static class PathSegmentBuilder
    {
        public static List<PathSegment> FromPath(IReadOnlyList<PathNode> nodes)
        {
            var segments = new List<PathSegment>(Math.Max(0, nodes.Count - 1));
            for (int i = 1; i < nodes.Count; i++)
            {
                PathSegment current = CreatePreview(nodes[i - 1], nodes[i]);
                PathSegment? next = i + 1 < nodes.Count ? CreatePreview(nodes[i], nodes[i + 1]) : null;
                PathSegment? nextNext = i + 2 < nodes.Count ? CreatePreview(nodes[i + 1], nodes[i + 2]) : null;

                PathTransitionType exitTransition = Classify(current, next);
                segments.Add(new PathSegment
                {
                    Start = current.Start,
                    End = current.End,
                    MoveType = current.MoveType,
                    ParkourProfile = current.ParkourProfile,
                    ExitTransition = exitTransition,
                    ExitHints = BuildHints(current, next, nextNext, exitTransition),
                    PreserveSprint = exitTransition is PathTransitionType.ContinueStraight or PathTransitionType.PrepareJump
                });
            }
            return segments;
        }

        private static PathTransitionType Classify(PathSegment current, PathSegment? next)
        {
            if (next is null)
                return PathTransitionType.FinalStop;

            if (next.MoveType is MoveType.Parkour or MoveType.Ascend)
                return PathTransitionType.PrepareJump;

            if (current.MoveType is MoveType.Parkour or MoveType.Descend or MoveType.Fall)
                return PathTransitionType.LandingRecovery;

            if (current.HeadingX == next.HeadingX && current.HeadingZ == next.HeadingZ)
                return PathTransitionType.ContinueStraight;

            return PathTransitionType.Turn;
        }

        private static PathSegment CreatePreview(PathNode start, PathNode end)
        {
            return new PathSegment
            {
                Start = new Location(start.X + 0.5, start.Y, start.Z + 0.5),
                End = new Location(end.X + 0.5, end.Y, end.Z + 0.5),
                MoveType = end.MoveUsed,
                ParkourProfile = end.ParkourProfile
            };
        }

        private static PathTransitionHints BuildHints(PathSegment current, PathSegment? next, PathSegment? nextNext,
            PathTransitionType exitTransition)
        {
            if (next is null)
            {
                return new PathTransitionHints(
                    DesiredHeadingX: current.HeadingX,
                    DesiredHeadingZ: current.HeadingZ,
                    MinExitSpeed: 0.0,
                    MaxExitSpeed: 0.02,
                    RequireStableFooting: true,
                    RequireGrounded: true,
                    RequireJumpReady: false,
                    AllowAirBrake: false,
                    HorizonTicks: 12);
            }

            if (next.MoveType is MoveType.Parkour or MoveType.Ascend)
            {
                return new PathTransitionHints(
                    DesiredHeadingX: next.HeadingX,
                    DesiredHeadingZ: next.HeadingZ,
                    MinExitSpeed: next.MoveType == MoveType.Parkour ? 0.10 : 0.0,
                    MaxExitSpeed: double.PositiveInfinity,
                    RequireStableFooting: false,
                    RequireGrounded: true,
                    RequireJumpReady: true,
                    AllowAirBrake: false,
                    HorizonTicks: 10);
            }

            bool turning = current.HeadingX != next.HeadingX || current.HeadingZ != next.HeadingZ;
            bool nextImmediatelyJumps = nextNext is not null
                && nextNext.MoveType is (MoveType.Parkour or MoveType.Ascend);

            // LandingRecovery (current is Descend/Parkour/Fall) takes precedence
            // over the turning branch even when heading changes between current
            // and next. The turning branch demands RequireStableFooting=true,
            // which forces the GroundedSegmentController completion gate to wait
            // for IsSettledOnTargetBlock (footprint inside, won't leave next
            // tick, horizontal speed^2 <= 0.0016). After a multi-block diagonal
            // Descend the bot lands inside the target block already carrying
            // ~0.02 m/tick of residual momentum that the planner can't shed
            // cleanly: the per-tick yaw bias toward the next segment's heading
            // pulls the bot off-axis, the bot slides off the target block, the
            // template re-targets the centre, and so on for ~60 ticks until
            // momentum decays. The LandingRecovery shortcut in
            // GroundedSegmentController.ShouldComplete (!RequireStableFooting +
            // footprint inside target) bypasses the speed gate cleanly the
            // moment the bot reaches the landing column.
            if (exitTransition == PathTransitionType.LandingRecovery)
            {
                return new PathTransitionHints(
                    DesiredHeadingX: next.HeadingX,
                    DesiredHeadingZ: next.HeadingZ,
                    MinExitSpeed: 0.03,
                    MaxExitSpeed: double.PositiveInfinity,
                    RequireStableFooting: false,
                    RequireGrounded: true,
                    RequireJumpReady: false,
                    AllowAirBrake: true,
                    HorizonTicks: 12);
            }

            if (turning)
            {
                return new PathTransitionHints(
                    DesiredHeadingX: next.HeadingX,
                    DesiredHeadingZ: next.HeadingZ,
                    MinExitSpeed: nextImmediatelyJumps ? 0.05 : 0.0,
                    MaxExitSpeed: nextImmediatelyJumps ? 0.16 : 0.05,
                    RequireStableFooting: !nextImmediatelyJumps,
                    RequireGrounded: true,
                    RequireJumpReady: nextImmediatelyJumps,
                    AllowAirBrake: true,
                    HorizonTicks: 12);
            }

            return new PathTransitionHints(
                DesiredHeadingX: next.HeadingX,
                DesiredHeadingZ: next.HeadingZ,
                MinExitSpeed: 0.06,
                MaxExitSpeed: double.PositiveInfinity,
                RequireStableFooting: false,
                RequireGrounded: false,
                RequireJumpReady: false,
                AllowAirBrake: false,
                HorizonTicks: 8);
        }
    }
}
