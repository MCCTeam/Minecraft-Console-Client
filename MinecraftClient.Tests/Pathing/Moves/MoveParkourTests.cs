using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Pathing.Moves.Impl;
using MinecraftClient.Tests.Pathing.Execution;
using System.Reflection;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Moves;

public sealed class MoveParkourTests
{
    private const int FloorY = 79;

    private static CalculationContext BuildContext(World world)
        => new(world, allowParkour: true, allowParkourAscend: true);

    private static void SetPreviousMoveType(CalculationContext ctx, MoveType moveType)
    {
        PropertyInfo? property = typeof(CalculationContext).GetProperty(
            nameof(CalculationContext.PreviousMoveType),
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        MethodInfo? setter = property?.SetMethod;
        Assert.NotNull(setter);
        setter!.Invoke(ctx, [moveType]);
    }

    [Fact]
    public void Rejects3x1JumpWhenRunUpMissing()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        world.SetBlock(new Location(-1, FloorY, 0), Block.Air);
        var ctx = BuildContext(world);
        var move = MoveJump.Parkour(3, 0);
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
        var move = MoveJump.Parkour(2, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(2, result.DestX);
    }

    [Fact]
    public void Accepts2x1Gap_TagsDefaultParkourProfile()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        world.SetBlock(new Location(1, FloorY, 0), Block.Air);
        var ctx = BuildContext(world);
        var move = MoveJump.Parkour(2, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(ParkourProfile.Default, result.ParkourProfile);
    }

    [Fact]
    public void Rejects2x1WhenAdjacentBlockIsStillWalkable()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        var ctx = BuildContext(world);
        var move = MoveJump.Parkour(2, 0);
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
        var move = MoveJump.Parkour(2, 0);
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
        var move = MoveJump.Parkour(1, 1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void AcceptsCarried4x1DescendingGapFromSingleBlockLanding()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.ClearBox(world, -2, FloorY - 2, -1, 6, FloorY + 4, 1);
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 4, FloorY - 1, 0);

        var ctx = BuildContext(world);
        SetPreviousMoveType(ctx, MoveType.Parkour);
        var move = MoveJump.Parkour(4, 0, yDelta: -1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.False(result.IsImpossible);
        Assert.Equal(4, result.DestX);
        Assert.Equal(FloorY, result.DestY);
    }

    [Fact]
    public void Rejects4x1AscendingGapEvenWithRunway()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.ClearBox(world, -2, FloorY, -1, 6, FloorY + 4, 1);
        FlatWorldTestBuilder.FillSolid(world, -2, FloorY, 0, 0, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 4, FloorY + 1, 0);

        var ctx = BuildContext(world);
        var move = MoveJump.Parkour(4, 0, yDelta: 1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void Rejects6x1DescendingGapEvenWithRunway()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.ClearBox(world, -3, FloorY - 1, -1, 8, FloorY + 4, 1);
        FlatWorldTestBuilder.FillSolid(world, -3, FloorY, 0, 0, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 6, FloorY - 1, 0);

        var ctx = BuildContext(world);
        var move = MoveJump.Parkour(6, 0, yDelta: -1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void Rejects6x2DescendingGapEvenWithRunway()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.ClearBox(world, -3, FloorY - 2, -1, 8, FloorY + 4, 1);
        FlatWorldTestBuilder.FillSolid(world, -3, FloorY, 0, 0, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 6, FloorY - 2, 0);

        var ctx = BuildContext(world);
        var move = MoveJump.Parkour(6, 0, yDelta: -2);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void RejectsCarried6x1DescendingGapFromSingleBlockLanding()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.ClearBox(world, -2, FloorY - 1, -1, 8, FloorY + 4, 1);
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 6, FloorY - 1, 0);

        var ctx = BuildContext(world);
        SetPreviousMoveType(ctx, MoveType.Parkour);
        var move = MoveJump.Parkour(6, 0, yDelta: -1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void RejectsCarried6x2DescendingGapFromSingleBlockLanding()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.ClearBox(world, -2, FloorY - 2, -1, 8, FloorY + 4, 1);
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 6, FloorY - 2, 0);

        var ctx = BuildContext(world);
        SetPreviousMoveType(ctx, MoveType.Parkour);
        var move = MoveJump.Parkour(6, 0, yDelta: -2);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    // Diagonal ascending parkour: +1 block up with diagonal offset, covers
    // the corner-step-up case seen in stepped pyramids where a straight
    // MoveSidewallParkour would demand an adjacent wall that isn't present.
    // Short (sqrt(5)) ascends work from a lone overhang block because a
    // cold-start sprint jump reaches ~2.5 blocks horizontally; longer
    // diagonals such as (2,2) require a runway and are exercised separately.
    [Theory]
    [InlineData(2, 1)]
    [InlineData(1, 2)]
    public void AcceptsDiagonalAscendingParkour_FromLoneStart(int dx, int dz)
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.ClearBox(world, -5, FloorY, -5, 10, FloorY + 5, 10);

        FlatWorldTestBuilder.SetSolid(world, 0, FloorY, 0);
        int destFloorY = FloorY + 1;
        FlatWorldTestBuilder.SetSolid(world, dx, destFloorY, dz);

        var ctx = BuildContext(world);
        var move = MoveJump.Parkour(dx, dz, yDelta: 1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.False(result.IsImpossible, $"diagonal ascend ({dx},{dz},+1) should plan from lone start");
        Assert.Equal(dx, result.DestX);
        Assert.Equal(destFloorY + 1, result.DestY);
        Assert.Equal(dz, result.DestZ);
    }

    [Fact]
    public void AcceptsDiagonalAscendingParkour_2x2_WithRunway()
    {
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.ClearBox(world, -5, FloorY, -5, 10, FloorY + 5, 10);

        // Diagonal runway behind the jump (opposite the jump direction)
        // so HasRunUp's back-step check at (-1,-1) succeeds.
        FlatWorldTestBuilder.SetSolid(world, -1, FloorY, -1);
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY, 0);
        int destFloorY = FloorY + 1;
        FlatWorldTestBuilder.SetSolid(world, 2, destFloorY, 2);

        var ctx = BuildContext(world);
        var move = MoveJump.Parkour(2, 2, yDelta: 1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.False(result.IsImpossible, "(2,2,+1) should plan with a straight runway behind the jump");
        Assert.Equal(2, result.DestX);
        Assert.Equal(destFloorY + 1, result.DestY);
        Assert.Equal(2, result.DestZ);
    }
}
