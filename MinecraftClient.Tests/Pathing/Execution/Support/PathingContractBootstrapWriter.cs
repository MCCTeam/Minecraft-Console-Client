using System.Text.Json;
using System.Text.Json.Serialization;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class PathingContractBootstrapWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    static PathingContractBootstrapWriter()
    {
        JsonOptions.Converters.Add(new JsonStringEnumConverter());
    }

    internal static string WritePlannerFragment(string scenarioId, PathResult planResult)
    {
        IReadOnlyList<PathSegment> segments = PathSegmentBuilder.FromPath(planResult.Path);

        return JsonSerializer.Serialize(new
        {
            scenarioId,
            expectedStatus = planResult.Status,
            segments = segments.Select(segment => new
            {
                moveType = segment.MoveType,
                startBlock = ToBlockObject(segment.Start),
                endBlock = ToBlockObject(segment.End)
            })
        }, JsonOptions);
    }

    internal static string WriteTimingFragment(string scenarioId, PathingScenarioResult result)
    {
        return JsonSerializer.Serialize(new
        {
            scenarioId,
            expectedTotalTicks = result.TotalTicks,
            maxTotalTicks = SeedMaxTicks(result.TotalTicks),
            segments = result.SegmentRuns.Select(run => new
            {
                moveType = run.MoveType,
                expectedTicks = run.ElapsedTicks,
                maxTicks = SeedMaxTicks(run.ElapsedTicks)
            })
        }, JsonOptions);
    }

    private static object ToBlockObject(MinecraftClient.Mapping.Location location) => new
    {
        x = (int)Math.Floor(location.X),
        y = (int)Math.Floor(location.Y),
        z = (int)Math.Floor(location.Z)
    };

    private static int SeedMaxTicks(int expectedTicks) =>
        expectedTicks + Math.Max(2, (int)Math.Ceiling(expectedTicks * 0.20));
}
