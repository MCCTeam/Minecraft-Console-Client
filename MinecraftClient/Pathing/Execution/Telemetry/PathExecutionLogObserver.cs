using System;
using System.Collections.Generic;
using System.Globalization;
using MinecraftClient.Mapping;

namespace MinecraftClient.Pathing.Execution.Telemetry
{
    public sealed class PathExecutionLogObserver : IPathExecutionObserver
    {
        private readonly Action<string>? _debug;

        public PathExecutionLogObserver(Action<string>? debug) => _debug = debug;

        public void OnNavigationStarted(IReadOnlyList<PathSegment> segments) =>
            _debug?.Invoke(string.Format(
                CultureInfo.InvariantCulture,
                Translations.pathing_metric_route_start,
                segments.Count));

        public void OnSegmentStarted(int segmentIndex, int totalSegments, PathSegment segment) =>
            _debug?.Invoke(string.Format(
                CultureInfo.InvariantCulture,
                Translations.pathing_metric_segment_start,
                segmentIndex,
                totalSegments,
                segment.MoveType,
                segment.ExitTransition));

        public void OnSegmentCompleted(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position) =>
            _debug?.Invoke(string.Format(
                CultureInfo.InvariantCulture,
                Translations.pathing_metric_segment_complete,
                segmentIndex,
                totalSegments,
                segment.MoveType,
                elapsedTicks,
                position.X,
                position.Y,
                position.Z));

        public void OnSegmentFailed(int segmentIndex, int totalSegments, PathSegment segment, int elapsedTicks, Location position) =>
            _debug?.Invoke(string.Format(
                CultureInfo.InvariantCulture,
                Translations.pathing_metric_segment_failed,
                segmentIndex,
                totalSegments,
                segment.MoveType,
                elapsedTicks,
                position.X,
                position.Y,
                position.Z));

        public void OnNavigationCompleted(int totalTicks) =>
            _debug?.Invoke(string.Format(
                CultureInfo.InvariantCulture,
                Translations.pathing_metric_route_complete,
                totalTicks));

        public void OnReplanStarted(int replanCount, Location position) =>
            _debug?.Invoke(string.Format(
                CultureInfo.InvariantCulture,
                Translations.pathing_metric_replan_start,
                replanCount,
                position.X,
                position.Y,
                position.Z));

        public void OnReplanSucceeded(int replanCount, IReadOnlyList<PathSegment> segments) =>
            _debug?.Invoke(string.Format(
                CultureInfo.InvariantCulture,
                Translations.pathing_metric_replan_success,
                replanCount,
                segments.Count));

        public void OnReplanFailed(int replanCount, Location position) =>
            _debug?.Invoke(string.Format(
                CultureInfo.InvariantCulture,
                Translations.pathing_metric_replan_failed,
                replanCount,
                position.X,
                position.Y,
                position.Z));
    }
}
