using MinecraftClient.Mapping;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class SidewallParkourScenarioBuilderTests
{
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
}
