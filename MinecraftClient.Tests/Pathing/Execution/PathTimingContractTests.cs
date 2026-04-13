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
}
