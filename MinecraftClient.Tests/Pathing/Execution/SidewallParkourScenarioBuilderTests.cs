using MinecraftClient.Mapping;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class SidewallParkourScenarioBuilderTests
{
    [Fact]
    public void AcceptedCases_MatchLiveSidewallMatrix()
    {
        (string Id, int Gap, int DeltaY, int WallOffset)[] cases = SidewallParkourScenarioBuilder.AcceptedCases()
            .Select(static c => AssertCase(c))
            .ToArray();

        Assert.Equal(
        [
            ("sidewall-ascend-gap2-dy+1-wo0", 2, 1, 0),
            ("sidewall-ascend-gap3-dy+1-wo0", 3, 1, 0),
            ("sidewall-ascend-gap3-dy+1-wo1", 3, 1, 1),
            ("sidewall-descend-gap2-dy-2-wo0", 2, -2, 0),
            ("sidewall-descend-gap3-dy-2-wo0", 3, -2, 0),
            ("sidewall-descend-gap4-dy-2-wo0", 4, -2, 0),
            ("sidewall-descend-gap5-dy-2-wo0", 5, -2, 0),
            ("sidewall-descend-gap3-dy-2-wo1", 3, -2, 1),
            ("sidewall-descend-gap4-dy-2-wo1", 4, -2, 1),
            ("sidewall-descend-gap5-dy-2-wo1", 5, -2, 1),
            ("sidewall-descend-gap2-dy-1-wo0", 2, -1, 0),
            ("sidewall-descend-gap3-dy-1-wo0", 3, -1, 0),
            ("sidewall-descend-gap4-dy-1-wo0", 4, -1, 0),
            ("sidewall-descend-gap5-dy-1-wo0", 5, -1, 0),
            ("sidewall-descend-gap3-dy-1-wo1", 3, -1, 1),
            ("sidewall-descend-gap4-dy-1-wo1", 4, -1, 1),
            ("sidewall-descend-gap5-dy-1-wo1", 5, -1, 1),
            ("sidewall-flat-gap2-wo0", 2, 0, 0),
            ("sidewall-flat-gap3-wo0", 3, 0, 0),
            ("sidewall-flat-gap4-wo0", 4, 0, 0),
            ("sidewall-flat-gap3-wo1", 3, 0, 1),
            ("sidewall-flat-gap4-wo1", 4, 0, 1),
        ],
        cases);
    }

    [Fact]
    public void RejectedCases_MatchLiveSidewallMatrix()
    {
        (string Id, int Gap, int DeltaY, int WallOffset)[] cases = SidewallParkourScenarioBuilder.RejectedCases()
            .Select(static c => AssertCase(c))
            .ToArray();

        Assert.Equal(
        [
            ("sidewall-ascend-gap4-dy+1-wo0", 4, 1, 0),
            ("sidewall-ascend-gap4-dy+1-wo1", 4, 1, 1),
            ("sidewall-descend-gap6-dy-2-wo0", 6, -2, 0),
            ("sidewall-descend-gap6-dy-2-wo1", 6, -2, 1),
            ("sidewall-descend-gap6-dy-1-wo0", 6, -1, 0),
            ("sidewall-descend-gap6-dy-1-wo1", 6, -1, 1),
            ("sidewall-flat-gap5-wo0", 5, 0, 0),
            ("sidewall-flat-gap5-wo1", 5, 0, 1),
        ],
        cases);
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
    public void BuildWorld_AscendGap3Wo1_RaisesWallsAndLandingsAcrossSegments()
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap: 3, deltaY: 1, wallOffset: 1);

        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 78, 100)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 87, 101)).Type);
        Assert.Equal(Material.Air, world.GetBlock(new Location(99, 88, 100)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 80, 103)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(98, 79, 103)).Type);
        Assert.Equal(Material.Air, world.GetBlock(new Location(98, 78, 103)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(97, 82, 109)).Type);
    }

    [Fact]
    public void BuildWorld_DescendGap5Wo1_LowersWallsAndLandingsAcrossSegments()
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap: 5, deltaY: -2, wallOffset: 1);

        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 76, 100)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 86, 101)).Type);
        Assert.Equal(Material.Air, world.GetBlock(new Location(99, 75, 100)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(99, 77, 105)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(98, 74, 105)).Type);
        Assert.Equal(Material.Air, world.GetBlock(new Location(98, 73, 105)).Type);
        Assert.Equal(Material.Stone, world.GetBlock(new Location(97, 73, 115)).Type);
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

    private static (string Id, int Gap, int DeltaY, int WallOffset) AssertCase(object[] values)
    {
        Assert.Equal(4, values.Length);

        return (
            Assert.IsType<string>(values[0]),
            Assert.IsType<int>(values[1]),
            Assert.IsType<int>(values[2]),
            Assert.IsType<int>(values[3]));
    }
}
