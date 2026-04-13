using System.Collections.Generic;
using MinecraftClient.Mapping;

namespace MinecraftClient.Pathing.Execution.Telemetry
{
    public interface IPathExecutionObserver
    {
        void OnNavigationStarted(IReadOnlyList<PathSegment> segments);
        void OnSegmentStarted(int segmentIndex, int totalSegments, PathSegment segment);
        void OnSegmentCompleted(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position);
        void OnSegmentFailed(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position);
        void OnNavigationCompleted(int totalTicks);
        void OnReplanStarted(int replanCount, Location position);
        void OnReplanSucceeded(int replanCount, IReadOnlyList<PathSegment> segments);
        void OnReplanFailed(int replanCount, Location position);
    }
}
