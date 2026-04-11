using System.Collections.Generic;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Execution
{
    public sealed class PathSegment
    {
        public required Location Start { get; init; }
        public required Location End { get; init; }
        public required MoveType MoveType { get; init; }

        public static List<PathSegment> FromPath(IReadOnlyList<PathNode> nodes)
        {
            var segments = new List<PathSegment>(nodes.Count - 1);
            for (int i = 1; i < nodes.Count; i++)
            {
                var prev = nodes[i - 1];
                var curr = nodes[i];
                segments.Add(new PathSegment
                {
                    Start = new Location(prev.X + 0.5, prev.Y, prev.Z + 0.5),
                    End = new Location(curr.X + 0.5, curr.Y, curr.Z + 0.5),
                    MoveType = curr.MoveUsed
                });
            }
            return segments;
        }

        public override string ToString() =>
            $"{MoveType}: ({Start.X:F1},{Start.Y:F1},{Start.Z:F1})->({End.X:F1},{End.Y:F1},{End.Z:F1})";
    }
}
