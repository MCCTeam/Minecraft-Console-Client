using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Telemetry;

namespace MinecraftClient.Tests.Pathing.Execution;

internal sealed record PathSegmentRun(int SegmentIndex, MoveType MoveType, int ElapsedTicks, Location Position);

internal sealed class RecordingPathExecutionObserver : IPathExecutionObserver
{
    internal List<PathSegmentRun> SegmentRuns { get; } = [];
    internal int ReplanCount { get; private set; }
    internal int TotalTicks { get; private set; }

    public void OnNavigationStarted(IReadOnlyList<PathSegment> segments) { }

    public void OnSegmentStarted(int segmentIndex, int totalSegments, PathSegment segment) { }

    public void OnSegmentCompleted(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position)
        => SegmentRuns.Add(new PathSegmentRun(segmentIndex, segment.MoveType, elapsedTicks, position));

    public void OnSegmentFailed(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position)
        => SegmentRuns.Add(new PathSegmentRun(segmentIndex, segment.MoveType, elapsedTicks, position));

    public void OnNavigationCompleted(int totalTicks) => TotalTicks = totalTicks;

    public void OnReplanStarted(int replanCount, Location position) => ReplanCount = replanCount;

    public void OnReplanSucceeded(int replanCount, IReadOnlyList<PathSegment> segments) { }

    public void OnReplanFailed(int replanCount, Location position) { }
}
