using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Moves;
using MinecraftClient.Pathing.Moves.Impl;
using MinecraftClient.Tests.Pathing.Execution;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Moves;

/// <summary>
/// Regression tests for the Baritone-parity cardinal-split gate in
/// <see cref="JumpFeasibility"/>'s diagonal Ascend branch. When a cardinal
/// fallback (cardinal Walk into the dx or dz shoulder + a cardinal Ascend
/// from there) exists, the diagonal Ascend must be rejected: it has no
/// physical way to redirect the preceding segment's axis-aligned ground
/// momentum into the diagonal in 2 handoff ticks, so executing it
/// overshoots the target and loops on replan.
/// </summary>
public sealed class MoveJumpDiagonalAscendTests
{
    private const int FloorY = 79;

    private static CalculationContext BuildContext(World world)
        => new(world, allowParkour: true, allowParkourAscend: true);

    [Fact]
    public void RejectsDiagonalAscendWhenCardinalSplitIsWalkable()
    {
        // Flat floor at FloorY, so the cardinal shoulders at (1, FloorY, 0)
        // and (0, FloorY, 1) both have solid ground. The ascend target is a
        // 1-block riser on the diagonal corner at (1, FloorY+1, 1). Either
        // "walk +X first, then cardinal Ascend +Z+Y" or "walk +Z first, then
        // cardinal Ascend +X+Y" produces a stable 2-step plan, so the direct
        // diagonal Ascend must be rejected.
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 1, 1);

        var ctx = BuildContext(world);
        var move = MoveJump.DiagonalAscend(1, 1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void AcceptsDiagonalAscendWhenBothCardinalShouldersLackFloor()
    {
        // Island configuration: the source pillar and the diagonal ascend
        // riser are the only walk-on surfaces near the bot. The cardinal
        // shoulders are open air, so no cardinal Walk + cardinal Ascend
        // split exists and the diagonal Ascend is the genuine only option.
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        world.SetBlock(new Location(1, FloorY, 0), Block.Air);
        world.SetBlock(new Location(0, FloorY, 1), Block.Air);
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 1, 1);

        var ctx = BuildContext(world);
        var move = MoveJump.DiagonalAscend(1, 1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(1, result.DestX);
        Assert.Equal(FloorY + 2, result.DestY);
        Assert.Equal(1, result.DestZ);
    }

    [Fact]
    public void RejectsDiagonalAscendWhenOnlyOneCardinalShoulderHasFloor()
    {
        // Only the +X shoulder has floor support; the +Z shoulder is open
        // air. Even a single viable cardinal split is enough for Baritone's
        // gate to forbid the diagonal Ascend, because A* can simply take
        // "walk +X then cardinal Ascend +Z+Y" instead.
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        world.SetBlock(new Location(0, FloorY, 1), Block.Air);
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 1, 1);

        var ctx = BuildContext(world);
        var move = MoveJump.DiagonalAscend(1, 1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void CardinalAscendStillAcceptedOnFlatFloor()
    {
        // Sanity: the gate must not touch cardinal Ascend. A plain +X Ascend
        // onto a 1-block riser on flat floor should still plan as before.
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 1, 0);

        var ctx = BuildContext(world);
        var move = MoveJump.Ascend(1, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(1, result.DestX);
        Assert.Equal(FloorY + 2, result.DestY);
    }
}
