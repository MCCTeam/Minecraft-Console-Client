using System.Threading;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;

namespace MinecraftClient.Tests.Pathing.Execution;

internal sealed record PathingScenarioResult(
    bool Completed,
    int ReplanCount,
    int TotalTicks,
    IReadOnlyList<PathSegmentRun> SegmentRuns,
    IReadOnlyList<string> DebugLogs,
    IReadOnlyList<string> InfoLogs,
    Location FinalPosition,
    PathResult PlanResult);

internal static class PathingScenarioRunner
{
    internal static PathResult PlanOnly(PathingExecutionScenario scenario)
    {
        World world = scenario.BuildWorld();
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var finder = new AStarPathFinder();

        return finder.Calculate(
            ctx,
            (int)Math.Floor(scenario.Start.X),
            (int)Math.Floor(scenario.Start.Y),
            (int)Math.Floor(scenario.Start.Z),
            scenario.Goal,
            CancellationToken.None,
            timeoutMs: 3000);
    }

    internal static PathingScenarioResult RunAccepted(PathingExecutionScenario scenario)
    {
        World world = scenario.BuildWorld();
        var debugLogs = new List<string>();
        var infoLogs = new List<string>();
        var observer = new RecordingPathExecutionObserver();
        var manager = new PathSegmentManager(debugLogs.Add, infoLogs.Add, observer);
        var physics = TemplateSimulationRunner.CreateGroundedPhysics(scenario.Start, scenario.StartYaw);
        var input = new MovementInput();

        PathResult planResult = PlanOnly(scenario);

        manager.StartNavigation(scenario.Goal, planResult);

        for (int tick = 0; tick < scenario.MaxExecutionTicks && manager.IsNavigating; tick++)
        {
            input.Reset();
            Location pos = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
            manager.Tick(pos, physics, input, world);
            if (!manager.IsNavigating)
                break;

            physics.ApplyInput(input);
            physics.Tick(world);
        }

        Location finalPosition = new(physics.Position.X, physics.Position.Y, physics.Position.Z);
        bool completed = !manager.IsNavigating && manager.Goal is null && observer.TotalTicks > 0;

        return new PathingScenarioResult(
            completed,
            observer.ReplanCount,
            observer.TotalTicks,
            observer.SegmentRuns.AsReadOnly(),
            debugLogs.AsReadOnly(),
            infoLogs.AsReadOnly(),
            finalPosition,
            planResult);
    }
}
