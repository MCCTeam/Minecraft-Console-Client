using MinecraftClient.Mapping;
using MinecraftClient.Pathing.Core;
using MinecraftClient.Pathing.Execution;
using MinecraftClient.Physics;
using Xunit;

namespace MinecraftClient.Tests.Pathing.Execution;

public sealed class TransitionLookaheadEvaluatorTests
{
    [Fact]
    public void ChooseGroundProfile_PicksBrake_WhenTurnEntryCapsResidualSpeed()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.Turn,
            ExitHints = new PathTransitionHints(0, 1, 0.0, 0.035, true, true, false, true, 12)
        };

        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.34, 80.0, 0.5),
            DeltaMovement = new Vec3d(0.156, 0.0, 0.0),
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };

        TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseGroundProfile(
            current,
            new Location(1.34, 80.0, 0.5),
            physics,
            world);

        Assert.Equal(TransitionInputProfile.Brake, profile);
    }

    [Fact]
    public void ChooseGroundProfile_PicksCarry_WhenPrepareJumpNeedsRunUpSpeed()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor();
        var current = new PathSegment
        {
            Start = new Location(0.5, 80, 0.5),
            End = new Location(1.5, 80, 0.5),
            MoveType = MoveType.Traverse,
            ExitTransition = PathTransitionType.PrepareJump,
            ExitHints = new PathTransitionHints(1, 0, 0.12, double.PositiveInfinity, false, true, true, false, 10),
            PreserveSprint = true
        };

        var physics = new PlayerPhysics
        {
            Position = new Vec3d(1.02, 80.0, 0.5),
            DeltaMovement = new Vec3d(0.086, 0.0, 0.0),
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };

        TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseGroundProfile(
            current,
            new Location(1.02, 80.0, 0.5),
            physics,
            world);

        Assert.Equal(TransitionInputProfile.Carry, profile);
    }

    [Fact]
    public void ChooseAirProfile_PicksRelease_WhenLandingNeedsSlowStableEntry()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 108, max: 126);
        var current = new PathSegment
        {
            Start = new Location(120.5, 80, 110.5),
            End = new Location(123.5, 80, 110.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.LandingRecovery,
            ExitHints = new PathTransitionHints(0, 1, 0.0, 0.035, true, true, false, true, 12)
        };

        var physics = new PlayerPhysics
        {
            Position = new Vec3d(123.06, 80.92, 110.5),
            DeltaMovement = new Vec3d(0.31, 0.0, 0.0),
            OnGround = false,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };

        TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseAirProfile(
            current,
            new Location(123.06, 80.92, 110.5),
            physics,
            world);

        Assert.Equal(TransitionInputProfile.AirRelease, profile);
    }

    [Fact]
    public void ChooseAirProfile_KeepsForward_WhenThreeBlockLandingRecoveryIsStillShort()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 108, max: 126);
        FlatWorldTestBuilder.ClearBox(world, 118, 79, 108, 126, 90, 112);
        FlatWorldTestBuilder.SetSolid(world, 120, 79, 110);
        FlatWorldTestBuilder.SetSolid(world, 123, 79, 110);
        FlatWorldTestBuilder.SetSolid(world, 123, 79, 111);

        var current = new PathSegment
        {
            Start = new Location(120.5, 80, 110.5),
            End = new Location(123.5, 80, 110.5),
            MoveType = MoveType.Parkour,
            ExitTransition = PathTransitionType.LandingRecovery,
            ExitHints = new PathTransitionHints(0, 1, 0.0, 0.035, true, true, false, true, 12)
        };

        var physics = new PlayerPhysics
        {
            Position = new Vec3d(122.13, 81.02, 110.5),
            DeltaMovement = new Vec3d(0.1798, -0.2277, 0.0),
            OnGround = false,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };

        TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseAirProfile(
            current,
            new Location(122.13, 81.02, 110.5),
            physics,
            world);

        Assert.Equal(TransitionInputProfile.AirHoldForward, profile);
    }

    [Fact]
    public void ChooseGroundProfile_PicksCarry_WhenFinalDescendHasNotClearedUpperSupport()
    {
        World world = FlatWorldTestBuilder.CreateStoneFloor(min: 360, max: 369);
        FlatWorldTestBuilder.ClearBox(world, 360, 79, 358, 369, 85, 362);
        FlatWorldTestBuilder.FillSolid(world, 362, 84, 359, 362, 84, 361);
        FlatWorldTestBuilder.FillSolid(world, 363, 83, 359, 363, 83, 361);
        FlatWorldTestBuilder.FillSolid(world, 364, 82, 359, 364, 82, 361);
        FlatWorldTestBuilder.FillSolid(world, 365, 81, 359, 365, 81, 361);
        FlatWorldTestBuilder.FillSolid(world, 366, 80, 359, 366, 80, 361);
        FlatWorldTestBuilder.FillSolid(world, 367, 79, 359, 367, 79, 361);

        var current = new PathSegment
        {
            Start = new Location(366.5, 81, 360.5),
            End = new Location(367.5, 80, 360.5),
            MoveType = MoveType.Descend,
            ExitTransition = PathTransitionType.FinalStop,
            ExitHints = new PathTransitionHints(1, 0, 0.0, 0.02, true, true, false, false, 12)
        };

        var physics = new PlayerPhysics
        {
            Position = new Vec3d(367.2316, 81.0, 360.4698),
            DeltaMovement = Vec3d.Zero,
            OnGround = true,
            MovementSpeed = 0.1f,
            Yaw = 270f
        };

        TransitionInputProfile profile = TransitionLookaheadEvaluator.ChooseGroundProfile(
            current,
            new Location(367.2316, 81.0, 360.4698),
            physics,
            world);

        Assert.Equal(TransitionInputProfile.Carry, profile);
    }
}
