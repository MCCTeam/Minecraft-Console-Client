using MinecraftClient.Pathing.Core;
using Xunit;
using Xunit.Abstractions;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathingContractBootstrapTests
{
    private readonly ITestOutputHelper _output;

    public PathingContractBootstrapTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("same-move-ascend-staircase")]
    [InlineData("same-move-descend-staircase")]
    [InlineData("rejected-3x1-invalid-goal")]
    public void PrintShortRouteContractFragments(string scenarioId)
    {
        PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
        PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);

        _output.WriteLine(PathingContractBootstrapWriter.WritePlannerFragment(scenarioId, planResult));
        if (planResult.Status == PathStatus.Success)
            _output.WriteLine(PathingContractBootstrapWriter.WriteTimingFragment(scenarioId, PathingScenarioRunner.RunAccepted(scenario)));
    }

    [Theory]
    [InlineData("repeated-cardinal-parkour-chain")]
    [InlineData("repeated-diagonal-parkour-chain")]
    [InlineData("obstructed-parkour-l-turns")]
    [InlineData("vertical-jump-mix")]
    [InlineData("diagonal-vertical-mix")]
    public void PrintJumpComboContractFragments(string scenarioId)
    {
        PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
        PathResult planResult = PathingScenarioRunner.PlanOnly(scenario);

        _output.WriteLine(PathingContractBootstrapWriter.WritePlannerFragment(scenarioId, planResult));
        if (planResult.Status == PathStatus.Success)
            _output.WriteLine(PathingContractBootstrapWriter.WriteTimingFragment(scenarioId, PathingScenarioRunner.RunAccepted(scenario)));
    }
}
