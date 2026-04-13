using MinecraftClient.Tests.Pathing.Execution.Contracts;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class PathTimingContractTests
{
    [Fact]
    public void Run_ManagerAcceptedAscendChain_CapturesPerSegmentTicks_AndZeroReplan()
    {
        PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get("manager-accepted-ascend-chain");

        PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);

        Assert.Equal(0, result.ReplanCount);
        Assert.True(result.Completed);
        Assert.Equal(6, result.SegmentRuns.Count);
        Assert.All(result.SegmentRuns, run => Assert.True(run.ElapsedTicks > 0));
    }

    [Theory]
    [InlineData("same-move-ascend-staircase")]
    [InlineData("same-move-descend-staircase")]
    public void Scenario_ExecutionStaysWithinTimingBudget(string scenarioId)
    {
        PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
        PathingTimingBudget budget = PathingContractStore.LoadFromRepositoryRoot().GetTiming(scenarioId);
        PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);

        PathingContractAssert.TimingMatches(budget, result);
    }

    [Theory]
    [InlineData("repeated-cardinal-parkour-chain")]
    [InlineData("repeated-diagonal-parkour-chain")]
    [InlineData("obstructed-parkour-l-turns")]
    [InlineData("vertical-jump-mix")]
    [InlineData("diagonal-vertical-mix")]
    public void JumpCombo_ExecutionStaysWithinBudget(string scenarioId)
    {
        PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
        PathingTimingBudget budget = PathingContractStore.LoadFromRepositoryRoot().GetTiming(scenarioId);
        PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);

        PathingContractAssert.TimingMatches(budget, result);
    }

    [Theory]
    [InlineData("same-move-straight-traverse-chain")]
    [InlineData("same-move-diagonal-chain")]
    [InlineData("same-move-ascend-staircase")]
    [InlineData("same-move-descend-staircase")]
    [InlineData("same-move-aligned-parkour-chain")]
    [InlineData("mixed-traverse-turn-parkour-turn-traverse")]
    [InlineData("mixed-diagonal-ascend-traverse-descend")]
    [InlineData("mixed-traverse-ascend-parkour-descend")]
    [InlineData("turn-density-alternating-traverse-diagonal-chain")]
    [InlineData("speed-carry-repeated-traverse-ascend")]
    [InlineData("speed-carry-repeated-traverse-descend")]
    [InlineData("speed-carry-repeated-traverse-parkour")]
    public void LongRoute_ExecutionStaysWithinBudget(string scenarioId)
    {
        PathingExecutionScenario scenario = PathingExecutionScenarioCatalog.Get(scenarioId);
        PathingTimingBudget budget = PathingContractStore.LoadFromRepositoryRoot().GetTiming(scenarioId);
        PathingScenarioResult result = PathingScenarioRunner.RunAccepted(scenario);

        PathingContractAssert.TimingMatches(budget, result);
    }
}
