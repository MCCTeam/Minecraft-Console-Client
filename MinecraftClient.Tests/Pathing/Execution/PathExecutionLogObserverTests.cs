using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Execution.Telemetry;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathExecutionLogObserverTests
{
    [Fact]
    public void Observer_EmitsMachineReadablePathMetricLines()
    {
        List<string> lines = [];
        PathExecutionLogObserver observer = new(lines.Add);
        PathSegment segment = new()
        {
            Start = new Location(10, 64, 10),
            End = new Location(11, 64, 10),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.ContinueStraight,
        };

        observer.OnNavigationStarted([segment]);
        observer.OnSegmentStarted(0, 1, segment);
        observer.OnSegmentCompleted(0, 1, segment, 7, new Location(11.5, 64, 10.5));
        observer.OnReplanStarted(1, new Location(11.5, 64, 10.5));
        observer.OnReplanSucceeded(1, [segment]);
        observer.OnNavigationCompleted(7);

        Assert.Collection(lines,
            line => Assert.Equal("[PathMetric] routeStart segments=1", line),
            line => Assert.Equal("[PathMetric] segmentStart index=0 total=1 move=Traverse transition=ContinueStraight", line),
            line => Assert.Equal("[PathMetric] segmentComplete index=0 total=1 move=Traverse ticks=7 x=11.50 y=64.00 z=10.50", line),
            line => Assert.Equal("[PathMetric] replanStart count=1 x=11.50 y=64.00 z=10.50", line),
            line => Assert.Equal("[PathMetric] replanSuccess count=1 segments=1", line),
            line => Assert.Equal("[PathMetric] routeComplete totalTicks=7", line));
    }
}
