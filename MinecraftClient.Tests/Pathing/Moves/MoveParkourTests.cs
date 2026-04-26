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
    public void Accepts2x1GapWithSingleSideWall()
    {
        // Cardinal 2 c2c flat parkour with a wall along ONE lateral side
        // (z=-1) and clear air on the other (z=+1). The bot's footprint at
        // z=0.5 stays z=[0.2,0.8], so the z=-1 wall (occupies z=[-1,0]) is
        // 0.2 m clear of the bot under on-axis yaw. The previous check
        // rejected this for safety; the relaxed gate accepts as long as at
        // least one lateral side is passable. Mirrors the live scenario
        // where breaking a head-height obstruction in a corridor leaves a
        // jump-over-the-gap option as the only reachable route.
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

        Assert.False(result.IsImpossible);
        Assert.Equal(2, result.DestX);
        Assert.Equal(FloorY + 1, result.DestY);
        Assert.Equal(0, result.DestZ);
    }

    [Fact]
    public void Rejects2x1GapInsideFullyWalledTunnel()
    {
        // Cardinal 2 c2c parkour with walls on BOTH lateral sides at body
        // and head height. With no lateral bail-out margin, an executor
        // yaw drift of >5 degrees during the arc can clip a wall, so the
        // planner still rejects this shape. Guards against accidentally
        // turning the relaxed-gate into "accept everything cardinal".
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.ClearBox(world, -1, FloorY, -2, 4, FloorY + 4, 2);
        FlatWorldTestBuilder.SetSolid(world, 0, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 2, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 1, -1);
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 2, -1);
        FlatWorldTestBuilder.SetSolid(world, 2, FloorY + 1, -1);
        FlatWorldTestBuilder.SetSolid(world, 2, FloorY + 2, -1);
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 1, 1);
        FlatWorldTestBuilder.SetSolid(world, 1, FloorY + 2, 1);
        FlatWorldTestBuilder.SetSolid(world, 2, FloorY + 1, 1);
        FlatWorldTestBuilder.SetSolid(world, 2, FloorY + 2, 1);

        var ctx = BuildContext(world);
        var move = MoveJump.Parkour(2, 0);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.True(result.IsImpossible);
    }

    [Fact]
    public void Accepts2x0Plus1AscendOverHeadObstructionGap()
    {
        // Live regression: bot at (256,127,225) with floor (256,126,225) and
        // a head-height stone at (255,128,225). After breaking the stone,
        // the bot should plan a +1 ascend cardinal sprint jump straight to
        // (254,128,225). One lateral side (z=224) is a continuous wall, the
        // other (z=226) is open. The gap column (255,*,225) and the cell
        // beyond the landing (253,128,225) used to be rejected by
        // HasCardinalSideClearance and HasLandingOvershootClearance.
        var world = FlatWorldTestBuilder.CreateStoneFloor(FloorY);
        FlatWorldTestBuilder.ClearBox(world, -2, FloorY, -2, 4, FloorY + 4, 2);

        FlatWorldTestBuilder.SetSolid(world, 0, FloorY, 0);
        FlatWorldTestBuilder.SetSolid(world, -2, FloorY + 1, 0);

        FlatWorldTestBuilder.SetSolid(world, -3, FloorY + 1, 0);
        FlatWorldTestBuilder.SetSolid(world, -3, FloorY + 2, 0);
        FlatWorldTestBuilder.SetSolid(world, -3, FloorY + 3, 0);

        for (int dx = -3; dx <= 1; dx++)
        {
            FlatWorldTestBuilder.SetSolid(world, dx, FloorY + 1, -1);
            FlatWorldTestBuilder.SetSolid(world, dx, FloorY + 2, -1);
        }

        var ctx = BuildContext(world);
        var move = MoveJump.Parkour(-2, 0, yDelta: 1);
        var result = default(MoveResult);

        move.Calculate(ctx, 0, FloorY + 1, 0, ref result);

        Assert.False(result.IsImpossible, "+1 ascend cardinal 2 c2c should plan past a single-side wall and a wall-bookended landing");
        Assert.Equal(-2, result.DestX);
        Assert.Equal(FloorY + 2, result.DestY);
        Assert.Equal(0, result.DestZ);
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
