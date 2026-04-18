using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Moves.Impl;
using MinecraftClient.Tests.Pathing.Execution;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Moves;

public sealed class MoveSidewallParkourTests
{
    [Theory]
    [InlineData("sidewall-flat-gap2-wo0", 2, 0, 0)]
    [InlineData("sidewall-flat-gap3-wo1", 3, 0, 1)]
    [InlineData("sidewall-ascend-gap2-dy+1-wo0", 2, 1, 0)]
    [InlineData("sidewall-ascend-gap3-dy+1-wo1", 3, 1, 1)]
    [InlineData("sidewall-descend-gap2-dy-1-wo0", 2, -1, 0)]
    [InlineData("sidewall-descend-gap3-dy-1-wo1", 3, -1, 1)]
    [InlineData("sidewall-descend-gap2-dy-2-wo0", 2, -2, 0)]
    [InlineData("sidewall-descend-gap3-dy-2-wo1", 3, -2, 1)]
    public void Calculate_AcceptsTheoryAllowedCases(string scenarioId, int gap, int deltaY, int wallOffset)
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap, deltaY, wallOffset);
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var move = new MoveSidewallParkour(xOffset: -1, zOffset: gap, yDelta: deltaY);
        MoveResult result = default;

        move.Calculate(ctx, 100, 80, 100, ref result);

        Assert.False(result.IsImpossible, scenarioId);
        Assert.Equal(ParkourProfile.Sidewall, result.ParkourProfile);
    }

    [Theory]
    [InlineData("sidewall-flat-gap5-wo0", 5, 0, 0)]
    [InlineData("sidewall-flat-gap5-wo1", 5, 0, 1)]
    [InlineData("sidewall-ascend-gap4-dy+1-wo0", 4, 1, 0)]
    [InlineData("sidewall-ascend-gap4-dy+1-wo1", 4, 1, 1)]
    [InlineData("sidewall-descend-gap6-dy-1-wo0", 6, -1, 0)]
    [InlineData("sidewall-descend-gap6-dy-2-wo1", 6, -2, 1)]
    public void Calculate_RejectsTheoryForbiddenCases(string scenarioId, int gap, int deltaY, int wallOffset)
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap, deltaY, wallOffset);
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var move = new MoveSidewallParkour(xOffset: -1, zOffset: gap, yDelta: deltaY);
        MoveResult result = default;

        move.Calculate(ctx, 100, 80, 100, ref result);

        Assert.True(result.IsImpossible, scenarioId);
    }
}
