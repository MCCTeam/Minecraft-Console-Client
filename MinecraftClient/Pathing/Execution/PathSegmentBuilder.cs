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
                PathSegment? next = null;
                if (i + 1 < nodes.Count)
                {
                    var nextNode = nodes[i + 1];
                    var curr = nodes[i];
                    next = new PathSegment
                    {
                        Start = new Location(curr.X + 0.5, curr.Y, curr.Z + 0.5),
                        End = new Location(nextNode.X + 0.5, nextNode.Y, nextNode.Z + 0.5),
                        MoveType = nextNode.MoveUsed
                    };
                }

                var prev = nodes[i - 1];
                var currNode = nodes[i];
                var current = new PathSegment
                {
                    Start = new Location(prev.X + 0.5, prev.Y, prev.Z + 0.5),
                    End = new Location(currNode.X + 0.5, currNode.Y, currNode.Z + 0.5),
                    MoveType = currNode.MoveUsed
                };

                PathTransitionType exitTransition = Classify(current, next);
                segments.Add(new PathSegment
                {
                    Start = current.Start,
                    End = current.End,
                    MoveType = current.MoveType,
                    ExitTransition = exitTransition,
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
    }
}
