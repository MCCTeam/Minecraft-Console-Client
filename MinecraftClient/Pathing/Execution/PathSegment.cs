using System;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;

namespace MinecraftClient.Pathing.Execution
{
    public sealed class PathSegment
    {
        public required Location Start { get; init; }
        public required Location End { get; init; }
        public required MoveType MoveType { get; init; }
        public ParkourProfile ParkourProfile { get; init; } = ParkourProfile.None;
        public PathTransitionType ExitTransition { get; init; } = PathTransitionType.FinalStop;
        public PathTransitionHints ExitHints { get; init; } = PathTransitionHints.Default;
        public bool PreserveSprint { get; init; }

        public int HeadingX => Math.Sign(End.X - Start.X);
        public int HeadingZ => Math.Sign(End.Z - Start.Z);

        public override string ToString() =>
            $"{MoveType}: ({Start.X:F1},{Start.Y:F1},{Start.Z:F1})->({End.X:F1},{End.Y:F1},{End.Z:F1}), transition={ExitTransition}, preserveSprint={PreserveSprint}";
    }
}
