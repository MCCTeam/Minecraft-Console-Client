using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Moves;
using MinecraftClient.Pathing.Moves.Impl;
using MinecraftClient.Tests.Pathing.Execution;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Moves;

public sealed class MoveDescendTests
{
    private const int FloorY = 79;

    private static CalculationContext BuildContext(World world)
        => new(world, allowParkour: true, allowParkourAscend: true);

    [Fact]
    public void Accepts1BlockStepDown()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        // Raise the source column by one so a +X step descends 1 block.
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY + 1, 0);

        var ctx = BuildContext(world);
        var move = new MoveDescend(1, 0);
        var result = default(MoveResult);

        // Source feet block is FloorY+2, destination feet block is FloorY+1.
        move.Calculate(ctx, 0, FloorY + 2, 0, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(1, result.DestX);
        Assert.Equal(FloorY + 1, result.DestY);
    }

    /// <summary>
    /// Regression: when the landing column is itself solid at y-1 (e.g. a
    /// 2-block-thick platform top), MoveDescend must reject the move.
    /// Previously the simple 1-block branch only checked the y-2 floor and the
    /// y / y+1 body-clearance at the destination, so A* emitted a Descend that
    /// the bot could never execute (it just walked onto the solid y-1 block at
    /// the same feet level), producing an infinite replan loop in live play.
    /// </summary>
    [Fact]
    public void Rejects1BlockDescendIntoSolidLandingColumn()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        // Bot stands on source pillar (0, FloorY+1), feet at FloorY+2.
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY + 1, 0);
        // Destination column is ALSO solid at the feet-landing level (y-1 of source).
        // Concretely: (1, FloorY+1) is stone, (1, FloorY) is stone, and the flat
        // floor under that is still there too. There is no valid 1-block drop.
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 1, 0);

        var ctx = BuildContext(world);
        var move = new MoveDescend(1, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 2, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void Accepts2BlockDrop()
    {
        // Two-tier setup: source pillar at y=FloorY+2, destination floor at y=FloorY.
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY + 1, 0);
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY + 2, 0);

        var ctx = BuildContext(world);
        var move = new MoveDescend(1, 0);
        var result = default(MoveResult);

        // Source feet block is FloorY+3, destination column drops to FloorY+1 floor.
        move.Calculate(ctx, 0, FloorY + 3, 0, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(1, result.DestX);
        Assert.Equal(FloorY + 1, result.DestY);
    }

    [Fact]
    public void RejectsMultiBlockDropWhenFlightColumnIsBlocked()
    {
        // Source pillar at y=FloorY+2, but destination column has a solid
        // block at y-1 that blocks the fall path entirely. The bot cannot
        // enter the destination column at all.
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY + 1, 0);
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY + 2, 0);
        // Blocker: (1, FloorY+2) is solid -- this is the y-1 of the source feet.
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 2, 0);

        var ctx = BuildContext(world);
        var move = new MoveDescend(1, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 3, 0, ref result);

        Assert.True(result.IsImpossible);
    }
}
