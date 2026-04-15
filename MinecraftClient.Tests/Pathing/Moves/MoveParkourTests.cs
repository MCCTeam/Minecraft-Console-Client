using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Moves.Impl;
using MinecraftClient.Tests.Pathing.Execution;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Moves;

public sealed class MoveParkourTests
{
    private const int FloorY = 79;

    private static CalculationContext BuildContext(World world)
        => new(world, allowParkour: true, allowParkourAscend: true);

    [Fact]
    public void Rejects3x1JumpWhenRunUpMissing()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        world.SetBlock(new Location(-1, FloorY, 0), Block.Air);
        var ctx = BuildContext(world);
        var move = new MoveParkour(3, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void Accepts2x1GapWithClearTakeoff()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        world.SetBlock(new Location(1, FloorY, 0), Block.Air);
        var ctx = BuildContext(world);
        var move = new MoveParkour(2, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(2, result.DestX);
    }

    [Fact]
    public void Accepts4x1JumpWithoutRearSupport_WhenTakeoffBlockProvidesRunway()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: -2, max: 6);
        FlatWorldTestBuilder.ClearBox(world, -2, FloorY, -1, 6, FloorY + 4, 1);
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 2, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 4, FloorY, 0);

        var ctx = BuildContext(world);
        var move = new MoveParkour(4, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(4, result.DestX);
        Assert.Equal(0, result.DestZ);
    }

    [Fact]
    public void Rejects2x1WhenAdjacentBlockIsStillWalkable()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        var ctx = BuildContext(world);
        var move = new MoveParkour(2, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void Rejects2x1GapWhenSideWallNarrowsLanding()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.ClearBox(world, -1, FloorY, -2, 4, FloorY + 4, 2);
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 2, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 1, -1);
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 2, -1);
        FlatWorldTestBuilder.SetSolid(world, 2, FloorY + 1, -1);
        FlatWorldTestBuilder.SetSolid(world, 2, FloorY + 2, -1);

        var ctx = BuildContext(world);
        var move = new MoveParkour(2, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void RejectsDiagonalWhenShoulderBlocked()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        world.SetBlock(new Location(1, FloorY + 1, 0), new Block(1));
        world.SetBlock(new Location(1, FloorY + 2, 0), new Block(1));
        var ctx = BuildContext(world);
        var move = new MoveParkour(1, 1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }
}
