using MinecraftClient.Mapping;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class SidewallParkourScenarioBuilderTests
{
    [Fact]
    public void AcceptedCases_MatchLiveSidewallMatrix()
    {
        string[] caseIds = SidewallParkourScenarioBuilder.AcceptedCases()
            .Select(static c => Assert.IsType<string>(c[0]))
            .ToArray();

        Assert.Equal(
        [
            "sidewall-ascend-gap2-dy+1-wo0",
            "sidewall-ascend-gap3-dy+1-wo0",
            "sidewall-ascend-gap3-dy+1-wo1",
            "sidewall-descend-gap2-dy-2-wo0",
            "sidewall-descend-gap3-dy-2-wo0",
            "sidewall-descend-gap4-dy-2-wo0",
            "sidewall-descend-gap5-dy-2-wo0",
            "sidewall-descend-gap3-dy-2-wo1",
            "sidewall-descend-gap4-dy-2-wo1",
            "sidewall-descend-gap5-dy-2-wo1",
            "sidewall-descend-gap2-dy-1-wo0",
            "sidewall-descend-gap3-dy-1-wo0",
            "sidewall-descend-gap4-dy-1-wo0",
            "sidewall-descend-gap5-dy-1-wo0",
            "sidewall-descend-gap3-dy-1-wo1",
            "sidewall-descend-gap4-dy-1-wo1",
            "sidewall-descend-gap5-dy-1-wo1",
            "sidewall-flat-gap2-wo0",
            "sidewall-flat-gap3-wo0",
            "sidewall-flat-gap4-wo0",
            "sidewall-flat-gap3-wo1",
            "sidewall-flat-gap4-wo1",
        ],
        caseIds);
    }

    [Fact]
    public void RejectedCases_MatchLiveSidewallMatrix()
    {
        string[] caseIds = SidewallParkourScenarioBuilder.RejectedCases()
            .Select(static c => Assert.IsType<string>(c[0]))
            .ToArray();

        Assert.Equal(
        [
            "sidewall-ascend-gap4-dy+1-wo0",
            "sidewall-ascend-gap4-dy+1-wo1",
            "sidewall-descend-gap6-dy-2-wo0",
            "sidewall-descend-gap6-dy-2-wo1",
            "sidewall-descend-gap6-dy-1-wo0",
            "sidewall-descend-gap6-dy-1-wo1",
            "sidewall-flat-gap5-wo0",
            "sidewall-flat-gap5-wo1",
        ],
        caseIds);
    }

    [Fact]
    public void BuildWorld_FlatGap2Wo0_MatchesLiveRouteGeometry()
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap: 2, deltaY: 0, wallOffset: 0);

        Assert.Equal(Material.Stone, world.GetBlock(new Location(100, 79, 98)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(100, 79, 99)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(100, 79, 100)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 78, 100)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 79, 102)).Type);
        Assert.Equal(Material.Air, world.GetBlock(new Location(100, 79, 101)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(98, 78, 102)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(97, 79, 106)).Type);
        Assert.Equal(Material.Air, world.GetBlock(new Location(98, 79, 103)).Type);
    }

    [Fact]
    public void BuildWorld_FlatGap3Wo1_ExtendsWallByTwoBlocksAlongRunwaySide()
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap: 3, deltaY: 0, wallOffset: 1);

        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 78, 100)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 78, 101)).Type);
        Assert.Equal(Material.Air, world.GetBlock(new Location(99, 78, 102)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 79, 103)).Type);
    }

    [Fact]
    public void Create_FlatGap2Wo0_UsesSameStartAndGoalAsLiveHarness()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(
            "sidewall-flat-gap2-wo0",
            gap: 2,
            deltaY: 0,
            wallOffset: 0);

        Assert.Equal(new Location(100.5, 80, 100.5), scenario.Start);
        Assert.Equal(97, scenario.Goal.X);
        Assert.Equal(80, scenario.Goal.Y);
        Assert.Equal(106, scenario.Goal.Z);
        Assert.Equal(0f, scenario.StartYaw);
    }

    [Fact]
    public void Create_AscendGap3Wo1_ComputesGoalForLaterRaisedEndpoint()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(
            "sidewall-ascend-gap3-dy+1-wo1",
            gap: 3,
            deltaY: 1,
            wallOffset: 1);

        Assert.Equal(new Location(100.5, 80, 100.5), scenario.Start);
        Assert.Equal(97, scenario.Goal.X);
        Assert.Equal(83, scenario.Goal.Y);
        Assert.Equal(109, scenario.Goal.Z);
        Assert.Equal(0f, scenario.StartYaw);
    }

    [Fact]
    public void Create_DescendGap5Wo1_ComputesGoalForLowerEndpoint()
    {
        PathingExecutionScenario scenario = SidewallParkourScenarioBuilder.Create(
            "sidewall-descend-gap5-dy-2-wo1",
            gap: 5,
            deltaY: -2,
            wallOffset: 1);

        Assert.Equal(new Location(100.5, 80, 100.5), scenario.Start);
        Assert.Equal(97, scenario.Goal.X);
        Assert.Equal(74, scenario.Goal.Y);
        Assert.Equal(115, scenario.Goal.Z);
        Assert.Equal(0f, scenario.StartYaw);
    }
}
