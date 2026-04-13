using System.Text;
using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Tests.Pathing.Execution.Contracts;
using Xunit;
using Xunit.Sdk;

namespace MinecraftClient.Tests.Pathing.Execution;

internal static class PathingContractAssert
{
    internal static void PlannerMatches(PathingPlannerContract contract, IReadOnlyList<PathSegment> segments, PathResult result)
    {
        if (result.Status != contract.ExpectedStatus)
            throw new XunitException($"planner status mismatch: expected {contract.ExpectedStatus}, got {result.Status}");

        if (segments.Count != contract.Segments.Count)
            throw new XunitException($"segment count mismatch: expected {contract.Segments.Count}, got {segments.Count}");

        for (int i = 0; i < segments.Count; i++)
        {
            PathSegment actual = segments[i];
            PathingPlannerSegmentContract expected = contract.Segments[i];

            Assert.Equal(expected.MoveType, actual.MoveType);
            Assert.Equal(expected.StartBlock, ToBlock(actual.Start));
            Assert.Equal(expected.EndBlock, ToBlock(actual.End));
        }
    }

    internal static void TimingMatches(PathingTimingBudget budget, PathingScenarioResult result)
    {
        if (!result.Completed)
            throw new XunitException($"navigation did not complete\n{Format(result, budget)}");

        if (result.ReplanCount != 0)
            throw new XunitException($"expected 0 replans, saw {result.ReplanCount}\n{Format(result, budget)}");

        if (result.TotalTicks > budget.MaxTotalTicks)
            throw new XunitException($"route exceeded budget: actual={result.TotalTicks} max={budget.MaxTotalTicks}\n{Format(result, budget)}");

        if (result.SegmentRuns.Count != budget.Segments.Count)
            throw new XunitException($"segment timing count mismatch: actual={result.SegmentRuns.Count} expected={budget.Segments.Count}");

        for (int i = 0; i < budget.Segments.Count; i++)
        {
            if (result.SegmentRuns[i].ElapsedTicks > budget.Segments[i].MaxTicks)
                throw new XunitException($"segment {i} exceeded budget\n{Format(result, budget)}");
        }
    }

    private static string Format(PathingScenarioResult result, PathingTimingBudget budget)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"completed={result.Completed} replans={result.ReplanCount}");
        sb.AppendLine($"route actual={result.TotalTicks} expected={budget.ExpectedTotalTicks} max={budget.MaxTotalTicks}");
        for (int i = 0; i < result.SegmentRuns.Count; i++)
        {
            PathSegmentRun actual = result.SegmentRuns[i];
            PathingSegmentTimingBudget expected = budget.Segments[i];
            sb.AppendLine($"seg[{i}] move={actual.MoveType} actual={actual.ElapsedTicks} expected={expected.ExpectedTicks} max={expected.MaxTicks}");
        }

        return sb.ToString();
    }

    private static PathingBlock ToBlock(Location location) =>
        new((int)Math.Floor(location.X), (int)Math.Floor(location.Y), (int)Math.Floor(location.Z));
}
