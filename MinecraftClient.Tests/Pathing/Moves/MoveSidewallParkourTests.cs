using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Moves.Impl;
using MinecraftClient.Tests.Pathing.Execution;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Moves;

public sealed class MoveSidewallParkourTests
{
    [Theory]
    [InlineData("sidewall-descend-gap5-dy-1-wo0", 5, 0)]
    [InlineData("sidewall-descend-gap5-dy-1-wo1", 5, 1)]
    public void Calculate_LongDescendStaticEntry_RejectsWithoutPreparedRunup(string scenarioId, int gap, int wallOffset)
    {
        World world = SidewallParkourScenarioBuilder.BuildWorld(gap, deltaY: -1, wallOffset);
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var move = MoveJump.Sidewall(dx: -1, dz: gap, yDelta: -1);
        MoveResult result = default;

        move.Calculate(ctx, 100, 80, 100, ref result);

        Assert.True(result.IsImpossible, scenarioId);
    }

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
        var move = MoveJump.Sidewall(dx: -1, dz: gap, yDelta: deltaY);
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
        var move = MoveJump.Sidewall(dx: -1, dz: gap, yDelta: deltaY);
        MoveResult result = default;

        move.Calculate(ctx, 100, 80, 100, ref result);

        Assert.True(result.IsImpossible, scenarioId);
    }

    // Scenarios captured from the staircase / step-pyramid image where the start
    // block is a lone, overhanging tread with no 2-block runway behind it.
    // Physics allows a cold-start sprint-jump to clear ~3 blocks horizontally,
    // so short sidewall gaps should still plan even without a runway.
    [Theory]
    [InlineData("sidewall-lone-start-flat-gap2-wo0", 2, 0, 0)]
    [InlineData("sidewall-lone-start-flat-gap3-wo0", 3, 0, 0)]
    [InlineData("sidewall-lone-start-flat-gap2-wo1", 2, 0, 1)]
    [InlineData("sidewall-lone-start-ascend-gap2-dy+1-wo0", 2, 1, 0)]
    [InlineData("sidewall-lone-start-descend-gap2-dy-1-wo0", 2, -1, 0)]
    [InlineData("sidewall-lone-start-descend-gap3-dy-1-wo0", 3, -1, 0)]
    public void Calculate_AcceptsLoneStart_ShortSidewallJumps(string scenarioId, int gap, int deltaY, int wallOffset)
    {
        World world = BuildLoneStartWorld(gap, deltaY, wallOffset);
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var move = MoveJump.Sidewall(dx: -1, dz: gap, yDelta: deltaY);
        MoveResult result = default;

        move.Calculate(ctx, 100, 80, 100, ref result);

        Assert.False(result.IsImpossible, scenarioId);
        Assert.Equal(ParkourProfile.Sidewall, result.ParkourProfile);
    }

    [Theory]
    [InlineData("sidewall-lone-start-flat-gap4-wo0", 4, 0, 0)]
    [InlineData("sidewall-lone-start-ascend-gap3-dy+1-wo0", 3, 1, 0)]
    [InlineData("sidewall-lone-start-descend-gap4-dy-1-wo0", 4, -1, 0)]
    public void Calculate_RejectsLoneStart_LongSidewallJumps(string scenarioId, int gap, int deltaY, int wallOffset)
    {
        World world = BuildLoneStartWorld(gap, deltaY, wallOffset);
        var ctx = new CalculationContext(world, allowParkour: true, allowParkourAscend: true);
        var move = MoveJump.Sidewall(dx: -1, dz: gap, yDelta: deltaY);
        MoveResult result = default;

        move.Calculate(ctx, 100, 80, 100, ref result);

        Assert.True(result.IsImpossible, scenarioId);
    }

    private static World BuildLoneStartWorld(int gap, int deltaY, int wallOffset)
    {
        const int startX = 100;
        const int startY = 80;
        const int startZ = 100;
        int floorY = startY - 1;
        int landX = startX - 1;
        int landY = startY + deltaY;
        int landZ = startZ + gap;

        World world = FlatWorldTestBuilder.CreateStoneFloor(floorY: 0, min: 80, max: landZ + 8);
        FlatWorldTestBuilder.ClearBox(world, 90, 70, 90, 110, 96, landZ + 8);

        FlatWorldTestBuilder.SetSolid(world, startX, floorY, startZ);

        FlatWorldTestBuilder.FillSolid(
            world,
            landX,
            Math.Min(floorY, landY - 1),
            startZ,
            landX,
            Math.Max(floorY, landY - 1) + 7,
            startZ + wallOffset);

        FlatWorldTestBuilder.SetSolid(world, landX, landY - 1, landZ);

        return world;
    }
}
